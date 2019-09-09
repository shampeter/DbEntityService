using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.Extensions.Logging;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.EntityGraph;

namespace AXAXL.DbEntity.Services
{
	public class DbQuery<T> : IQuery<T> where T : class, new()
	{
		private IDbServiceOption ServiceOption { get; set; }
		private ILogger Log { get; set; }
		private INodeMap NodeMap { get; set; }
		private IDictionary<Node, NodeProperty[]> Exclusion { get; set; }
		private IDatabaseDriver Driver { get; set; }
		private int TimeoutDurationInSeconds { get; set; }
		private Expression<Func<T, bool>> WhereClause { get; set; }
		internal DbQuery(ILogger log, IDbServiceOption serviceOption, INodeMap nodeMap, IDatabaseDriver driver)
		{
			this.ServiceOption = serviceOption;
			this.Log = log;
			this.NodeMap = nodeMap;
			this.Exclusion = new Dictionary<Node, NodeProperty[]>();
			this.Driver = driver;
			// According to https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.commandtimeout?view=netcore-2.2,
			// default Sql server query timeout is 30 seconds.
			this.TimeoutDurationInSeconds = 30;
		}
		public IQuery<T> Exclude(params Expression<Func<T, dynamic>>[] exclusions)
		{
			var node = this.NodeMap.GetNode(typeof(T));
			var excludedProperties = node.IdentifyMembers<T>(exclusions);
			if (excludedProperties.Length > 0)
			{
				this.Exclusion.Add(node, excludedProperties);
			}
			return this;
		}

		public IQuery<T> Exclude<TObject>(params Expression<Func<TObject, dynamic>>[] exclusions) where TObject : class, new()
		{
			var node = this.NodeMap.GetNode(typeof(TObject));
			var excludedProperties = node.IdentifyMembers<TObject>(exclusions);
			if (excludedProperties.Length > 0)
			{
				this.Exclusion.Add(node, excludedProperties);
			}
			return this;
		}


		public IQuery<T> SetTimeout(int timeoutDurationInSeconds = 30)
		{
			this.TimeoutDurationInSeconds = timeoutDurationInSeconds;
			return this;
		}
		public T[] ToArray(int maxNumOfRow = -1)
		{
			return this.ExecuteQuery(typeof(T), maxNumOfRow).ToArray();
		}

		public IList<T> ToList(int maxNumOfRow = -1)
		{
			return this.ExecuteQuery(typeof(T), maxNumOfRow).ToList();
		}

		public IQuery<T> Where(Expression<Func<T, bool>> whereClause)
		{
			this.WhereClause = whereClause;
			return this;
		}
		private IEnumerable<T> ExecuteQuery(Type entityType, int maxNumOfRow)
		{
			var node = this.NodeMap.GetNode(entityType);
			var connection = string.IsNullOrEmpty(node.DbConnectionName) ? this.ServiceOption.GetDefaultConnectionString() : this.ServiceOption.GetConnectionString(node.DbConnectionName);
			var director = new Director(this.ServiceOption, this.NodeMap, this.Driver, this.Log, this.Exclusion);
			var queryResult = this.Driver.Select(connection, node, this.WhereClause, maxNumOfRow, this.TimeoutDurationInSeconds);
			foreach(var eachEntity in queryResult)
			{
				director.Build<T>(eachEntity, true, true);
			}
			return queryResult;
		}
	}
}
