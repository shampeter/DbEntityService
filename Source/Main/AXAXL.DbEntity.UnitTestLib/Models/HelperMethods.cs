using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Transactions;
using AXAXL.DbEntity.Interfaces;

namespace AXAXL.DbEntity.UnitTestLib.Models
{
	public class HelperMethods : INextSequence
	{
		private IDbService DbService { get; set; }
		public HelperMethods(IDbService dbService)
		{
			this.DbService = dbService;
		}
		public int NextIntSequence(int type, int range)
		{
			IDictionary<string, object> outputParameters;
			var resultSet = this.DbService.ExecuteCommand()
										.SetStoredProcedure("[dbo].[spu_getguid]")
										.SetParameters(
											(@"seq_type", type, ParameterDirection.Input),
											(@"range", range, ParameterDirection.Input),
											(@"next_seq", -1, ParameterDirection.Output)
										)
										.SetTransactionScopeOption(TransactionScopeOption.Suppress)
										.Execute(out outputParameters);
			return (int)outputParameters["next_seq"];
		}
		public long NextLongSequence(int type, int range)
		{
			IDictionary<string, object> outputParameters;
			var resultSet = this.DbService.ExecuteCommand()
										.SetStoredProcedure("[dbo].[spu_getlong]")
										.SetParameters(
											(@"seq_type", type, ParameterDirection.Input),
											(@"range", range, ParameterDirection.Input),
											(@"next_seq", -1L, ParameterDirection.Output)
										)
										.SetTransactionScopeOption(TransactionScopeOption.Suppress)
										.Execute(out outputParameters);
			return (long)outputParameters["next_seq"];
		}
		// TODO: Need to figure out how to get the session user Id.  This is left for security service design where we may use JWT to carry user's claims round a user sessin.
		public static string CurrentUserId => "Testing";
	}
}
