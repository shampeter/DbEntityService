﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.Benchmarks.Models;

using Autofac;
using Autofac.Extensions.DependencyInjection;

namespace AXAXL.DbEntity.Benchmarks
{
	public class BenchmarkBase
	{
		private const string C_BASELINE_SQL =
			@"Select {0}
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
		protected static string ConnecitonString { get; set; }
		protected IDbService DbService { get; set; }

		protected void Setup()
		{
			var config = new ConfigurationBuilder()
								.SetBasePath(Directory.GetCurrentDirectory())
								.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
								.AddEnvironmentVariables()
								.Build();
			ConnecitonString = config["ConnectionString:CLR"];

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
		public static void PrintTotalSampleSize()
		{
			var config = new ConfigurationBuilder()
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
					.AddEnvironmentVariables()
					.Build();
			ConnecitonString = config["ConnectionString:CLR"];
			var total = new BenchmarkBase().BaselineSQLQuery().Count();
			Console.WriteLine("Test data size = {0} rows", total);

		}
		protected List<BaseLineSQLResultVM> BaselineSQLQuery(int maxRow = -1)
		{
			string query = FormatBaseQuery(maxRow);
			var buffer = new List<BaseLineSQLResultVM>();
			using (var conn = new SqlConnection(ConnecitonString))
			{
				conn.Open();
				var cmd = new SqlCommand(query, conn);
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

			return buffer;
		}

		protected IEnumerable<dynamic> DbEntityExecCmd(int maxRow = -1)
		{
			var query = this.FormatBaseQuery(maxRow);
			var resultSet = this.DbService
							.ExecuteCommand()
							.SetCommand(query)
							.Execute(out IDictionary<string, object> output);
			return resultSet;
		}

		protected string FormatBaseQuery(int maxRow = -1)
		{
			return String.Format(C_BASELINE_SQL, maxRow > 0 ? $"TOP {maxRow}" : String.Empty);
		}

		protected List<BaseLineSQLResultVM> DbEntityQuery(RetrievalStrategies strategies, int maxRow = -1)
		{
			var query = this.DbService
						.Query<Event>()
						.Where(e => e.IsActive == true)
						.LeftOuterJoin<Event, CLRUserSession>(s => s.LogOffDt == null)
						.OrderBy(e => e.Description)
						;

			var eventList = query.ToList(maxRow, strategies);

			List<BaseLineSQLResultVM> resultSet = this.EventToVM(eventList);
			return resultSet;
		}
		protected List<BaseLineSQLResultVM> EventToVM(IList<Event> eventList)
		{
			return eventList
					.Select(
						p =>
						{
							var lockedSession = p.CLRUserSessionList.FirstOrDefault();
							string userLockingEvent = null;
							DateTime? sessionLockDate = null;
							if (lockedSession != null)
							{
								userLockingEvent = $"{lockedSession.LockedByUser.LastName}, {lockedSession.LockedByUser.FirstName}";
								sessionLockDate = lockedSession.LogOnDt;
							}
							var result = new BaseLineSQLResultVM
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
		}
	}
}
