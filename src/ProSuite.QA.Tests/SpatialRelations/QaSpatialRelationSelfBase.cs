using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.SpatialRelations
{
	/// <summary>
	/// Check if there are any elements inside a group of layers that 
	/// have a certain relation with each other.
	/// </summary>
	public abstract class QaSpatialRelationSelfBase : QaSpatialRelationBase
	{
		#region Constructors

		protected QaSpatialRelationSelfBase([NotNull] IList<IReadOnlyFeatureClass> featureClasses,
		                                    esriSpatialRelEnum relation)
			: base(featureClasses, relation) { }

		protected QaSpatialRelationSelfBase([NotNull] IList<IReadOnlyFeatureClass> featureClasses,
		                                    [NotNull] string intersectionMatrix)
			: base(featureClasses, intersectionMatrix) { }

		protected QaSpatialRelationSelfBase([NotNull] IReadOnlyFeatureClass featureClass,
		                                    esriSpatialRelEnum relation)
			: this(new[] {featureClass}, relation) { }

		protected QaSpatialRelationSelfBase([NotNull] IReadOnlyFeatureClass featureClass,
		                                    [NotNull] string intersectionMatrix)
			: this(new[] {featureClass}, intersectionMatrix) { }

		#endregion

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			var feature = row as IReadOnlyFeature;
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
	}
}
