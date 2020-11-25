using System;

namespace ProSuite.QA.Container.PolygonGrower
{
	[CLSCompliant(false)]
	public interface INodesDirectedRow : IDirectedRow
	{
		NetNode FromNode { get; set; }
		NetNode ToNode { get; set; }
	}
}
