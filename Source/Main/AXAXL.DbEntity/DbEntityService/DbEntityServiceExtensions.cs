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
// TODO: Figure out how to build shortcut to just call, like, AddDbEntityService((c) => c.Addconnection ...)
	public static class DbEntityServiceExtensions
	{
		//public static IServiceCollection AddDbEntityService(this IServiceCollection service, Action<IDbServcieOption> config)
		//{
		//	service
		//		.AddSingleton<IDatabaseDriver, MSSqlDriver>()
		//		.AddSingleton<INodeMap, NodeMap>()
		//		.AddSingleton<IMSSqlGenerator, MSSqlGenerator>()
		//		.AddSingleton<IDbService, DbService>()
		//}
	}
	//public class DbServiceProvider : IServiceProvider
	//{
	//	public object GetService(Type serviceType)
	//	{
	//		if (typeof(IDbService).IsAssignableFrom(serviceType))
	//		{
	//			return new DbService();
	//		}
	//	}
	//}
}
