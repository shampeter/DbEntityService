using System;
using System.Collections.Generic;
using System.Text;

namespace AXAXL.DbEntity.UnitTestLib.Models
{
	public interface INextSequence
	{
		int NextIntSequence(int type, int range);
		long NextLongSequence(int type, int range);
	}
}
