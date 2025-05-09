using System;
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

		public bool HasDefaultJunctionClass => _defaultJunctionClass != null;

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

		public bool IsEdgeFeature([NotNull] IFeature feature,
		                          bool ignoreWhereClause = false)
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

				return ignoreWhereClause || edgeClassDef.IsInLinearNetworkClass(feature);
			}

			return false;
		}

		/// <summary>
		/// Determines if the specified feature is a junction in the linear network. This requires
		/// not just being part of a network feature class but also conforming to a potential where
		/// clause.
		/// </summary>
		/// <param name="feature"></param>
		/// <returns></returns>
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

				if (junctionClassDef.IsInLinearNetworkClass(feature))
				{
					return true;
				}
			}

			return false;
		}

		public bool IsSplittingJunction(IFeature feature)
		{
			if (DatasetUtils.GetShapeType(feature.Class) != esriGeometryType.esriGeometryPoint)
			{
				return false;
			}

			foreach (LinearNetworkClassDef junctionClassDef in NetworkClassDefinitions.Where(
				         nc => nc.GeometryType == esriGeometryType.esriGeometryPoint))
			{
				if (! junctionClassDef.Splitting)
				{
					continue;
				}

				// The junction is splitting, if it really is part of the network definition
				if (IsFeatureInNetworkClass(feature, junctionClassDef))
				{
					return true;
				}
			}

			return false;
		}

		public bool IsSplittingEdge(IFeature feature)
		{
			if (DatasetUtils.GetShapeType(feature.Class) != esriGeometryType.esriGeometryPolyline)
			{
				return false;
			}

			foreach (LinearNetworkClassDef edgeClassDef in NetworkClassDefinitions.Where(
				         nc => nc.GeometryType == esriGeometryType.esriGeometryPolyline))
			{
				if (! edgeClassDef.Splitting)
				{
					continue;
				}

				// The edge is split, if it really is part of the network definition
				if (IsFeatureInNetworkClass(feature, edgeClassDef))
				{
					return true;
				}
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

		protected bool Equals(LinearNetworkDef other)
		{
			return Equals(_defaultJunctionClass, other._defaultJunctionClass)
			       && _defaultSubtype == other._defaultSubtype &&
			       NetworkClassDefinitions.SequenceEqual(other.NetworkClassDefinitions);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((LinearNetworkDef) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = _defaultJunctionClass != null
					               ? _defaultJunctionClass.GetHashCode()
					               : 0;
				hashCode = (hashCode * 397) ^ _defaultSubtype.GetHashCode();

				foreach (var classDef in NetworkClassDefinitions)
				{
					hashCode = (hashCode * 397) ^ classDef.GetHashCode();
				}

				return hashCode;
			}
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
