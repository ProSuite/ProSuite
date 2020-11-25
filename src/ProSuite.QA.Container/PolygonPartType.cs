using System;

namespace ProSuite.QA.Container
{
	[Flags]
	public enum PolygonPartType
	{
		Full = 1,
		ExteriorRing = 2,
		Ring = 4
	}
}
