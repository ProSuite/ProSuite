using System;

namespace ProSuite.DomainModel.Core.DataModel
{
	public interface IRegisteredGdbObject
	{
		bool Deleted { get; }

		DateTime? DeletionRegisteredDate { get; }

		void RegisterDeleted();

		void RegisterExisting();
	}
}