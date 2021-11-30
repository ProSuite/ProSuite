using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto
{
	public class FeatureClassSelection
	{
		/// <summary>
		/// The feature class.
		/// </summary>
		[NotNull]
		public FeatureClass FeatureClass { get; }

		/// <summary>
		/// The list of features from <see cref="FeatureClass"/>. They do not all necessarily belong to the same layer.
		/// </summary>
		[NotNull]
		public List<Feature> Features { get; }

		/// <summary>
		/// The (top-most) layer which references the FeatureClass of the selected features.
		/// </summary>
		[CanBeNull]
		public BasicFeatureLayer FeatureLayer { get; }

		public int FeatureCount => Features.Count;

		public int ShapeDimension
		{
			get
			{
				GeometryType shapeType =
					FeatureLayer != null
						? GeometryUtils.TranslateEsriGeometryType(FeatureLayer.ShapeType)
						: FeatureClass.GetDefinition().GetShapeType();

				return GetShapeDimension(shapeType);
			}
		}

		public FeatureClassSelection([NotNull] FeatureClass featureClass,
		                             [NotNull] List<Feature> features,
		                             [CanBeNull] BasicFeatureLayer featureLayer)
		{
			FeatureClass = featureClass;
			Features = features;
			FeatureLayer = featureLayer;
		}

		private static int GetShapeDimension(GeometryType geometryType)
		{
			switch (geometryType)
			{
				case GeometryType.Point:
				case GeometryType.Multipoint:
					return 0;
				case GeometryType.Polyline:
					return 1;
				case GeometryType.Polygon:
				case GeometryType.Multipatch:
				case GeometryType.Envelope:
					return 2;

				default:
					throw new ArgumentOutOfRangeException(nameof(geometryType), geometryType,
					                                      $"Unexpected geometry type: {geometryType}");
			}
		}
	}
}
