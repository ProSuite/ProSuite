namespace ProSuite.DomainModel.AGP.Workflow;

public interface IWorkUnit
{
	int ReleaseCycleId { get; }

	int? MonthOfRevision { get; }

	int? YearOfRevision { get; }

	int? MonthOfOrigin { get; }

	int? YearOfOrigin { get;  }

	int? ObjectOrigin { get;  }

	string ObjectOriginDomainName { get; }
}
