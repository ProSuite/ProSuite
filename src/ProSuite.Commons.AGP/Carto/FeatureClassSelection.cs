using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AGP.Carto
{
	// todo daro move to .\Selection?
	/// <summary>
	/// Encapsulates a set of selected features that belong to the same FeatureClass and possibly
	/// to the same layer.
	/// </summary>
	public class FeatureClassSelection
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private List<Feature> _features;
		private List<long> _objectIds;

		private readonly GeometryType _shapeType;
		private readonly SpatialReference _outputSpatialReference;

		/// <summary>
		/// Initializes a new instance of the <see cref="FeatureClassSelection"/> class.
		/// This method must be called on the Main CIM Thread.
		/// </summary>
		/// <param name="featureClass"></param>
		/// <param name="features"></param>
		/// <param name="featureLayer"></param>
		/// <param name="outputSpatialReference"></param>
		public FeatureClassSelection([NotNull] FeatureClass featureClass,
		                             [NotNull] List<Feature> features,
		                             [CanBeNull] BasicFeatureLayer featureLayer,
		                             [CanBeNull] SpatialReference outputSpatialReference)
			: this(featureClass, featureLayer, outputSpatialReference)
		{
			_features = features;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FeatureClassSelection"/> class.
		/// This method must be called on the Main CIM Thread.
		/// </summary>
		/// <param name="featureClass"></param>
		/// <param name="objectIds"></param>
		/// <param name="featureLayer"></param>
		/// <param name="outputSpatialReference"></param>
		public FeatureClassSelection([NotNull] FeatureClass featureClass,
		                             [NotNull] List<long> objectIds,
		                             [CanBeNull] BasicFeatureLayer featureLayer,
		                             [CanBeNull] SpatialReference outputSpatialReference)
			: this(featureClass, featureLayer, outputSpatialReference)
		{
			_objectIds = objectIds;
		}

		private FeatureClassSelection([NotNull] FeatureClass featureClass,
		                              [CanBeNull] BasicFeatureLayer basicFeatureLayer,
		                              [CanBeNull] SpatialReference outputSpatialReference)
		{
			FeatureClass = featureClass;
			BasicFeatureLayer = basicFeatureLayer;

			_shapeType = GetShapeType();
			_outputSpatialReference = outputSpatialReference;
		}

		// todo daro drop obvious summaries
		/// <summary>
		/// The feature class.
		/// </summary>
		[NotNull]
		public FeatureClass FeatureClass { get; }

		/// <summary>
		/// The (top-most) layer which references the FeatureClass of the selected features.
		/// </summary>
		[CanBeNull]
		public BasicFeatureLayer BasicFeatureLayer { get; }

		public int FeatureCount => _objectIds?.Count ?? _features.Count;

		public IReadOnlyList<long> ObjectIds
		{
			get
			{
				if (_objectIds == null)
				{
					_objectIds = new List<long>();

					foreach (Feature feature in GetFeatures())
					{
						_objectIds.Add(feature.GetObjectID());
					}
				}

				return _objectIds.AsReadOnly();
			}
		}

		public int ShapeDimension => GetShapeDimension(_shapeType);

		/// <summary>
		/// The list of features from <see cref="FeatureClass"/>. They do not all necessarily belong to the same layer.
		/// Must be called on a CIM thread.
		/// </summary>
		[NotNull]
		public IEnumerable<Feature> GetFeatures()
		{
			if (_features == null)
			{
				Assert.NotNull(_outputSpatialReference, "No spatial reference");

				_features = GdbQueryUtils.GetFeatures(
					                         FeatureClass, _objectIds,
					                         _outputSpatialReference, false)
				                         .ToList();

				if (_features.Count != _objectIds.Count)
				{
					_msg.DebugFormat(
						"FeatureClassSelection: Some features of might have been deleted. FeatureCount: {0}, ObjectIdCount: {1}",
						_features.Count, _objectIds.Count);
				}
			}

			return _features;
		}

		private GeometryType GetShapeType()
		{
			return BasicFeatureLayer != null
				       ? GeometryUtils.TranslateEsriGeometryType(BasicFeatureLayer.ShapeType)
				       : FeatureClass.GetDefinition().GetShapeType();
		}

		// todo daro to utils?
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

		public override string ToString()
		{
			_objectIds.Sort();

			string oids = StringUtils.Concatenate(_objectIds, "; ");

			if (BasicFeatureLayer != null)
			{
				return $"{BasicFeatureLayer.Name}, {oids}";
			}

			return $"{FeatureClass.GetDefinition().GetName()}, {oids}";
		}
	}
}
