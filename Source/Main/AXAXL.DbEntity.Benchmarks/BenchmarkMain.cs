using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Data.SqlClient;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.CsProj;

using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.Benchmarks.Models;

namespace AXAXL.DbEntity.Benchmarks
{
	[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
	[CategoriesColumn]
	[Config(typeof(Config))]
	[MemoryDiagnoser]
	public class BenchmarkMain : BenchmarkBase
	{

		private const string C_INNER_JOIN_SQL =
			@"
			Select
				t_event.event_guid,
				t_event.dt_of_loss_from,
				t_event_total_marketloss.total_market_loss,
				t_sec_principal.last_name + ', ' +  t_sec_principal.first_name as locked_by,
				t_clr_user_session.log_on_dt,
				t_event.dt_of_loss_to,
				t_event.catstr_id,
				t_event.description,
				t_event.lloyd_reference
			from t_event
				inner join t_event_total_marketloss on t_event.event_guid = t_event_total_marketloss.event_guid
				inner join t_clr_user_session on t_event.event_guid = t_clr_user_session.event_guid
				inner join t_sec_principal on t_clr_user_session.locked_by = t_sec_principal.login_name
			where t_event.active_ind = 1
			order by t_event.description";

		private const string C_CLR_USER_SESSION_SQL =
			@"
			select
				[s].[user_session_guid],
				[s].[event_guid],
				[s].[locked_by],
				[p].[first_name],
				[p].[last_name],
				[s].[added_dt],
				[s].[added_app],
				[s].[added_by],
				[s].[modify_dt],
				[s].[modify_app],
				[s].[modify_by],
				[s].[version]
			from
				 [t_clr_user_session] [s]
			left outer join [t_sec_principal] [p] on [p].[login_name] = [s].[locked_by]
			";

		private class Config : ManualConfig
		{
			public Config()
			{
				this.Add(new AnyCategoriesFilter(new[] { "Full", "Top 200" }));
				//this.Add(new AnyCategoriesFilter(new[] { "Full" }));

				#region EtwProfiler
				/*				
				 *	Not working.  Don't know why but PerfView said that the trace file was 30% broken due to 64 bit runtime but
				 *	the benchmark was certainly running in x86 target.

				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					// 2020-01-11. EtwProfiler (analyze performance using PerfView) is only available on Windows for the time being.
					this.Add(new EtwProfiler());
					var dotnetCli32bit = NetCoreAppSettings
								.NetCoreApp31
								.WithCustomDotNetCliPath(@"C:\Program Files (x86)\dotnet\dotnet.exe", "32 bit cli");

					this.Add(Job.Default
						.With(Platform.X86)
						.With(CsProjCoreToolchain.From(dotnetCli32bit))
						.WithId("32 bit cli")
						);
				}
				 */
				#endregion
			}
		}

		[GlobalSetup]
		public void GlobalSetup()
		{
			this.Setup();
		}

		[BenchmarkCategory("Full"), Benchmark(Baseline = true, Description = "Baseline. Query by direct SQL")]
		public int BaseLine()
		{
			List<BaseLineSQLResultVM> buffer = BaselineSQLQuery();
			return buffer.Count;
		}

		[BenchmarkCategory("Full"), Benchmark(Baseline = false, Description = "Query by DbEntity Exec Command")]
		public int QueryByExecCmd()
		{
			IEnumerable<dynamic> resultSet = DbEntityExecCmd();
			return resultSet.Count();
		}

		[BenchmarkCategory("Diagnostics"), Benchmark(Baseline = false, Description = "Query by DbEntity without Optimization")]
		public int QueryByEntityWithVMWithNoOptimization()
		{
			var resultSet = this.DbEntityQuery(RetrievalStrategies.OneEntityAtATimeInSequence, -1);
			return resultSet.Count;
		}

		[BenchmarkCategory("Diagnostics"), Benchmark(Baseline = false, Description = "Query by DbEntity with Optimization 1")]
		public int QueryByEntityWithVMWithOptimization1()
		{
			var resultSet = this.DbEntityQuery(RetrievalStrategies.OneEntityAtATimeInParallel, -1);
			return resultSet.Count;
		}

		[BenchmarkCategory("Full"), Benchmark(Baseline = false, Description = "Query by DbEntity with Optimization 2")]
		public int QueryByEntityWithVMWithOptimization2()
		{
			var resultSet = this.DbEntityQuery(RetrievalStrategies.AllEntitiesAtOnce, -1);
			return resultSet.Count;
		}

		[BenchmarkCategory("Top 200"), Benchmark(Baseline = true, Description = "Baseline. Query by direct SQL")]
		public int Top200BaseLine()
		{
			List<BaseLineSQLResultVM> buffer = BaselineSQLQuery(200);
			return buffer.Count;
		}

		[BenchmarkCategory("Top 200"), Benchmark(Baseline = false, Description = "Query by DbEntity Exec Command")]
		public int Top200QueryByExecCmd()
		{
			IEnumerable<dynamic> resultSet = DbEntityExecCmd(200);
			return resultSet.Count();
		}

		[BenchmarkCategory("Diagnostics"), Benchmark(Baseline = false, Description = "Query by DbEntity without Optimization")]
		public int Top200QueryByEntityWithVMWithNoOptimization()
		{
			var resultSet = this.DbEntityQuery(RetrievalStrategies.OneEntityAtATimeInSequence, 200);
			return resultSet.Count;
		}

		[BenchmarkCategory("Diagnostics"), Benchmark(Baseline = false, Description = "Query by DbEntity with Optimization 1")]
		public int Top200QueryByEntityWithVMWithOptimization1()
		{
			var resultSet = this.DbEntityQuery(RetrievalStrategies.OneEntityAtATimeInParallel, 200);
			return resultSet.Count;
		}

		[BenchmarkCategory("Top 200"), Benchmark(Baseline = false, Description = "Query by DbEntity with Optimization 2")]
		public int Top200QueryByEntityWithVMWithOptimization2()
		{
			var resultSet = this.DbEntityQuery(RetrievalStrategies.AllEntitiesAtOnce, 200);
			return resultSet.Count;
		}

		#region Kept for later diagnostics
		[BenchmarkCategory("Diagnostics"), Benchmark(Baseline = false, Description = "Query by DbEntity without Children with Optimization 2")]
		public int QueryByEntityWithoutChild()
		{
			var query = this.DbService
						.Query<Event>()
						.Where(e => e.IsActive == true)
						.Exclude(e => e.EventTotalMarketLossList, e => e.CLRUserSessionList)
						.OrderBy(e => e.Description)
						;

			var eventList = query.ToList();

			return eventList.Count;
		}

		[BenchmarkCategory("Diagnostics"), Benchmark(Baseline = false, Description = "Query by DbEntity with only Mkt Loss with Optimization 2")]
		public int QueryByEntityWithOnlyMktLoss()
		{
			var query = this.DbService
						.Query<Event>()
						.Where(e => e.IsActive == true)
						.Exclude(e => e.CLRUserSessionList)
						.OrderBy(e => e.Description)
						;

			var eventList = query.ToList();

			return eventList.Count;
		}

		[BenchmarkCategory("Diagnostics"), Benchmark(Baseline = false, Description = "Query by DbEntity with only User Session with Optimization 2")]
		public int QueryByEntityWithOnlyUserSessn()
		{
			var query = this.DbService
						.Query<Event>()
						.Where(e => e.IsActive == true)
						.Exclude(e => e.EventTotalMarketLossList)
						.LeftOuterJoin<Event, CLRUserSession>(s => s.LogOffDt == null)
						.OrderBy(e => e.Description)
						;

			var eventList = query.ToList();

			return eventList.Count;
		}

		[BenchmarkCategory("Diagnostics"), Benchmark(Baseline = false, Description = "Query by DbEntity with Inner Join")]
		public int QueryByEntityWithInnerJoin()
		{
			var query = this.DbService
						.Query<Event>()
						.Where(e => e.IsActive == true)
						.InnerJoin<Event, CLRUserSession>(s => s.EventGuid == s.EventGuid)
						.InnerJoin<Event, EventTotalMarketLoss>(s => s.EventGuid == s.EventGuid)
						.OrderBy(e => e.Description)
						;

			var eventList = query.ToList();

			return eventList.Count;
		}

		[BenchmarkCategory("Diagnostics"), Benchmark(Baseline = false, Description = "Query by DbEntity On CLR User Session")]
		public int QueryByEntityOnCLRUserSession()
		{
			var result = this.DbService
								.Query<CLRUserSession>()
								.ToList();
			return result.Count;
		}
		[BenchmarkCategory("Diagnostics"), Benchmark(Baseline = true, Description = "Query by direct SQL on CLR User Session")]
		public int DirectSQLOnCLRUserSession()
		{
			var buffer = new List<CLRUserSQLResultVM>();
			using (var conn = new SqlConnection(ConnecitonString))
			{
				conn.Open();
				var cmd = new SqlCommand(C_CLR_USER_SESSION_SQL, conn);
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						var result = new CLRUserSQLResultVM();

						result.UserSessionGuid = reader.GetInt32(0);
						result.EventGuid = reader.GetInt32(1);
						result.LockedBy = reader.GetString(2);
						result.FirstName = reader.GetString(3);
						result.LastName = reader.GetString(4);
						result.AddedDt = reader.GetDateTime(5);
						result.AddedApp = reader.GetString(6);
						result.AddedBy = reader.GetString(7);
						result.ModifyDt = reader.GetDateTime(8);
						result.ModifyApp = reader.GetString(9);
						result.ModifyBy = reader.GetString(10);
						result.Version = reader.GetInt32(11);

						buffer.Add(result);
					}
				}
			}
			return buffer.Count;
		}

		[BenchmarkCategory("Diagnostics"), Benchmark(Baseline = false, Description = "Query by direct SQL in inner join query")]
		public int InnerJoinQuery()
		{
			var buffer = new List<BaseLineSQLResultVM>();
			using (var conn = new SqlConnection(ConnecitonString))
			{
				conn.Open();
				var cmd = new SqlCommand(C_INNER_JOIN_SQL, conn);
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						var result = new BaseLineSQLResultVM();
						result.EventGuid = reader.GetInt32(0);
						result.DOLFrom = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);
						result.TotalMarketLoss = reader.IsDBNull(2) ? (double?)null : Convert.ToDouble(reader.GetDecimal(2));
						result.LockedBy = reader.IsDBNull(3) ? null : reader.GetString(3);
						result.LockedDt = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);
						result.DOLTo = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5);
						result.CatstrId = reader.IsDBNull(6) ? null : reader.GetString(6);
						result.Description = reader.IsDBNull(7) ? null : reader.GetString(7);
						result.LloydReference = reader.IsDBNull(8) ? null : reader.GetString(8);
						buffer.Add(result);
					}
				}
			}
			return buffer.Count;
		}
		#endregion
	}
}
