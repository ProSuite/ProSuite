using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Selection;

// todo daro rename to LayerSelection
public abstract class FeatureSelectionBase : TableSelection
{
	protected FeatureSelectionBase([NotNull] BasicFeatureLayer basicFeatureLayer)
		: base(basicFeatureLayer.GetTable())
	{
		BasicFeatureLayer = basicFeatureLayer ??
		                    throw new ArgumentNullException(nameof(basicFeatureLayer));
	}

	[NotNull]
	public FeatureClass FeatureClass => (FeatureClass) Table;

	[NotNull]
	public BasicFeatureLayer BasicFeatureLayer { get; }

	public int ShapeDimension => GeometryUtils.GetShapeDimension(GetShapeType());

	public abstract IEnumerable<Feature> GetFeatures();

	private GeometryType GetShapeType()
	{
		return GeometryUtils.TranslateEsriGeometryType(BasicFeatureLayer.ShapeType);
	}
}
