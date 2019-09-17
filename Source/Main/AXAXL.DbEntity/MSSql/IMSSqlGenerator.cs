using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Data;
using System.Linq.Expressions;
using AXAXL.DbEntity.EntityGraph;
using Autofac.Extras.DynamicProxy;
using AXAXL.DbEntity.MSSql.Autofac;

namespace AXAXL.DbEntity.MSSql
{
	[Intercept(MSSqlGeneratorResponseCache.C_MS_SQL_GENERATOR_CACHE_INTERCEPTOR_NAME)]
	public interface IMSSqlGenerator
	{
		SqlDbType GetSqlDbTypeFromCSType(Type csType);

		(int ParameterSequence, string WhereClause, string InnerJoinsClause, Func<SqlParameter>[] SqlParameters) CompileWhereClause<T>(Node startingPoint, int parameterSeq, string tableAliasPrefix, IOrderedDictionary innerJoinMap, Expression<Func<T, bool>> whereClause);

		NodeProperty[] ExtractPrimaryKeyAndConcurrencyControlColumns(Node node);

		NodeProperty[] ExtractColumnByPropertyName(Node node, params string[] propertyNames);

		(string SelectClause, Func<SqlDataReader, dynamic> DataReaderToEntityFunc) CreateSelectComponent(string tableAlias, Node node, int maxNumOfRow);

		IDictionary<string, SqlParameter> CreateSqlParameters(Node node, NodeProperty[] columns, string parameterPrefix = null);

		IDictionary<string, SqlParameter> CreateSqlParametersForRawSqlParameters((string Name, object Value, ParameterDirection Direction)[] parameters);
	
		string CreateWhereClause(Node node, NodeProperty[] whereColumns, string parameterPrefix = null);

		(string OutputClause, Action<SqlDataReader, dynamic> EntityUpdateAction) CreateOutputComponent(Node node, bool IsInserting = true);

		(string AssignmentClause, NodeProperty[] UpdateColumns) CreateUpdateAssignmentComponent(Node node);

		(string InsertColumnsClause, string InsertValueClause, NodeProperty[] InsertColumns) CreateInsertComponent(Node node);

		string CreateDeleteClause(Node node);
		
		IDictionary<string, Func<dynamic, dynamic>> CreatePropertyValueReaderMap(Node node, NodeProperty[] columns);
		
		string CompileOrderByClause((NodeProperty Property, bool IsAscending)[] orderBy);
		
		string FormatTableName(Node node, string tableAlias = null);
	}
}