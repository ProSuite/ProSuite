using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.LinearNetwork
{
	public class LinearNetworkClassDef
	{
		private FilterHelper _filterHelper;

		[NotNull]
		public IFeatureClass FeatureClass { get; }

		[CanBeNull]
		public string WhereClause { get; }

		public esriGeometryType GeometryType { get; }

		public LinearNetworkClassDef([NotNull] IFeatureClass featureClass,
		                             [CanBeNull] string whereClause = null)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			esriGeometryType shapeType = featureClass.ShapeType;
			Assert.True(
				shapeType == esriGeometryType.esriGeometryPolyline ||
				shapeType == esriGeometryType.esriGeometryPoint,
				"featureClass.ShapeType must either be esriGeometryPolyline or esriGeometryPoint");

			FeatureClass = featureClass;
			WhereClause = whereClause;
			GeometryType = shapeType;
		}

		/// <summary>
		/// Determines whether the specified feature, which is assumed to be from the correct
		/// feature class, is part of the network w.r.t. the where clause.
		/// <remarks>Internally uses a <see cref="FilterHelper"/> instantiated upon first use.</remarks> 
		/// </summary>
		public bool IsInLinearNetworkClass([NotNull] IFeature feature)
		{
			if (string.IsNullOrEmpty(WhereClause))
			{
				return true;
			}

			if (_filterHelper == null)
			{
				_filterHelper =
					FilterHelper.Create(ReadOnlyTableFactory.Create(FeatureClass), WhereClause);
			}

			// TOP-5699: Do not use the FeatureClass as owner because it could be from the default version
			// and there is a reference-equals assertion between the owner's base table and the feature's table.
			IReadOnlyRow readOnlyFeature = ReadOnlyFeature.Create(feature);

			return _filterHelper.Check(readOnlyFeature);
		}

		protected bool Equals(LinearNetworkClassDef other)
		{
			return FeatureClass.Equals(other.FeatureClass) && WhereClause == other.WhereClause &&
			       GeometryType == other.GeometryType;
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

			return Equals((LinearNetworkClassDef) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = FeatureClass.GetHashCode();
				hashCode = (hashCode * 397) ^ (WhereClause != null ? WhereClause.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (int) GeometryType;
				return hashCode;
			}
		}

		public override string ToString()
		{
			return WhereClause != null
				       ? $"Class: {DatasetUtils.GetName(FeatureClass)}, Where-Clause: {WhereClause}"
				       : $"Class: {DatasetUtils.GetName(FeatureClass)}";
		}
	}
}
