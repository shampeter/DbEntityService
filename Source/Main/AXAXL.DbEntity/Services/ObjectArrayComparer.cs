using System;
using System.Collections.Generic;
using System.Text;

namespace AXAXL.DbEntity.Services
{
	public class ObjectArrayComparer : IEqualityComparer<object[]>
	{
		public bool Equals(object[] x, object[] y)
		{
			if (x == null || y == null)
			{
				throw new ArgumentException("Parameter cannot be null.");
			}
			return this.GetHashCode(x) == this.GetHashCode(y);
		}

		public int GetHashCode(object[] objects)
		{
			if (objects == null)
			{
				throw new ArgumentException("Input object array should not be null");
			}
			if (objects.Length > 8)
			{
				throw new ArgumentException("Too many objects.  Maximum length of array is 8 for this function.");
			}
			if (objects.Length == 1)
			{
				return objects[0].GetHashCode();
			}
			int hash = 17;
			foreach(var obj in objects)
			{
				hash = hash * 31 + obj.GetHashCode();
			}
			return hash;
		}
	}
}
