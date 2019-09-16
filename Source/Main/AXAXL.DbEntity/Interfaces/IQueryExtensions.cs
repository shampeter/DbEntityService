using System;
using System.Collections.Generic;

namespace AXAXL.DbEntity.Interfaces
{
	/// <summary>
	/// This class defines placeholding extensions that represents various SQL operatiors, such as IN or LIKE.  These extensions performs not logic but are used
	/// by the library to identify the respective SQL operators in a Lambda where clause.
	/// </summary>
	public static class IQueryExtensions
	{
		public static bool Like(this string target, string pattern)
		{
			return true;
		}
		public static bool In(this int target, IEnumerable<int> list)
		{
			return true;
		}
		public static bool In(this int? target, IEnumerable<int> list)
		{
			return true;
		}
		public static bool In(this long target, IEnumerable<long> list)
		{
			return true;
		}
		public static bool In(this long? target, IEnumerable<long> list)
		{
			return true;
		}
		public static bool In(this DateTime target, IEnumerable<DateTime> list)
		{
			return true;
		}
		public static bool In(this DateTime? target, IEnumerable<DateTime> list)
		{
			return true;
		}
		public static bool In(string target, IEnumerable<string> list)
		{
			return true;
		}
	}
}
