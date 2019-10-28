using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using AXAXL.DbEntity.EntityGraph;
using AXAXL.DbEntity.Interfaces;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace AXAXL.DbEntity.MSSql
{
	internal partial class MSSqlGenerator : IMSSqlGenerator
	{
		private ILogger log = null;
		private static IQueryExtensionForSqlOperators _extensions = new QueryExtensionForSqlOperators();

		public MSSqlGenerator(ILoggerFactory factory)
		{
			this.log = factory.CreateLogger<MSSqlGenerator>();
		}

		#region IMSSqlGenerator Implementation

		public (int parameterSequence, string whereClause, Func<SqlParameter>[] sqlParameters) CompileWhereClause<T>(Node startingPoint, int parameterSeq, string tableAliasPrefix, string rootMapKey, IInnerJoinMap innerJoinMap, Expression<Func<T, bool>> whereClause)
		{
			var visitor = new WhereClauseVisitor<T>(startingPoint, parameterSeq, tableAliasPrefix, rootMapKey, this.log, innerJoinMap, MSSqlGenerator.CSTypeToSqlTypeMap, _extensions);
			return visitor.Compile(whereClause);
		}

		public (string InsertColumnsClause, string InsertValueClause, NodeProperty[] InsertColumns) CreateInsertComponent(Node node)
		{
			NodeProperty[] insertColumns, outputColumns;
			this.IdentifyUpdateAndOutputColumns(node, true, out outputColumns, out insertColumns);

			var columns = string.Join(", ", insertColumns.Select(p => p.DbColumnName));
			var values = string.Join(
								", ", 
								insertColumns.Select(p => $"@{p.PropertyName}")
								);

			return (columns, values, insertColumns);
		}

		public (string OutputClause, Action<SqlDataReader, dynamic> EntityUpdateAction) CreateOutputComponent(Node node, bool isInserting = true)
		{
			NodeProperty[] updateColumns, outputColumns;

			this.IdentifyUpdateAndOutputColumns(node, isInserting, out outputColumns, out updateColumns);

			if (outputColumns == null || outputColumns.Length <= 0)
			{
				return (string.Empty, null);
			}

			var outputClause = string.Join(", ", outputColumns.Select(p => $"INSERTED.{p.DbColumnName} "));
			outputClause = string.IsNullOrEmpty(outputClause) ? string.Empty : "OUTPUT " + outputClause;

			var inputReader = Expression.Parameter(typeof(SqlDataReader), "reader");
			var inputObject = Expression.Parameter(typeof(object), "object");
			var inputEntity = Expression.Parameter(node.NodeType, "entity");
			var exprBuffer = new List<Expression>();

			exprBuffer.Add(
				Expression.Assign(
					inputEntity,
					Expression.Convert(inputObject, node.NodeType)
					)
				);

			// run through the column by counting because, in such way, we can be 100% sure the ordinal position of the column in SELECT clause, and thus just use
			// dataReader.Get???(ordinal position) instead of using column name.  Using column name in dataReader.Get???() method will be slower.
			exprBuffer.AddRange(
				this.CreateSqlReaderFetchingExpressions(outputColumns, inputReader, inputEntity)
				);

			var exprBlock = Expression.Block(new [] { inputEntity }, exprBuffer.ToArray());
			var lambdaFunc = Expression.Lambda<Action<SqlDataReader, object>> (exprBlock, new [] { inputReader, inputObject });

			this.LogDataFetchingExpression($"Delegate to capture output clause on {node.Name} when inserting='{isInserting}'", lambdaFunc);

			return (outputClause, lambdaFunc.Compile());
		}

		public (string SelectedColumns, Func<SqlDataReader, dynamic> DataReaderToEntityFunc) CreateSelectComponent(string tableAlias, Node node)
		{
			var tableName = this.FormatTableName(node, tableAlias);
			var allColumns = node.AllDbColumns;

			Debug.Assert(allColumns != null && allColumns.Length > 0, $"No column found to create select statement for '{node.NodeType.FullName}'");

			var selectedColumns = string.Join(", ", allColumns.Select(p => $"{tableAlias}.[{p.DbColumnName}]"));
			//var selectClause = string.Format(@"{0} FROM {1}", selectedColumns, tableName);

			var exprBuffer = new List<Expression>();
			var inputParameter = Expression.Parameter(typeof(SqlDataReader), "dataReader");
			var outputParameter = Expression.Variable(node.NodeType, "entity");

			// entity = new T();
			exprBuffer.Add(
				Expression.Assign(
					outputParameter,
					Expression.New(node.NodeType)
				)
			);
			// run through the column by counting because, in such way, we can be 100% sure the ordinal position of the column in SELECT clause, and thus just use
			// dataReader.Get???(ordinal position) instead of using column name.  Using column name in dataReader.Get???() method will be slower.
			exprBuffer.AddRange(CreateSqlReaderFetchingExpressions(allColumns, inputParameter, outputParameter));

			var returnLabel = Expression.Label(node.NodeType, "return");

			exprBuffer.Add(Expression.Label(returnLabel, outputParameter));

			var exprBlock = Expression.Block(new[] { outputParameter }, exprBuffer.ToArray());
			var lambdaFunc = Expression.Lambda<Func<SqlDataReader, dynamic>>(exprBlock, inputParameter);

			this.LogDataFetchingExpression($"Created delegate to fetch SqlReader into entity {node.Name}", lambdaFunc);

			return (selectedColumns, lambdaFunc.Compile());
		}
		/* Archived 2019-10-26
		 * 
		public (string SelectClause, Func<SqlDataReader, dynamic> DataReaderToEntityFunc) CreateSelectComponent(string tableAlias, Node node, int maxNumOfRow)
		{
			var tableName = this.FormatTableName(node, tableAlias);
			var allColumns = node.AllDbColumns;

			Debug.Assert(allColumns != null && allColumns.Length > 0, $"No column found to create select statement for '{node.NodeType.FullName}'");

			var selectColumns = string.Join(", ", allColumns.Select(p => $"{tableAlias}.[{p.DbColumnName}]"));
			var selectClause = string.Format(@"SELECT {0}{1} FROM {2}", maxNumOfRow <= 0 ? string.Empty : $" TOP {maxNumOfRow} ", selectColumns, tableName);

			var exprBuffer = new List<Expression>();
			var inputParameter = Expression.Parameter(typeof(SqlDataReader), "dataReader");
			var outputParameter = Expression.Variable(node.NodeType, "entity");

			// entity = new T();
			exprBuffer.Add(
				Expression.Assign(
					outputParameter,
					Expression.New(node.NodeType)
				)
			);
			// run through the column by counting because, in such way, we can be 100% sure the ordinal position of the column in SELECT clause, and thus just use
			// dataReader.Get???(ordinal position) instead of using column name.  Using column name in dataReader.Get???() method will be slower.
			exprBuffer.AddRange(CreateSqlReaderFetchingExpressions(allColumns, inputParameter, outputParameter));

			var returnLabel = Expression.Label(node.NodeType, "return");

			exprBuffer.Add(Expression.Label(returnLabel, outputParameter));

			var exprBlock = Expression.Block(new[] { outputParameter }, exprBuffer.ToArray());
			var lambdaFunc = Expression.Lambda<Func<SqlDataReader, dynamic>>(exprBlock, inputParameter);

			this.LogDataFetchingExpression($"Created delegate to fetch SqlReader into entity {node.Name}", lambdaFunc);

			return (selectClause, lambdaFunc.Compile());
		}
		*/
		public IDictionary<string, SqlParameter> CreateSqlParameters(Node node, NodeProperty[] columns, string parameterPrefix = null)
		{
			var prefix = string.IsNullOrEmpty(parameterPrefix) ? string.Empty : $"{parameterPrefix}";

			return columns.ToDictionary(
				key => key.PropertyName, 
				value =>
				{
					SqlDbType dbType;
					var validDbType = Enum.TryParse<SqlDbType>(value.DbColumnType, true, out dbType);
					Debug.Assert(validDbType, $"Invalid SqlDbType '{value.DbColumnType}'");

					var parameter = new SqlParameter($"@{prefix}{value.PropertyName}", dbType);
					return parameter;
				});
		}

		public (string AssignmentClause, NodeProperty[] UpdateColumns) CreateUpdateAssignmentComponent(Node node)
		{
			NodeProperty[] updateColumns, outputColumns;
			this.IdentifyUpdateAndOutputColumns(node, false, out outputColumns, out updateColumns);

			return (
					string.Join(
						", ",
						updateColumns.Select(p => $"{p.DbColumnName} = @{p.PropertyName}")
					),
					updateColumns
			);
		}

		public IDictionary<string, SqlParameter> CreateSqlParametersForRawSqlParameters((string Name, object Value, ParameterDirection Direction)[] parameters)
		{
			var resultBuffer = parameters?.ToDictionary(
				key => key.Name,
				p =>
				{
					var parameterName = p.Name.StartsWith('@') ? p.Name : "@" + p.Name;
					var sqlParameter = new SqlParameter()
					{
						ParameterName = parameterName,
						Direction = p.Direction
					};
					if (p.Value != null)
					{
						var dbType = this.GetSqlDbTypeFromCSType(p.Value.GetType());
						sqlParameter.SqlDbType = dbType;
					}
					return sqlParameter;
				});
			resultBuffer = resultBuffer ?? new Dictionary<string, SqlParameter>();
			if (resultBuffer.Any(kv => kv.Value.Direction == ParameterDirection.ReturnValue) == false)
			{
				resultBuffer.Add("Return", new SqlParameter() { ParameterName = @"@ReturnValue", Direction = ParameterDirection.ReturnValue });
			}
			return resultBuffer;
		}

		public string CreateWhereClause(Node node, NodeProperty[] whereColumns, string parameterPrefix = null, string tableAlias = null)
		{
			//Debug.Assert(whereColumns != null && whereColumns.Length > 0 && whereColumns.All(p => string.IsNullOrEmpty(p.DbColumnName) == false));
			var alias = tableAlias == null ? string.Empty : $"{tableAlias}.";
			var whereClause = string.Join(
				" AND ",
				whereColumns.Select(
					p => {
						var rightHandSide = p.IsConstant ? this.FormatConstantValueAsParameterValue(node, p) : $"@{parameterPrefix ?? string.Empty}{p.PropertyName}";
						var condition = string.Format("{0}{1} = {2}", alias, p.DbColumnName, rightHandSide);
						return condition;
					}));
			return whereClause;
		}

		public NodeProperty[] ExtractColumnByPropertyName(Node node, params string[] propertyNames)
		{
			return propertyNames.Select(p => node.GetPropertyFromNode(p)).ToArray();
		}

		public NodeProperty[] ExtractPrimaryKeyAndConcurrencyControlColumns(Node node)
		{
			var whereColumnList = node.PrimaryKeys.Values.ToList();
			if (node.ConcurrencyControl != null)
			{
				whereColumnList.Add(node.ConcurrencyControl);
			}
			return whereColumnList.ToArray();
		}

		public SqlDbType GetSqlDbTypeFromCSType(Type csType)
		{
			Debug.Assert(MSSqlGenerator.CSTypeToSqlTypeMap.ContainsKey(csType), $"Failed to locate mapping for C# type '{csType.FullName}' from C# to SqlDbType mapping");
			return CSTypeToSqlTypeMap[csType];
		}

		public string CreateDeleteClause(Node node)
		{
			return $"DELETE FROM {this.FormatTableName(node)}";
		}

		public IDictionary<string, Func<dynamic, dynamic>> CreatePropertyValueReaderMap(Node node, NodeProperty[] columns)
		{
			Debug.Assert(columns != null && columns.Length > 0);

			return columns.ToDictionary(k => k.PropertyName, v => this.CreatePropertyValueReaderFunc(node, v));
		}

		public string CompileOrderByClause((NodeProperty Property, bool IsAscending)[] orderBy, string tableAlias = null)
		{
			var alias = ! string.IsNullOrEmpty(tableAlias) ? $"{tableAlias}." : string.Empty;
			var orderByClause = string.Empty;
			if (orderBy != null && orderBy.Length > 0)
			{
				orderByClause = 
					" ORDER BY " 
					+
					string.Join(
						", ",
						orderBy.Select(
							o =>
							{
								var asc = o.IsAscending ? "ASC" : "DESC";
								return $"{alias}[{o.Property.DbColumnName}] {asc}";
							})
					);
			}
			return orderByClause;
		}

		public string FormatTableName(Node node, string tableAlias = null)
		{
			Debug.Assert(node != null, $"Input node is null!");
			var tableName = string.IsNullOrEmpty(node.DbSchemaName) ? $"[{node.DbTableName}]" : $"[{node.DbSchemaName}].[{node.DbTableName}]";
			return tableAlias == null ? $"{tableName}" : $"{tableName} AS {tableAlias}";
		}
		#endregion

		#region Private methods

		private Expression[] CreateSqlReaderFetchingExpressions(NodeProperty[] columns, ParameterExpression dataReader, Expression entity)
		{
			Expression<Func<IDataReader, int, bool>> isDbNullFunc = (r, i) => r.IsDBNull(i);
			List<Expression> exprBuffer = new List<Expression>();

			for (int i = 0; i < columns.Length; i++)
			{
				SqlDbType dbType;
				var column = columns[i];

				var validDbType = Enum.TryParse<SqlDbType>(column.DbColumnType, true, out dbType);
				Debug.Assert(validDbType == true, $"Found unknown SqlDbType '{column.DbColumnType}'");

				var entityProperty = Expression.Property(entity, column.PropertyName);
				/* After introduction of RowVersion, the assignment failed because the expression doesn't accept direct assignment from byte[] to RowVersion.
				 * Thus change to always cast the reader result to the property type.
					Expression dbReaderMethod = Expression.Invoke(
						SqlTypeToReaderMap[dbType],
						dataReader,
						Expression.Constant(i)
					);
					if (column.IsNullable == true)
					{
						dbReaderMethod = Expression.Convert(dbReaderMethod, column.PropertyType);
					}
				*/
				Expression dbReaderMethod = Expression.Convert(
					Expression.Invoke(
						SqlTypeToReaderMap[dbType],
						dataReader,
						Expression.Constant(i)
						),
					column.PropertyType
					);

				var assignmentIfNotDbNull = Expression.Assign(entityProperty, dbReaderMethod);
				var assignmentIfDbNull = Expression.Assign(entityProperty, Expression.Default(column.PropertyType));

				var conditional = Expression.IfThenElse(
					Expression.Invoke(isDbNullFunc, dataReader, Expression.Constant(i)),
					assignmentIfDbNull,
					assignmentIfNotDbNull
				);
				exprBuffer.Add(conditional);
			}
			return exprBuffer.ToArray();
		}

		private Func<object, dynamic> CreatePropertyValueReaderFunc(Node node, NodeProperty column)
		{
			var lambda = node.CreatePropertyValueReaderFunc(column);

			this.LogDataFetchingExpression($"Delgate for reading {node.Name}.{column.PropertyName}", lambda);

			return lambda.Compile();
		}

		private void IdentifyUpdateAndOutputColumns(Node node,  bool isInserting, out NodeProperty[] outputColumnList, out NodeProperty[] updateColumnList)
		{
			var currentOp = isInserting ? NodePropertyUpdateOptions.ByDbOnInsert : NodePropertyUpdateOptions.ByDbOnUpdate;
			var allColumnList = node.AllDbColumns.AsEnumerable();
			// Include primary keys from the list for consideration if inserting. Exclude primary keys when updating because primary key should not change
			// during update and thus won't appear ever in setting clause and output clause.
			if (! isInserting)
			{
				allColumnList = allColumnList.Except(node.PrimaryKeys.Values);
			}
			outputColumnList = allColumnList
									.Where(p => (p.UpdateOption & currentOp) != NodePropertyUpdateOptions.ByApp).ToArray();
			updateColumnList = allColumnList
									.Except(outputColumnList)
									.ToArray();
		}

		private string FormatConstantValueAsParameterValue(Node node, NodeProperty property)
		{
			Debug.Assert(property.IsConstant, $"{property.PropertyName} of {node.Name} is not marked as constant");
			var constantValue = property.ConstantValue;
			var typeCode = Type.GetTypeCode(constantValue?.GetType());
			var constantString = constantValue?.ToString();
			if (typeCode == TypeCode.String || typeCode == TypeCode.DateTime)
			{
				constantString = $"'{constantString}'";
			}
			return constantString;
		}
		#endregion
	}
}
