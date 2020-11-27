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
		[NotNull] public IProcessingSymbology Symbology { get; }
		public GeometryType ShapeType { get; }
		public string ShapeFieldName { get; }
		public double XYTolerance { get; }
		public SpatialReference SpatialReference { get; }

		public ProcessingDataset(ProcessDatasetName datasetName, FeatureClass featureClass,
		                         IProcessingSelection processingSelection = null,
		                         IProcessingSymbology processingSymbology = null)
		{
			DatasetName = datasetName.DatasetName;
			WhereClause = datasetName.WhereClause;
			FeatureClass = featureClass ?? throw new ArgumentNullException(nameof(featureClass));
			Selection = processingSelection ?? new NoProcessingSelection();
			Symbology = processingSymbology ?? new NoProcessingSymbology();

			var definition = FeatureClass.GetDefinition(); // bombs on joined FC
			ShapeType = definition.GetShapeType(); // MCT
			ShapeFieldName = definition.GetShapeField(); // MCT
			SpatialReference = definition.GetSpatialReference(); // MCT
			XYTolerance = SpatialReference.XYTolerance;
		}

		public override string ToString()
		{
			if (string.IsNullOrWhiteSpace(WhereClause))
				return DatasetName ?? string.Empty;
			return $"{DatasetName} where {WhereClause}";
		}
	}
}
