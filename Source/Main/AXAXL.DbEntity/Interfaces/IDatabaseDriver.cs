using System;
using System.Data;
using System.Collections.Generic;
using System.Linq.Expressions;
using AXAXL.DbEntity.EntityGraph;

namespace AXAXL.DbEntity.Interfaces
{
	public interface IDatabaseDriver
	{
		/// <summary>
		/// Insert a data row represented by the entity class into database.  The entity object is required to implement <see cref="ITrackable"/>.
		/// </summary>
		/// <typeparam name="T">Entity object class type</typeparam>
		/// <param name="connectionString">Database connection string.</param>
		/// <param name="entity">Business entity object to be inserted.</param>
		/// <param name="node">The <see cref="Node"/> representing the meta data and object-to-relational database mapping of the entity class.</param>
		/// <returns>Entity object which contains updated data after insert.</returns>
		T Insert<T>(string connectionString, T entity, Node node) where T : class, ITrackable;
		/// <summary>
		/// Delete the data row corresponding to this input entity class.  Entity object is required to implement <see cref="ITrackable"/>.
		/// </summary>
		/// <typeparam name="T">Entity object class type</typeparam>
		/// <param name="connectionString">Database connection string.</param>
		/// <param name="entity">Business entity object to be deleted.</param>
		/// <param name="node">The <see cref="Node"/> representing the meta data and object-to-relational database mapping of the entity class.</param>
		/// <returns>Entity object has been deleted.</returns>
		T Delete<T>(string connectionString, T entity, Node node) where T : class, ITrackable;
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T">Entity object class type</typeparam>
		/// <param name="connectionString">Database connection string.</param>
		/// <param name="entity">Business entity object to be updated.</param>
		/// <param name="node">The <see cref="Node"/> representing the meta data and object-to-relational database mapping of the entity class.</param>
		/// <returns>Entity object which contains updated data after update.</returns>
		T Update<T>(string connectionString, T entity, Node node) where T : class, ITrackable;
		/// <summary>
		/// Select entity object into <see cref="IEnumerable{T}"/> using the Lambda expression as the where clause.
		/// </summary>
		/// <typeparam name="T">Entity object type</typeparam>
		/// <param name="connectionString">Database connection string</param>
		/// <param name="node">The <see cref="Node"/> representing the meta data and object-to-relational database mapping of the entity class.</param>
		/// <param name="whereClauses">List of Lambda expressions representing the where clauses for select.</param>
		/// <param name="orClausesGroup">List of Lambda expresson group representing groups of 'OR' conditions.</param>
		/// <param name="timeoutDurationInSeconds">Timeout setting for this query.  Default is 30 seconds.</param>
		/// <param name="maxNumOfRow">Specific maximum number of rows to be returned.  Set as <= 0 to return all rows.</param>
		/// <returns><see cref="IEnumerable{T}"/> of entity object.</returns>
		IEnumerable<T> Select<T>(
			string connectionString,
			Node node,
			IEnumerable<Expression<Func<T, bool>>> whereClauses,
			IEnumerable<Expression<Func<T, bool>>[]> orClausesGroup,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> Expressions)> childInnerJoinWhereClauses,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> Expressions)> childInnerJoinOrClausesGroup,
			int maxNumOfRow,
			(NodeProperty Property, bool IsAscending)[] orderBy,
			int timeoutDurationInSeconds = 30
			) where T : class, new();
		/// <summary>
		/// Select entity object into <see cref="IEnumerable{T}"/> using the <paramref name="parameters"/> dictionary for the where clause.
		/// </summary>
		/// <typeparam name="T">Entity object type</typeparam>
		/// <param name="connectionString">Database connection string</param>
		/// <param name="node">The <see cref="Node"/> representing the meta data and object-to-relational database mapping of the entity class.</param>
		/// <param name="parameters">Dictionary of name to value representing the where condition, assuming AND operation on all key-value pairs.</param>
		/// <param name="whereClauses">Additonal where clause in addition to the <paramref name="parameters"/></param>
		/// <param name="orClausesGroup">Additional or clauses in addition to the <paramref name="parameters"/></param>
		/// <param name="childInnerJoinWhereClauses">Inner joins to childset and children selection condition</param>
		/// <param name="childInnerJoinOrClausesGroup">Inner joins to childset and children selection or group condition</param>
		/// <param name="timeoutDurationInSeconds">Timeout setting for this query.  Default is 30 seconds.</param>
		/// <returns><see cref="IEnumerable{T}"/> of entity object.</returns>
		IEnumerable<T> Select<T>(
			string connectionString,
			Node node,
			IDictionary<string, object> parameters,
			IEnumerable<Expression> whereClauses, 
			IEnumerable<Expression[]> orClausesGroup,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> Expressions)> childInnerJoinWhereClauses,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> Expressions)> childInnerJoinOrClausesGroup,
			int timeoutDurationInSeconds = 30
			) where T : class, new();
		IEnumerable<T> Select<T>(
			string connectionString,
			Node node,
			IDictionary<string, object[]> parameters,
			IEnumerable<Expression> whereClauses,
			IEnumerable<Expression[]> orClausesGroup,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression> Expressions)> childInnerJoinWhereClauses,
			IList<(IList<NodeEdge> Path, Node TargetChild, IEnumerable<Expression[]> Expressions)> childInnerJoinOrClausesGroup,
			int timeoutDurationInSeconds = 30
			) where T : class, new();
		/// <summary>
		/// Select entity object into <see cref="IEnumerable{T}"/> using the <paramref name="parameters"/> dictionary for the where clause.
		/// </summary>
		/// <typeparam name="T">Entity object type</typeparam>
		/// <param name="connectionString">Database connection string</param>
		/// <param name="node">The <see cref="Node"/> representing the meta data and object-to-relational database mapping of the entity class.</param>
		/// <param name="parameters">Dictionary of name to value representing the where condition, assuming AND operation on all key-value pairs.</param>
		/// <param name="timeoutDurationInSeconds">Timeout setting for this query.  Default is 30 seconds.</param>
		/// <returns><see cref="IEnumerable{T}"/> of entity object.</returns>
		IEnumerable<T> Select<T>(string connectionString, Node node, IDictionary<string, object> parameters, int timeoutDurationInSeconds = 30) where T : class, new();
		/// <summary>
		/// Execute sql command using values from <paramref name="parameters"/> as parameter value for sql command.
		/// </summary>
		/// <param name="connectionString">Database connection string</param>
		/// <param name="isStoredProcedure">true if command string is a stored procedure name</param>
		/// <param name="rawSqlCommand">Raw sql query</param>
		/// <param name="parameters">Array of value tuple which has parameter name, value and direction.</param>
		/// <param name="outputParameters">value returned for those paraeters marked as OUTPUT</param>
		/// <param name="timeoutDurationInSeconds">Timeout setting for this query.  Default is 30 seconds.</param>
		/// <returns>List of <see cref="System.Dynamic.ExpandoObject"/> of any resultset returned.</returns>
		IEnumerable<dynamic> ExecuteCommand(string connectionString, bool isStoredProcedure, string rawSqlCommand, (string Name, object Value, ParameterDirection Direction)[] parameters, out IDictionary<string, object> outputParameters, int timeoutDurationInSeconds = 30);
		/// <summary>
		/// Execute sql command using values from <paramref name="parameters"/> as parameter value for sql command.
		/// </summary>
		/// <typeparam name="T">Entity type used to store returned result set.</typeparam>
		/// <param name="connectionString">Database connection string</param>
		/// <param name="node">Node of <typeparamref name="T"/></param>
		/// <param name="isStoredProcedure">true if command string is a stored procedure name</param>
		/// <param name="rawSqlCommand">Raw sql query</param>
		/// <param name="parameters">Array of value tuple which has parameter name, value and direction.</param>
		/// <param name="outputParameters">value returned for those paraeters marked as OUTPUT</param>
		/// <param name="timeoutDurationInSeconds">Timeout setting for this query.  Default is 30 seconds.</param>
		/// <returns>Enumerable of <typeparamref name="T"/></returns>
		IEnumerable<T> ExecuteCommand<T>(string connectionString, Node node, bool isStoredProcedure, string rawSqlCommand, (string Name, object Value, ParameterDirection Direction)[] parameters, out IDictionary<string, object> outputParameters, int timeoutDurationInSeconds = 30) where T : class, new();
		/// <summary>
		/// Return corresponding <see cref="System.Data.SqlDbType"/> for a C# object type.
		/// </summary>
		/// <param name="csType">C# object type</param>
		/// <returns><see cref="System.Data.SqlTypes"/></returns>
		string GetSqlDbType(Type csType);
	}
}