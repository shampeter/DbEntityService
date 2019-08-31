using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq.Expressions;
using AXAXL.DbEntity.EntityGraph;

namespace AXAXL.DbEntity.MSSql
{
	public interface IMSSqlGenerator
	{
		SqlDbType GetSqlDbTypeFromCSType(Type csType);

		(string WhereClause, Func<SqlParameter>[] SqlParameters) CompileWhereClause<T>(Node node, Expression<Func<T, bool>> whereClause);

		NodeProperty[] ExtractPrimaryKeyAndConcurrencyControlColumns(Node node);

		NodeProperty[] ExtractColumnByPropertyName(Node node, params string[] propertyNames);

		(string SelectClause, Func<SqlDataReader, dynamic> DataReaderToEntityFunc) CreateSelectComponent(Node node);

		IDictionary<string, SqlParameter> CreateSqlParameters(Node node, NodeProperty[] columns, string parameterPrefix = null);

		IDictionary<string, SqlParameter> CreateSqlParametersForRawSqlParameters((string Name, object Value, ParameterDirection Direction)[] parameters);
	
		string CreateWhereClause(Node node, NodeProperty[] whereColumns, string parameterPrefix = null);

		(string OutputClause, Action<SqlDataReader, dynamic> EntityUpdateAction) CreateOutputComponent(Node node, bool IsInserting = true);

		(string AssignmentClause, NodeProperty[] UpdateColumns) CreateUpdateAssignmentComponent(Node node);

		(string InsertColumnsClause, string InsertValueClause, NodeProperty[] InsertColumns) CreateInsertComponent(Node node);

		string CreateDeleteClause(Node node);
		IDictionary<string, Func<dynamic, dynamic>> CreatePropertyValueReaderMap(Node node, NodeProperty[] columns);
		string FormatTableName(Node node);
	}
}