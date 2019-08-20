using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using AXAXL.DbEntity.EntityGraph;
using System.Text;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace AXAXL.DbEntity.MSSql
{
	public partial class MSSqlGenerator : IMSSqlGenerator
	{
		private ILogger log = null;
		public MSSqlGenerator(ILoggerFactory factory)
		{
			this.log = factory.CreateLogger<MSSqlGenerator>();
		}

		#region IMSSqlGenerator Implementation

		public (string WhereClause, Func<SqlParameter>[] SqlParameters) CompileWhereClause<T>(Node node, Expression<Func<T, bool>> whereClause)
		{
			var visitor = new WhereClauseVisitor<T>(this.log, node, MSSqlGenerator.CSTypeToSqlTypeMap);
			return visitor.Compile(whereClause);
		}

		public (string InsertColumnsClause, string InsertValueClause, NodeProperty[] InsertColumns) CreateInsertComponent(Node node)
		{
			NodeProperty[] insertColumns, outputColumns;
			this.IdentifyUpdateAndOutputColumns(node, NodePropertyUpdateOptions.ByDbOnInsert, out outputColumns, out insertColumns);

			var columns = string.Join(", ", insertColumns.Select(p => p.DbColumnName));
			var values = string.Join(", ", insertColumns.Select(p => $"@{p.PropertyName}"));

			return (columns, values, insertColumns);
		}

		public (string OutputClause, Action<SqlDataReader, dynamic> EntityUpdateAction) CreateOutputComponent(Node node, bool IsInserting = true)
		{
			NodeProperty[] updateColumns, outputColumns;
			var mode = IsInserting ? NodePropertyUpdateOptions.ByDbOnInsert : NodePropertyUpdateOptions.ByDbOnInsertAndUpdate;

			this.IdentifyUpdateAndOutputColumns(node, NodePropertyUpdateOptions.ByDbOnInsert, out outputColumns, out updateColumns);

			if (outputColumns == null || outputColumns.Length <= 0)
			{
				return (string.Empty, null);
			}

			var outputClause = string.Join(", ", outputColumns.Select(p => $"INSERTED.{p.DbColumnName} "));
			outputClause = string.IsNullOrEmpty(outputClause) ? string.Empty : "OUTPUT " + outputClause;

			var inputReader = Expression.Parameter(typeof(SqlDataReader), "reader");
			var inputEntity = Expression.Parameter(node.NodeType, "entity");
			var exprBuffer = new List<Expression>();

			Expression<Func<IDataReader, int, bool>> isDbNullFunc = (r, i) => r.IsDBNull(i);

			// run through the column by counting because, in such way, we can be 100% sure the ordinal position of the column in SELECT clause, and thus just use
			// dataReader.Get???(ordinal position) instead of using column name.  Using column name in dataReader.Get???() method will be slower.
			for (int i = 0; i < outputColumns.Length; i++)
			{
				var column = outputColumns[i];
				SqlDbType dbType;
				var validDbType = Enum.TryParse<SqlDbType>(column.DbColumnType, true, out dbType);
				Debug.Assert(validDbType == true, $"Found unknown SqlDbType '{column.DbColumnType}'");
				var entityProperty = Expression.Property(inputEntity, column.PropertyName);
				var dbReaderMethod = Expression.Invoke(
					SqlTypeToReaderMap[dbType],
					inputReader,
					Expression.Constant(i)
				);
				var assignmentIfNotDbNull = Expression.Assign(entityProperty, dbReaderMethod);
				var assignmentIfDbNull = Expression.Assign(entityProperty, Expression.Default(column.PropertyType));

				var conditional = Expression.IfThenElse(
					Expression.Invoke(isDbNullFunc, inputReader, Expression.Constant(i)),
					assignmentIfDbNull,
					assignmentIfNotDbNull
				);
				exprBuffer.Add(conditional);
			}
			var exprBlock = Expression.Block(exprBuffer.ToArray());
			var lambdaFunc = Expression.Lambda<Action<SqlDataReader, dynamic>> (exprBlock, new [] { inputReader, inputEntity });

			this.LogDataFetchingExpression($"Delegate to capture output clause on {node.Name} when inserting='{IsInserting}'", lambdaFunc);

			return (outputClause, lambdaFunc.Compile());
		}

		public (string SelectClause, Func<SqlDataReader, dynamic> DataReaderToEntityFunc) CreateSelectComponent(Node node)
		{
			var tableName = this.FormatTableName(node);
			var allColumns = node.PrimaryKeys.Values
								.Concat(node.DataColumns.Values.Where(p => string.IsNullOrEmpty(p.DbColumnName) == false))
								.ToArray();
			if (node.ConcurrencyControl != null)
			{
				allColumns = allColumns.Concat(new[] { node.ConcurrencyControl }).ToArray();
			}

			Debug.Assert(allColumns != null && allColumns.Length > 0, $"No column found to create select statement for '{node.NodeType.FullName}'");

			var selectColumns = string.Join(", ", allColumns.Select(p => $"{p.DbColumnName}"));
			var selectClause = string.Format(@"SELECT {0} FROM {1}", selectColumns, tableName);
			
			var exprBuffer = new List<Expression>();
			var inputParameter = Expression.Parameter(typeof(SqlDataReader), "dataReader");
			var outputParameter = Expression.Variable(node.NodeType, "entity");
			Expression<Func<IDataReader, int, bool>> isDbNullFunc = (r, i) => r.IsDBNull(i);

			// entity = new T();
			exprBuffer.Add(
				Expression.Assign(
					outputParameter,
					Expression.New(node.NodeType)
				)
			);
			// run through the column by counting because, in such way, we can be 100% sure the ordinal position of the column in SELECT clause, and thus just use
			// dataReader.Get???(ordinal position) instead of using column name.  Using column name in dataReader.Get???() method will be slower.
			for (int i = 0; i < allColumns.Length; i++)
			{
				var column = allColumns[i];
				SqlDbType dbType;
				var validDbType = Enum.TryParse<SqlDbType>(column.DbColumnType, true, out dbType);
				Debug.Assert(validDbType == true, $"Found unknown SqlDbType '{column.DbColumnType}'");
				var entityProperty = Expression.Property(outputParameter, column.PropertyName);
				var dbReaderMethod = Expression.Invoke(
					SqlTypeToReaderMap[dbType],
					inputParameter,
					Expression.Constant(i)
				);
				var assignmentIfNotDbNull = Expression.Assign(entityProperty, dbReaderMethod);
				var assignmentIfDbNull = Expression.Assign(entityProperty, Expression.Default(column.PropertyType));

				var conditional = Expression.IfThenElse(
					Expression.Invoke(isDbNullFunc, inputParameter, Expression.Constant(i)),
					assignmentIfDbNull,
					assignmentIfNotDbNull
				);
				exprBuffer.Add(conditional);
			}

			var returnLabel = Expression.Label(node.NodeType, "return");

			exprBuffer.Add(Expression.Label(returnLabel, outputParameter));

			var exprBlock = Expression.Block(new[] { outputParameter }, exprBuffer.ToArray());
			var lambdaFunc = Expression.Lambda<Func<SqlDataReader, dynamic>>(exprBlock, inputParameter);

			this.LogDataFetchingExpression($"Created delegate to fetch SqlReader into entity {node.Name}", lambdaFunc);

			return (selectClause, lambdaFunc.Compile());
		}

		public IDictionary<string, SqlParameter> CreateSqlParameters(Node node, NodeProperty[] columns, string parameterPrefix = null)
		{
			Debug.Assert(columns != null && columns.Length > 0 && columns.All(p => string.IsNullOrEmpty(p.DbColumnName) == false));
			var prefix = string.IsNullOrEmpty(parameterPrefix) ? string.Empty : parameterPrefix;

			return columns.ToDictionary(
				key => key.PropertyName, 
				value =>
				{
					var name = $"@{prefix}{value.PropertyName}";
					SqlDbType dbType;
					var validDbType = Enum.TryParse<SqlDbType>(value.DbColumnType, true, out dbType);
					Debug.Assert(validDbType, $"Invalid SqlDbType '{value.DbColumnType}'");
					var parameter = new SqlParameter(name, dbType);
					return parameter;
				});
		}

		public (string AssignmentClause, NodeProperty[] UpdateColumns) CreateUpdateAssignmentComponent(Node node)
		{
			NodeProperty[] updateColumns, outputColumns;
			this.IdentifyUpdateAndOutputColumns(node, NodePropertyUpdateOptions.ByDbOnInsertAndUpdate, out outputColumns, out updateColumns);

			return (
					string.Join(
						", ",
						updateColumns.Select(p => $"{p.DbColumnName} = @{p.PropertyName}")
					),
					updateColumns
			);
		}

		public SqlParameter[] CreateSqlParametersForRawSqlParameters(IDictionary<string, object> parameters)
		{
			if (parameters == null)
			{
				return new SqlParameter[0];
			}
			return parameters.Select(kv => {
				var parameterName = kv.Key.StartsWith('@') ? kv.Key : "@"+kv.Key;
				object parameterValue = kv.Value ?? DBNull.Value;
				return new SqlParameter(parameterName, parameterValue);

			}).ToArray();			
		}

		public string CreateWhereClause(Node node, NodeProperty[] whereColumns, string parameterPrefix = null)
		{
			Debug.Assert(whereColumns != null && whereColumns.Length > 0 && whereColumns.All(p => string.IsNullOrEmpty(p.DbColumnName) == false));
			var whereClause = string.Join(
				" AND ",
				whereColumns.Select(p => $"{p.DbColumnName} = @{parameterPrefix ?? string.Empty}{p.PropertyName}")
				);
			return string.IsNullOrEmpty(whereClause) ? string.Empty : @" WHERE " + whereClause;
		}

		public NodeProperty[] ExtractColumnByPropertyName(Node node, params string[] propertyNames)
		{
			Debug.Assert(propertyNames != null && propertyNames.Length > 0);
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
		public string FormatTableName(Node node)
		{
			Debug.Assert(node != null, $"Input node is null!");
			return string.IsNullOrEmpty(node.DbSchemaName) ? $"[{node.DbTableName}]" : $"[{node.DbSchemaName}].[{node.DbTableName}]";
		}
		#endregion

		#region Private methods

		private Func<object, dynamic> CreatePropertyValueReaderFunc(Node node, NodeProperty column)
		{
			var lambda = node.CreatePropertyValueReaderFunc(column);

			this.LogDataFetchingExpression($"Delgate for reading {node.Name}.{column.PropertyName}", lambda);

			return lambda.Compile();
		}

		private void IdentifyUpdateAndOutputColumns(Node node, NodePropertyUpdateOptions updateOption, out NodeProperty[] outputColumnList, out NodeProperty[] updateColumnList)
		{
			var allColumnList = node.DataColumns.Values
									.Where(p => string.IsNullOrEmpty(p.DbColumnName) == false);
			outputColumnList = allColumnList
									.Where(p => p.UpdateOption == updateOption).ToArray();
			updateColumnList = allColumnList
									.Except(outputColumnList)
									.ToArray();
		}

		#endregion
	}
}
