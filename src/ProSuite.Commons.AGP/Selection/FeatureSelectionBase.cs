using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Selection
{
	public abstract class FeatureSelectionBase
	{
		protected FeatureSelectionBase([NotNull] BasicFeatureLayer basicFeatureLayer)
		{
			BasicFeatureLayer = basicFeatureLayer ??
			                    throw new ArgumentNullException(nameof(basicFeatureLayer));
			FeatureClass = basicFeatureLayer.GetFeatureClass();
		}

		[NotNull]
		public FeatureClass FeatureClass { get; }

		[NotNull]
		public BasicFeatureLayer BasicFeatureLayer { get; }

		public int ShapeDimension => GeometryUtils.GetShapeDimension(GetShapeType());

		public abstract IEnumerable<long> GetOids();

		public abstract IEnumerable<Feature> GetFeatures();

		public abstract int GetCount();

		private GeometryType GetShapeType()
		{
			return GeometryUtils.TranslateEsriGeometryType(BasicFeatureLayer.ShapeType);
		}
	}
}
