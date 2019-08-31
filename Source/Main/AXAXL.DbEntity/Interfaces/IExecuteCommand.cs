using System;
using System.Data;
using System.Collections.Generic;
using System.Transactions;

namespace AXAXL.DbEntity.Interfaces
{
	public interface IExecuteCommand
	{
		IExecuteCommand SetStoredProcedure(string storedProcedureName, string connectionName = null);
		IExecuteCommand SetCommand(string command, string connectionName = null);
		IExecuteCommand SetParameters(params (string Name, object Value, ParameterDirection Direction)[] parameters);
		IExecuteCommand SetTransactionScopeOption(TransactionScopeOption option);
		IExecuteCommand SetIsolationLevel(System.Transactions.IsolationLevel isolationLevel);
		/// <summary>
		/// Assign command timeout time.  If none is specified, default is 30 seconds,
		/// according to https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.commandtimeout?view=netcore-2.2.
		/// </summary>
		/// <param name="timeoutDurationInSeconds">Timeout in seconds</param>
		/// <returns>Return itself for method call chaining.</returns>
		IExecuteCommand SetTimeout(int timeoutDurationInSeconds = 30);
		IEnumerable<dynamic> Execute(out IDictionary<string, object> parameters);
	}
}
