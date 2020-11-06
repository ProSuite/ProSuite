using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Processing.Domain
{
	public class ProcessingDataset
	{
		public string DatasetName { get; }
		[CanBeNull] public string WhereClause { get; }
		[NotNull] public FeatureClass FeatureClass { get; }
		[NotNull] public IProcessingSelection Selection { get; }
		public GeometryType ShapeType { get; }
		public double XYTolerance { get; }
		public SpatialReference SpatialReference { get; }

		public ProcessingDataset(ProcessDatasetName datasetName, FeatureClass featureClass, IProcessingSelection featureLayer = null)
		{
			DatasetName = datasetName.DatasetName;
			WhereClause = datasetName.WhereClause;
			FeatureClass = featureClass ?? throw new ArgumentNullException(nameof(featureClass));
			Selection = featureLayer ?? new NoProcessingSelection();

			var definition = FeatureClass.GetDefinition(); // bombs on joined FC
			ShapeType = definition.GetShapeType(); // MCT
			SpatialReference = definition.GetSpatialReference(); // MCT
			XYTolerance = SpatialReference.XYTolerance;
		}
	}
}