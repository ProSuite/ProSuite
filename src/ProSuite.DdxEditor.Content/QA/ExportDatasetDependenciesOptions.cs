namespace ProSuite.DdxEditor.Content.QA
{
	public class ExportDatasetDependenciesOptions
	{
		public ExportDatasetDependenciesOptions(
			bool exportBidirectionalDependenciesAsUndirectedEdges,
			bool exportModelsAsParentNodes,
			bool includeSelfDependencies)
		{
			ExportBidirectionalDependenciesAsUndirectedEdges =
				exportBidirectionalDependenciesAsUndirectedEdges;
			ExportModelsAsParentNodes = exportModelsAsParentNodes;
			IncludeSelfDependencies = includeSelfDependencies;
		}

		public bool ExportBidirectionalDependenciesAsUndirectedEdges { get; private set; }

		public bool ExportModelsAsParentNodes { get; private set; }

		public bool IncludeSelfDependencies { get; private set; }
	}
}