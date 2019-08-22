using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.Services;
using AXAXL.DbEntity.MSSql;
using AXAXL.DbEntity.EntityGraph;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class DbEntityServiceExtensions
	{
		public static IServiceCollection AddSqlDbEntityService(this IServiceCollection service, Action<IDbServiceOption> config)
		{
			service
				.AddSingleton<IDatabaseDriver, MSSqlDriver>()
				.AddSingleton<INodeMap, NodeMap>()
				.AddSingleton<IMSSqlGenerator, MSSqlGenerator>()
				.AddSingleton<IDbService, DbService>()
				.AddSingleton<IDbServiceOption, DbServiceOption>((provider) => {
					var option = new DbServiceOption();
					config?.Invoke(option);
					return option;
				})
			;
			return service;
		}
	}
}
