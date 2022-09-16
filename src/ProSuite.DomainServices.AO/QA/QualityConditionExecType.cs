using System;

namespace ProSuite.DomainServices.AO.QA
{
	[Flags]
	public enum QualityConditionExecType
	{
		Mixed = 0,
		NonContainer = 1,
		Container = 2,
		TileParallel = 4
	}
}
