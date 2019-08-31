using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Transactions;
using AXAXL.DbEntity.Interfaces;
using Microsoft.Extensions.Logging;

namespace AXAXL.DbEntity.Services
{
	public class ExecuteCommand : IExecuteCommand
	{
		private IDbServiceOption ServiceOption { get; set; }
		private ILogger Log { get; set; }
		private IDatabaseDriver Driver { get; set; }
		private string CommandText { get; set; }
		private bool IsStoredProcedure { get; set; }
		private string ConnectionName { get; set; }
		private (string Name, object Value, ParameterDirection Direction)[] Parameters { get; set; }
		private int TimeoutDurationInSeconds { get; set; }
		private bool isolationChanged;
		private bool scopeOptionChanged;

		internal ExecuteCommand(ILogger log, IDbServiceOption serviceOption, IDatabaseDriver driver)
		{
			this.Log = log;
			this.ServiceOption = serviceOption;
			this.Driver = driver;
			this.isolationChanged = false;
			this.scopeOptionChanged = false;
		}
		private TransactionScopeOption ScopeOption { get; set; }

		private System.Transactions.IsolationLevel Isolation { get; set; }

		public IEnumerable<dynamic> Execute(out IDictionary<string, object> parameters)
		{
			var connectionString = string.IsNullOrEmpty(this.ConnectionName) ? this.ServiceOption.GetDefaultConnectionString() : this.ServiceOption.GetConnectionString(this.ConnectionName);
			IEnumerable<dynamic> resultSet = null;
			if (this.isolationChanged || this.scopeOptionChanged)
			{
				if (! scopeOptionChanged)
				{
					this.ScopeOption = TransactionScopeOption.Required;
				}
				if (! isolationChanged)
				{
					this.Isolation = System.Transactions.IsolationLevel.ReadCommitted;
				}
				var option = new TransactionOptions() { IsolationLevel = this.Isolation };
				using (var transaction = new TransactionScope(this.ScopeOption, option))
				{
					resultSet = this.Driver.ExecuteCommand(connectionString, this.IsStoredProcedure, this.CommandText, this.Parameters, out parameters, this.TimeoutDurationInSeconds);
					transaction.Complete();
				}
			}
			else
			{
				resultSet = this.Driver.ExecuteCommand(connectionString, this.IsStoredProcedure, this.CommandText, this.Parameters, out parameters, this.TimeoutDurationInSeconds);
			}
			return resultSet;
		}

		public IExecuteCommand SetCommand(string command, string connectionName = null)
		{
			this.CommandText = command;
			this.ConnectionName = connectionName;
			this.IsStoredProcedure = false;
			return this;
		}

		public IExecuteCommand SetIsolationLevel(System.Transactions.IsolationLevel isolationLevel)
		{
			this.Isolation = isolationLevel;
			this.isolationChanged = true;
			return this;
		}

		public IExecuteCommand SetParameters(params (string Name, object Value, ParameterDirection Direction)[] parameters)
		{
			this.Parameters = parameters;
			return this;
		}

		public IExecuteCommand SetStoredProcedure(string storedProcedureName, string connectionName = null)
		{
			this.CommandText = storedProcedureName;
			this.ConnectionName = connectionName;
			this.IsStoredProcedure = true;
			return this;
		}

		public IExecuteCommand SetTimeout(int timeoutDurationInSeconds = 30)
		{
			this.TimeoutDurationInSeconds = timeoutDurationInSeconds;
			return this;
		}

		public IExecuteCommand SetTransactionScopeOption(TransactionScopeOption option)
		{
			this.ScopeOption = option;
			this.scopeOptionChanged = true;

			return this;
		}
	}
}
