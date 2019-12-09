using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Data.SqlClient;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using BenchmarkDotNet.Attributes;

using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.Benchmarks.Models;

using Autofac;
using Autofac.Extensions.DependencyInjection;

namespace AXAXL.DbEntity.Benchmarks
{

	public class BenchmarkMain
	{
		private string ConnecitonString { get; set; }
		private IDbService DbService { get; set; }

		private const string C_BASELINE_TOP_200_SQL =
			@"Select Top 200 
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
				left outer join t_event_total_marketloss on t_event.event_guid = t_event_total_marketloss.event_guid 
				left outer join t_clr_user_session on t_event.event_guid = t_clr_user_session.event_guid and t_clr_user_session.log_off_dt is null 
				left outer join t_sec_principal on t_clr_user_session.locked_by = t_sec_principal.login_name 
				where t_event.active_ind = 1 
				order by t_event.description";

		private const string C_BASELINE_SQL = 
			@"Select 
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
				left outer join t_event_total_marketloss on t_event.event_guid = t_event_total_marketloss.event_guid 
				left outer join t_clr_user_session on t_event.event_guid = t_clr_user_session.event_guid and t_clr_user_session.log_off_dt is null 
				left outer join t_sec_principal on t_clr_user_session.locked_by = t_sec_principal.login_name 
				where t_event.active_ind = 1 
				order by t_event.description";

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

		private string[] columnNames =
		{
				"event_guid",
				"dt_of_loss_from",
				"total_market_loss",
				"locked_by",
				"log_on_dt",
				"dt_of_loss_to",
				"catstr_id",
				"description",
				"lloyd_reference"
		};
		[GlobalSetup]
		public void GlobalSetup()
		{
			var config = new ConfigurationBuilder()
								.SetBasePath(Directory.GetCurrentDirectory())
								.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
								.AddEnvironmentVariables()
								.Build();
			this.ConnecitonString = config["ConnectionString:CLR"];

			var services = new ServiceCollection()
				.AddLogging(
					c => c
						.AddConsole()
						.SetMinimumLevel(LogLevel.Information)
				)
				.AddMemoryCache()
				;
			var containerBuilder = new ContainerBuilder();
			containerBuilder.RegisterModule(new AutofacModule(config));
			containerBuilder.Populate(services);

			var serviceProvider = new AutofacServiceProvider(containerBuilder.Build());

			#region Commented.  DI without Autofac
			/*
			 *			var serviceProvider = new ServiceCollection()
							.AddLogging(
								c => c
									.AddConsole()
									.SetMinimumLevel(LogLevel.Information)
							)
							.AddSqlDbEntityService(
								option => option
											.AddOrUpdateConnection("SQL_Connection", this.ConnecitonString)
											.SetAsDefaultConnection("SQL_Connection")
											.PrintNodeMapToFile(config.GetValue<string>(@"DbEntity:NodeMapExport"))
							)
							.BuildServiceProvider()
							;
			*/
			#endregion

			this.DbService = serviceProvider
					.GetService<IDbService>()
					.Bootstrap();
		}

		[Benchmark(Baseline = true, Description = "Baseline. Query by direct SQL")]
		public int BaseLine()
		{
			var buffer = new List<BaseLineSQLResult>();
			using (var conn = new SqlConnection(this.ConnecitonString))
			{
				conn.Open();
				var cmd = new SqlCommand(C_BASELINE_SQL, conn);
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						var result = new BaseLineSQLResult();
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

		[Benchmark(Baseline = false, Description = "Query by direct SQL for Top 200")]
		public int DirectQueryForTop200()
		{
			var buffer = new List<BaseLineSQLResult>();
			using (var conn = new SqlConnection(this.ConnecitonString))
			{
				conn.Open();
				var cmd = new SqlCommand(C_BASELINE_TOP_200_SQL, conn);
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						var result = new BaseLineSQLResult();
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
		[Benchmark(Baseline = false, Description = "Query by direct SQL in inner join query")]
		public int InnerJoinQuery()
		{
			var buffer = new List<BaseLineSQLResult>();
			using (var conn = new SqlConnection(this.ConnecitonString))
			{
				conn.Open();
				var cmd = new SqlCommand(C_INNER_JOIN_SQL, conn);
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						var result = new BaseLineSQLResult();
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

		[Benchmark(Baseline = false, Description = "Query by DbEntity with VM")]
		public int QueryByEntityWithVM()
		{
			var query = this.DbService
						.Query<Event>()
						.Where(e => e.IsActive == true)
						.LeftOuterJoin<Event, CLRUserSession>(s => s.LogOffDt == null)
						.OrderBy(e => e.Description)
						;

			var eventList = query.ToList();

			var resultSet = eventList
					.Select(
						p => {
							var lockedSession = p.CLRUserSessionList.FirstOrDefault();
							string userLockingEvent = null;
							DateTime? sessionLockDate = null;
							if (lockedSession != null)
							{
								userLockingEvent = $"{lockedSession.LockedByUser.LastName}, {lockedSession.LockedByUser.FirstName}";
								sessionLockDate = lockedSession.LogOnDt;
							}
							var result = new BaseLineSQLResult
							{
								EventGuid = p.EventGuid,
								DOLFrom = p.DOLFrom,
								DOLTo = p.DOLTo,
								CatstrId = p.CatstrId,
								Description = p.Description,
								LloydReference = p.LloydReference,
								TotalMarketLoss = p.EventTotalMarketLossList.FirstOrDefault()?.TotalMarketLoss,
								LockedBy = userLockingEvent,
								LockedDt = sessionLockDate
							};
							return result;
						})
					.ToList();
			return resultSet.Count;
		}

		[Benchmark(Baseline = false, Description = "Query by DbEntity with VM for Top 200")]
		public int QueryByEntityWithVMForTop200()
		{
			var query = this.DbService
						.Query<Event>()
						.Where(e => e.IsActive == true)
						.LeftOuterJoin<Event, CLRUserSession>(s => s.LogOffDt == null)
						.OrderBy(e => e.Description)
						;

			var eventList = query.ToList(200);

			var resultSet = eventList
					.Select(
						p => {
							var lockedSession = p.CLRUserSessionList.FirstOrDefault();
							string userLockingEvent = null;
							DateTime? sessionLockDate = null;
							if (lockedSession != null)
							{
								userLockingEvent = $"{lockedSession.LockedByUser.LastName}, {lockedSession.LockedByUser.FirstName}";
								sessionLockDate = lockedSession.LogOnDt;
							}
							var result = new BaseLineSQLResult
							{
								EventGuid = p.EventGuid,
								DOLFrom = p.DOLFrom,
								DOLTo = p.DOLTo,
								CatstrId = p.CatstrId,
								Description = p.Description,
								LloydReference = p.LloydReference,
								TotalMarketLoss = p.EventTotalMarketLossList.FirstOrDefault()?.TotalMarketLoss,
								LockedBy = userLockingEvent,
								LockedDt = sessionLockDate
							};
							return result;
						})
					.ToList();
			return resultSet.Count;
		}
		[Benchmark(Baseline = false, Description = "Query by DbEntity without VM")]
		public int QueryByEntityWithoutVM()
		{
			var query = this.DbService
						.Query<Event>()
						.Where(e => e.IsActive == true)
						.LeftOuterJoin<Event, CLRUserSession>(s => s.LogOffDt == null)
						.OrderBy(e => e.Description)
						;

			var eventList = query.ToList();

			return eventList.Count;
		}

		[Benchmark(Baseline = false, Description = "Query by DbEntity without Children")]
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

		[Benchmark(Baseline = false, Description = "Query by DbEntity with only Mkt Loss")]
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

		[Benchmark(Baseline = false, Description = "Query by DbEntity with only User Session")]
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

		[Benchmark(Baseline = false, Description = "Query by DbEntity Exec Cmd Dyn Result")]
		public int QueryByExecCmd()
		{
			var resultSet = this.DbService
							.ExecuteCommand()
							.SetCommand(C_BASELINE_SQL)
							.Execute(out IDictionary<string, object> output);
			return resultSet.Count();
		}

		[Benchmark(Baseline = false, Description = "Query by DbEntity with Inner Join")]
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
	}
}
