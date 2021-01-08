using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.SpatialRelations
{
	/// <summary>
	/// Check if there are any elements inside a group of layers that 
	/// have a certain relation with each other.
	/// </summary>
	[CLSCompliant(false)]
	public abstract class QaSpatialRelationSelfBase : QaSpatialRelationBase
	{
		#region Constructors

		protected QaSpatialRelationSelfBase([NotNull] IList<IFeatureClass> featureClasses,
		                                    esriSpatialRelEnum relation)
			: base(featureClasses, relation) { }

		protected QaSpatialRelationSelfBase([NotNull] IList<IFeatureClass> featureClasses,
		                                    [NotNull] string intersectionMatrix)
			: base(featureClasses, intersectionMatrix) { }

		protected QaSpatialRelationSelfBase([NotNull] IFeatureClass featureClass,
		                                    esriSpatialRelEnum relation)
			: this(new[] {featureClass}, relation) { }

		protected QaSpatialRelationSelfBase([NotNull] IFeatureClass featureClass,
		                                    [NotNull] string intersectionMatrix)
			: this(new[] {featureClass}, intersectionMatrix) { }

		#endregion

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			var feature = row as IFeature;
			if (feature == null)
			{
				return NoError;
			}

			IGeometry searchGeometry = GetSearchGeometry(feature, tableIndex);

			if (searchGeometry == null || searchGeometry.IsEmpty)
			{
				return NoError;
			}

			var errorCount = 0;

			int startCompare = IgnoreUndirected && UsesSymmetricRelation
				                   ? tableIndex
				                   : 0;

			for (int searchTableIndex = startCompare;
			     searchTableIndex < TotalClassCount;
			     searchTableIndex++)
			{
				QueryFilterHelper filter = GetQueryFilterHelper(searchTableIndex);
				filter.MinimumOID = IgnoreUndirected && UsesSymmetricRelation &&
				                    tableIndex == searchTableIndex
					                    ? row.OID
					                    : -1;

				errorCount += FindErrors(feature, searchGeometry, tableIndex, searchTableIndex);
			}

			return errorCount;
		}

		[CanBeNull]
		protected virtual IGeometry GetSearchGeometry([NotNull] IFeature feature,
		                                              int tableIndex)
		{
			return feature.Shape;
		}
	}
}
