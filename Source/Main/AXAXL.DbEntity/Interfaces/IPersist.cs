using System;
using System.Text;

namespace AXAXL.DbEntity.Interfaces
{
	public interface IPersist
	{
		IPersist Submit(Func<IChangeSet, IChangeSet> submitChangeSet);
		int Commit();
	}
}
