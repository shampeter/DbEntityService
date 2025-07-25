﻿using System;
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

		(int parameterSequence, string whereClause, Func<SqlParameter>[] sqlParameters) CompileWhereClause<T>(Node startingPoint, int parameterSeq, string tableAliasPrefix, string rootMapKey, IInnerJoinMap innerJoinMap, Expression<Func<T, bool>> whereClause);

		NodeProperty[] ExtractPrimaryKeyAndConcurrencyControlColumns(Node node);

		NodeProperty[] ExtractColumnByPropertyName(Node node, params string[] propertyNames);

		//(string SelectClause, Func<SqlDataReader, dynamic> DataReaderToEntityFunc) CreateSelectComponent(string tableAlias, Node node, int maxNumOfRow);

		(string SelectedColumns, Func<SqlDataReader, dynamic> DataReaderToEntityFunc) CreateSelectComponent(string tableAlias, Node node);

		(string SelectedColumns, Delegate DataReaderToEntityFunc) CreateSelectAndGroupKeysComponent(string tableAlias, Node node, NodeProperty[] groupingKeys);

		IDictionary<string, SqlParameter> CreateSqlParameters(Node node, NodeProperty[] columns, string parameterPrefix = null);

		IDictionary<string, SqlParameter> CreateSqlParametersForRawSqlParameters((string Name, object Value, ParameterDirection Direction)[] parameters);

		string CreateWhereClause(Node node, NodeProperty[] whereColumns, string parameterPrefix = null, string tableAlias = null);

		(string OutputClause, Action<SqlDataReader, dynamic> EntityUpdateAction) CreateOutputComponent(Node node, bool IsInserting = true);

		(string AssignmentClause, NodeProperty[] UpdateColumns) CreateUpdateAssignmentComponent(Node node);

		(string InsertColumnsClause, string InsertValueClause, NodeProperty[] InsertColumns) CreateInsertComponent(Node node);

		string CreateDeleteClause(Node node);
		
		IDictionary<string, Func<dynamic, dynamic>> CreatePropertyValueReaderMap(Node node, NodeProperty[] columns);
		
		string CompileOrderByClause((NodeProperty Property, bool IsAscending)[] orderBy, string tableAlias = null);
		
		string FormatTableName(Node node, string tableAlias = null);

		(string primaryWhereClause, SqlParameter[] primaryWhereParameters)[] CreateWhereClauseAndSqlParametersFromKeyValues(Node node, IDictionary<string, object[]> keyValues, out NodeProperty[] groupingKeys, string parameterPrefix = null, string tableAlias = null, int batchSize = 1800);
	}
}