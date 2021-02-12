namespace ProSuite.QA.Container.PolygonGrower
{
	public interface INodesDirectedRow : IDirectedRow
	{
		NetNode FromNode { get; set; }
		NetNode ToNode { get; set; }
	}
}
