using System;
using System.Collections.Generic;
using System.Reflection;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.SpatialRelations
{
	public abstract class QaSpatialRelationBase : ContainerTest
	{
		private QueryFilterHelper[] _filterHelpers;
		private ISpatialFilter[] _spatialFilters;
		private ISpatialFilter[] _spatialFiltersIntersects;
		private readonly bool _disjointIsError;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Constructors

		protected QaSpatialRelationBase([NotNull] IList<IFeatureClass> featureClasses,
		                                esriSpatialRelEnum relation)
			: base(CastToTables((IEnumerable<IFeatureClass>) featureClasses))
		{
			TotalClassCount = featureClasses.Count;
			Relation = relation;

			UsesSymmetricRelation = IsKnownSymmetric(relation);
		}

		protected QaSpatialRelationBase([NotNull] IList<IFeatureClass> featureClasses,
		                                [NotNull] string intersectionMatrix)
			: this(featureClasses, esriSpatialRelEnum.esriSpatialRelRelation)
		{
			Assert.ArgumentNotNullOrEmpty(intersectionMatrix, nameof(intersectionMatrix));

			IntersectionMatrix = new IntersectionMatrix(intersectionMatrix);
			UsesSymmetricRelation = IntersectionMatrix.Symmetric;

			_disjointIsError = ! IntersectionMatrix.Intersects &&
			                   IntersectionMatrix.IntersectsExterior;
		}

		protected QaSpatialRelationBase([NotNull] IFeatureClass featureClass,
		                                esriSpatialRelEnum relation)
			: this(new[] {featureClass}, relation) { }

		protected QaSpatialRelationBase([NotNull] IFeatureClass featureClass,
		                                [NotNull] string intersectionMatrix)
			: this(new[] {featureClass}, intersectionMatrix) { }

		#endregion

		protected bool UsesSymmetricRelation { get; }

		protected int TotalClassCount { get; }

		[CanBeNull]
		protected IntersectionMatrix IntersectionMatrix { get; }

		protected esriSpatialRelEnum Relation { get; }

		[NotNull]
		protected QueryFilterHelper GetQueryFilterHelper(int tableIndex)
		{
			return FilterHelpers[tableIndex];
		}

		protected virtual bool RequiresInvertedRelation(int tableIndex)
		{
			return false;
		}

		protected abstract int FindErrors([NotNull] IRow row1, int tableIndex1,
		                                  [NotNull] IRow row2, int tableIndex2);

		protected int FindErrors([NotNull] IFeature feature,
		                         [NotNull] IGeometry searchGeometry,
		                         int tableIndex, int relatedTableIndex)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));
			Assert.ArgumentNotNull(searchGeometry, nameof(searchGeometry));

			ITable relatedTable = InvolvedTables[relatedTableIndex];

			// access through properties to allow lazy initialization:
			ISpatialFilter relatedFilter = SpatialFilters[relatedTableIndex];
			QueryFilterHelper relatedFilterHelper = FilterHelpers[relatedTableIndex];

			relatedFilter.Geometry = searchGeometry;

			var errorCount = 0;
			var anyFound = false;
			foreach (IRow relatedRow in Search(relatedTable,
			                                   relatedFilter,
			                                   relatedFilterHelper,
			                                   feature.Shape))
			{
				anyFound = true;

				errorCount += FindErrors(feature, tableIndex,
				                         relatedRow, relatedTableIndex);
			}

			if (! anyFound && _disjointIsError)
			{
				if (IsDisjoint(searchGeometry,
				               relatedTable, relatedTableIndex,
				               relatedFilterHelper,
				               feature.Shape))
				{
					errorCount += FindErrorsNoRelated(feature);
				}
			}

			return errorCount;
		}

		private static bool IsKnownSymmetric(esriSpatialRelEnum relation)
		{
			switch (relation)
			{
				case esriSpatialRelEnum.esriSpatialRelIntersects:
				case esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects:
				case esriSpatialRelEnum.esriSpatialRelTouches:
				case esriSpatialRelEnum.esriSpatialRelOverlaps: // TODO revise
				case esriSpatialRelEnum.esriSpatialRelCrosses:
					return true;

				case esriSpatialRelEnum.esriSpatialRelWithin:
				case esriSpatialRelEnum.esriSpatialRelContains:
				case esriSpatialRelEnum.esriSpatialRelRelation: // can be asymmetric
					return false;

				case esriSpatialRelEnum.esriSpatialRelIndexIntersects:
				case esriSpatialRelEnum.esriSpatialRelUndefined:
					throw new ArgumentOutOfRangeException(string.Format("Unsupported relation: {0}",
					                                                    relation));

				default:
					throw new ArgumentOutOfRangeException(string.Format("Unknown relation: {0}",
					                                                    relation));
			}
		}

		protected virtual int FindErrorsNoRelated(IRow row)
		{
			return NoError;
		}

		private bool IsDisjoint([NotNull] IGeometry shape,
		                        [NotNull] ITable relatedTable, int relatedTableIndex,
		                        [NotNull] QueryFilterHelper relatedFilterHelper,
		                        [NotNull] IGeometry cacheShape)
		{
			ISpatialFilter intersectsFilter = _spatialFiltersIntersects[relatedTableIndex];
			intersectsFilter.Geometry = shape;

			foreach (
				IRow row in
				Search(relatedTable, intersectsFilter, relatedFilterHelper, cacheShape))
			{
				_msg.VerboseDebug(() => $"not disjoint (row found: {GdbObjectUtils.ToString(row)})");

				return false;
			}

			return true;
		}

		private QueryFilterHelper[] FilterHelpers
		{
			get
			{
				if (_filterHelpers == null)
				{
					InitializeFilters();
				}

				return _filterHelpers;
			}
		}

		private ISpatialFilter[] SpatialFilters
		{
			get
			{
				if (_spatialFilters == null)
				{
					InitializeFilters();
				}

				return _spatialFilters;
			}
		}

		/// <summary>
		/// create a filter that gets the lines crossing the current row,
		/// with the same attribute constraints as the table
		/// </summary>
		private void InitializeFilters()
		{
			_spatialFilters = new ISpatialFilter[TotalClassCount];
			_filterHelpers = new QueryFilterHelper[TotalClassCount];
			_spatialFiltersIntersects = new ISpatialFilter[TotalClassCount];

			// there is one table and hence one filter (see constructor)
			// Create copy of this filter and use it for quering crossing lines
			IList<ISpatialFilter> spatialFilters;
			IList<QueryFilterHelper> filterHelpers;
			CopyFilters(out spatialFilters, out filterHelpers);

			for (var tableIndex = 0; tableIndex < TotalClassCount; tableIndex++)
			{
				_spatialFilters[tableIndex] = spatialFilters[tableIndex];
				_filterHelpers[tableIndex] = filterHelpers[tableIndex];

				_spatialFiltersIntersects[tableIndex] =
					CreateIntersectionFilter(_spatialFilters[tableIndex]);

				ConfigureSpatialFilter(_spatialFilters[tableIndex], tableIndex);
			}
		}

		private static ISpatialFilter CreateIntersectionFilter(
			[NotNull] ISpatialFilter defaultFilter)
		{
			var filter = (ISpatialFilter) ((IClone) defaultFilter).Clone();

			filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
			filter.SpatialRelDescription = null;

			return filter;
		}

		private void ConfigureSpatialFilter([NotNull] ISpatialFilter spatialFilter,
		                                    int tableIndex)
		{
			if (RequiresInvertedRelation(tableIndex))
			{
				switch (Relation)
				{
					case esriSpatialRelEnum.esriSpatialRelContains:
						spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelWithin;
						break;
					case esriSpatialRelEnum.esriSpatialRelWithin:
						spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;
						break;
					case esriSpatialRelEnum.esriSpatialRelRelation:
						string matrixString = Assert.NotNull(IntersectionMatrix).MatrixString;
						spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelRelation;
						spatialFilter.SpatialRelDescription = $"RELATE(G2, G1, '{matrixString}')";
						break;
					default:
						spatialFilter.SpatialRel = Relation;
						break;
				}
			}
			else
			{
				spatialFilter.SpatialRel = Relation;

				if (Relation == esriSpatialRelEnum.esriSpatialRelRelation)
				{
					string matrixString = Assert.NotNull(IntersectionMatrix).MatrixString;
					spatialFilter.SpatialRelDescription = $"RELATE (G1, G2, '{matrixString}')";
				}
			}
		}
	}
}
