using System;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Selection;

public abstract class FeatureSelectionBase : TableSelection
{
	protected FeatureSelectionBase([NotNull] BasicFeatureLayer basicFeatureLayer) : base(
		basicFeatureLayer.GetFeatureClass())
	{
		BasicFeatureLayer = basicFeatureLayer ??
		                    throw new ArgumentNullException(nameof(basicFeatureLayer));
	}

	[NotNull]
	public BasicFeatureLayer BasicFeatureLayer { get; }

	public int ShapeDimension => GeometryUtils.GetShapeDimension(GetShapeType());

	private GeometryType GetShapeType()
	{
		return GeometryUtils.TranslateEsriGeometryType(BasicFeatureLayer.ShapeType);
	}
}
