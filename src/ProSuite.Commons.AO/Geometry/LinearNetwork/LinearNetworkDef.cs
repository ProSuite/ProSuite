﻿using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry.LinearNetwork.Editing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AO.Geometry.LinearNetwork
{
	/// <summary>
	/// The definition of a linear network that must contains one or more edge (polyline)
	/// feature classes and optionally one or more junction (point) feature classes. One
	/// of the junction feature classes can be defined as default junction type which
	/// should be created e.g. by the <see cref="LinearNetworkEditAgent"/> when deemed
	/// necessary.
	/// </summary>
	[CLSCompliant(false)]
	public class LinearNetworkDef
	{
		[CanBeNull] private readonly IFeatureClass _defaultJunctionClass;
		[CanBeNull] private readonly int? _defaultSubtype;

		public LinearNetworkDef([NotNull] IList<LinearNetworkClassDef> networkClassDefinitions,
		                        [CanBeNull] IFeatureClass defaultJunctionClass,
		                        [CanBeNull] int? defaultSubtype = null)
		{
			_defaultJunctionClass = defaultJunctionClass;
			_defaultSubtype = defaultSubtype;

			NetworkClassDefinitions = networkClassDefinitions;
		}

		/// <summary>
		/// Name of the network for logging purposes.
		/// </summary>
		[CanBeNull]
		public string Name { get; set; }

		[NotNull]
		public IList<LinearNetworkClassDef> NetworkClassDefinitions { get; }

		[CanBeNull]
		public IFeatureClass GetDefaultJunctionClass(out int? defaultSubtype)
		{
			defaultSubtype = _defaultSubtype;
			return _defaultJunctionClass;
		}

		public ISpatialReference GetSpatialReference()
		{
			foreach (IFeatureClass featureClass in NetworkClassDefinitions.Select(
				c => c.FeatureClass))
			{
				ISpatialReference sr = DatasetUtils.GetSpatialReference(featureClass);

				if (sr != null)
				{
					return sr;
				}
			}

			return null;
		}

		public bool IsNetworkFeatureClass([NotNull] IFeatureClass featureClass)
		{
			foreach (LinearNetworkClassDef networkClassDef in NetworkClassDefinitions)
			{
				if (DatasetUtils.IsSameObjectClass(featureClass, networkClassDef.FeatureClass))
				{
					return true;
				}
			}

			return false;
		}

		public bool IsEdgeFeatureClass([NotNull] IFeatureClass featureClass)
		{
			if (DatasetUtils.GetShapeType(featureClass) != esriGeometryType.esriGeometryPolyline)
			{
				return false;
			}

			return IsNetworkFeatureClass(featureClass);
		}

		public bool IsJunctionFeatureClass([NotNull] IFeatureClass featureClass)
		{
			if (DatasetUtils.GetShapeType(featureClass) != esriGeometryType.esriGeometryPoint)
			{
				return false;
			}

			return IsNetworkFeatureClass(featureClass);
		}

		public bool IsEdgeFeature(IFeature feature)
		{
			if (DatasetUtils.GetShapeType(feature.Class) != esriGeometryType.esriGeometryPolyline)
			{
				return false;
			}

			foreach (LinearNetworkClassDef edgeClassDef in NetworkClassDefinitions.Where(
				nc => nc.GeometryType == esriGeometryType.esriGeometryPolyline))
			{
				if (! DatasetUtils.IsSameObjectClass(feature.Class, edgeClassDef.FeatureClass))
				{
					continue;
				}

				return edgeClassDef.IsInLinearNetworkClass(feature);
			}

			return false;
		}

		public bool IsJunctionFeature([NotNull] IFeature feature)
		{
			if (DatasetUtils.GetShapeType(feature.Class) != esriGeometryType.esriGeometryPoint)
			{
				return false;
			}

			foreach (LinearNetworkClassDef junctionClassDef in NetworkClassDefinitions.Where(
				nc => nc.GeometryType == esriGeometryType.esriGeometryPoint))
			{
				if (! IsFeatureInNetworkClass(feature, junctionClassDef))
				{
					continue;
				}

				return junctionClassDef.IsInLinearNetworkClass(feature);
			}

			return false;
		}

		public bool IsNetworkFeature([CanBeNull] IFeature feature)
		{
			if (feature == null)
			{
				return false;
			}

			esriGeometryType shapeType = DatasetUtils.GetShapeType(feature.Class);

			if (shapeType != esriGeometryType.esriGeometryPoint &&
			    shapeType != esriGeometryType.esriGeometryPolyline)
			{
				return false;
			}

			foreach (LinearNetworkClassDef networkClassDef in NetworkClassDefinitions)
			{
				if (IsFeatureInNetworkClass(feature, networkClassDef))
				{
					return true;
				}
			}

			return false;
		}

		public override string ToString()
		{
			string defaultJunctionClass = _defaultJunctionClass != null
				                              ? DatasetUtils.GetName(_defaultJunctionClass)
				                              : null;

			return
				$"Network Classes: {StringUtils.Concatenate(NetworkClassDefinitions, Environment.NewLine)}" +
				$"{Environment.NewLine}Default Junctions : {defaultJunctionClass}";
		}

		private static bool IsFeatureInNetworkClass(IFeature feature,
		                                            LinearNetworkClassDef networkClassDef)
		{
			if (! DatasetUtils.IsSameObjectClass(feature.Class, networkClassDef.FeatureClass))
			{
				return false;
			}

			return networkClassDef.IsInLinearNetworkClass(feature);
		}
	}
}