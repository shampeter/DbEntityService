using System;
using System.Collections.Generic;
using System.Text;

namespace AXAXL.DbEntity.Interfaces
{
	public class DbUpdateConcurrencyException : Exception
	{
		public DbUpdateConcurrencyException(int rowCount, string sqlStatement, string parameters)
			: base($"{rowCount} row returned from sql {sqlStatement} with where clause parameter being {parameters}")
		{
		}
	}
}
