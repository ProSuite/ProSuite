using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Processing.AGP.Core.Utils;

namespace ProSuite.Processing.AGP.Core.Domain;

public class ProcessingDataset
{
	public string DatasetName { get; }
	[CanBeNull] public string WhereClause { get; }
	[NotNull] public FeatureClass FeatureClass { get; }
	[NotNull] public IProcessingSelection Selection { get; }
	[NotNull] public IProcessingSymbology Symbology { get; }
	public GeometryType ShapeType { get; }
	public string ShapeFieldName { get; }
	public int ShapeFieldIndex { get; }
	public double XYTolerance { get; }
	public SpatialReference SpatialReference { get; }

	public ProcessingDataset(string datasetName, FeatureClass featureClass, string whereClause,
	                         IProcessingSelection processingSelection = null,
	                         IProcessingSymbology processingSymbology = null)
	{
		DatasetName = datasetName ?? throw new ArgumentNullException(nameof(datasetName));
		FeatureClass = featureClass ?? throw new ArgumentNullException(nameof(featureClass));
		WhereClause = whereClause; // can be null
		Selection = processingSelection ?? new NoProcessingSelection();
		Symbology = processingSymbology ?? new NoProcessingSymbology();

		if (featureClass.IsJoinedTable())
		{
			// GetDefinition() fails, but GetBaseTable(fc).GetDefinition() works;
			// however, feature[field] = value fails if feature from joined layer
			// and I know of no workaround...
			throw new NotSupportedException($"{datasetName}: Layers with Joins are not supported");
		}

		//var definition = FeatureClass.GetDefinition(); // bombs on joined FC
		var  baseTable = ProProcessingUtils.GetBaseTable(FeatureClass);
		using var definition = baseTable.GetDefinition();
		ShapeType = definition.GetShapeType(); // MCT
		ShapeFieldName = definition.GetShapeField(); // MCT
		ShapeFieldIndex = definition.FindField(ShapeFieldName); // MCT
		SpatialReference = definition.GetSpatialReference(); // MCT
		XYTolerance = SpatialReference.XYTolerance;

		// Dispose the baseTable (but not the FeatureClass that was passed in)
		if (baseTable.Handle != FeatureClass.Handle)
		{
			baseTable.Dispose();
		}
	}

	public int GetFieldIndex(string fieldName)
	{
		if (fieldName == null)
			throw new ArgumentNullException(nameof(fieldName));

		var baseTable = ProProcessingUtils.GetBaseTable(FeatureClass);
		var definition = baseTable.GetDefinition(); // bombs with joined tables
		int result = definition.FindField(fieldName);

		if (result < 0)
		{
			string datasetName = FeatureClass.GetName();
			throw new CartoConfigException(
				$"Field '{fieldName}' does not exist on {datasetName}");
		}

		return result;
	}

	public ProcessingDataset Restrict(string whereClause)
	{
		if (string.IsNullOrWhiteSpace(whereClause)) return this;

		if (! string.IsNullOrEmpty(WhereClause))
		{
			whereClause = string.Concat("(", WhereClause, ") AND (", whereClause, ")");
		}

		return new ProcessingDataset(DatasetName, FeatureClass, whereClause,
		                             Selection, Symbology);
	}

	public override string ToString()
	{
		if (string.IsNullOrWhiteSpace(WhereClause))
			return DatasetName ?? string.Empty;
		return $"{DatasetName} where {WhereClause}";
	}
}
