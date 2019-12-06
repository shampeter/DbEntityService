using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using BenchmarkDotNet.Attributes;
using System.Data.SqlClient;

namespace AXAXL.DbEntity.Benchmarks
{

	public class BenchmarkMain
	{
		private string ConnecitonString { get; set; }
		private string baselineSql = 
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

		[GlobalSetup]
		public void GlobalSetup()
		{
			var config = new ConfigurationBuilder()
								.SetBasePath(Directory.GetCurrentDirectory())
								.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
								.AddEnvironmentVariables()
								.Build();
			this.ConnecitonString = config["ConnectionString:CLR"];
		}

		/// <summary>
		/// Following is the baseline SQL.
		/// <code>
		/// Select
		/// t_event.event_guid,
		///		t_event.dt_of_loss_from, 
		///		t_event_total_marketloss.total_market_loss, 
		///		t_sec_principal.last_name + ', ' +  t_sec_principal.first_name as locked_by, 
		///		t_clr_user_session.log_on_dt,
		///		t_event.dt_of_loss_to,
		///		t_event.catstr_id,
		///		t_event.description,
		///		t_event.lloyd_reference
		///		from t_event
		///		left outer join t_event_total_marketloss on t_event.event_guid = t_event_total_marketloss.event_guid
		///		left outer join t_clr_user_session on t_event.event_guid = t_clr_user_session.event_guid and t_clr_user_session.log_off_dt is null
		///		left outer join t_sec_principal on t_clr_user_session.locked_by = t_sec_principal.login_name
		///		where t_event.active_ind = 1
		///		order by t_event.description
		/// </code>
		/// </summary>
		[Benchmark(Baseline = true, Description = "Baseline. Query by direct SQL")]
		public void BaseLine()
		{
			
		}
	}
}
