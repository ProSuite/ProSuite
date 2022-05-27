using System;

namespace ProSuite.DomainServices.AO.QA
{
	[Flags]
	public enum QualityConditionExecType
	{
		NonContainer = 1,
		Container = 2,
		TileParallel = 4
	}
}
