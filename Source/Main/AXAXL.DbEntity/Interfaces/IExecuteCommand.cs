using System;
using System.Data;
using System.Collections.Generic;
using System.Transactions;

namespace AXAXL.DbEntity.Interfaces
{
	public interface IExecuteCommand
	{
		/// <summary>
		/// Assign the stored procedure name to this execution.
		/// </summary>
		/// <param name="storedProcedureName">stored procedure name</param>
		/// <param name="connectionName">connection name</param>
		/// <returns>self for method chaining.</returns>
		IExecuteCommand SetStoredProcedure(string storedProcedureName, string connectionName = null);

		/// <summary>
		/// Assign raw sql to this execution.
		/// </summary>
		/// <param name="command">Raw sql commands</param>
		/// <param name="connectionName">connection name</param>
		/// <returns>self for method chaining.</returns>
		IExecuteCommand SetCommand(string command, string connectionName = null);

		/// <summary>
		/// Assign parameters as specified by the stored procedure or as code in the raw sql command.
		/// </summary>
		/// <param name="parameters">Array of value tuple.</param>
		/// <returns>self for method chaining.</returns>
		IExecuteCommand SetParameters(params (string Name, object Value, ParameterDirection Direction)[] parameters);

		/// <summary>
		/// Assign transaction scope option to this execution.
		/// </summary>
		/// <param name="option"><see cref="TransactionScopeOption"/></param>
		/// <returns>self for method chaining.</returns>
		IExecuteCommand SetTransactionScopeOption(TransactionScopeOption option);

		/// <summary>
		/// Assign isolation level to this execution.
		/// </summary>
		/// <param name="isolationLevel"><see cref="System.Transactions.IsolationLevel"/></param>
		/// <returns>self for method chaining.</returns>
		IExecuteCommand SetIsolationLevel(System.Transactions.IsolationLevel isolationLevel);

		/// <summary>
		/// Assign command timeout time.  If none is specified, default is 30 seconds,
		/// according to https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.commandtimeout?view=netcore-2.2.
		/// </summary>
		/// <param name="timeoutDurationInSeconds">Timeout in seconds</param>
		/// <returns>Return itself for method call chaining.</returns>
		IExecuteCommand SetTimeout(int timeoutDurationInSeconds = 30);

		/// <summary>
		/// Execute the command or stored procedure assigned.
		/// </summary>
		/// <param name="parameters">
		/// Dictionary of output or returned parameters with their name and value.  
		/// Parameters as specified in <seealso cref="SetParameters((string Name, object Value, ParameterDirection Direction)[])"/>
		/// with direction being not <see cref="ParameterDirection.Input"/> will be returned in this dictionary.
		/// </param>
		/// <returns>
		/// If the execute returns any data row, they will be returned as list of <see cref="System.Dynamic.ExpandoObject"/>.
		/// The column name in the data row returned will be the property names of the <see cref="System.Dynamic.ExpandoObject"/> object.
		/// </returns>
		IEnumerable<dynamic> Execute(out IDictionary<string, object> parameters);
	}
}
