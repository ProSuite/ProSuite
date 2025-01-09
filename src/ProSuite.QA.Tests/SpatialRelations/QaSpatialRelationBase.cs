using System;
using System.Collections.Generic;
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
		private IFeatureClassFilter[] _spatialFilters;
		private IFeatureClassFilter[] _spatialFiltersIntersects;
		private readonly bool _disjointIsError;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		protected QaSpatialRelationBase([NotNull] IList<IReadOnlyFeatureClass> featureClasses,
		                                esriSpatialRelEnum relation)
			: base(CastToTables((IEnumerable<IReadOnlyFeatureClass>) featureClasses))
		{
			TotalClassCount = featureClasses.Count;
			Relation = relation;

			UsesSymmetricRelation = IsKnownSymmetric(relation);
		}

		protected QaSpatialRelationBase([NotNull] IList<IReadOnlyFeatureClass> featureClasses,
		                                [NotNull] string intersectionMatrix)
			: this(featureClasses, esriSpatialRelEnum.esriSpatialRelRelation)
		{
			Assert.ArgumentNotNullOrEmpty(intersectionMatrix, nameof(intersectionMatrix));

			IntersectionMatrix = new IntersectionMatrix(intersectionMatrix);
			UsesSymmetricRelation = IntersectionMatrix.Symmetric;

			_disjointIsError = ! IntersectionMatrix.Intersects &&
			                   IntersectionMatrix.IntersectsExterior;
		}

		protected QaSpatialRelationBase([NotNull] IReadOnlyFeatureClass featureClass,
		                                esriSpatialRelEnum relation)
			: this(new[] { featureClass }, relation) { }

		protected QaSpatialRelationBase([NotNull] IReadOnlyFeatureClass featureClass,
		                                [NotNull] string intersectionMatrix)
			: this(new[] { featureClass }, intersectionMatrix) { }

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

		protected abstract int FindErrors([NotNull] IReadOnlyRow row1, int tableIndex1,
		                                  [NotNull] IReadOnlyRow row2, int tableIndex2);

		protected int FindErrors([NotNull] IReadOnlyFeature feature,
		                         [NotNull] IGeometry searchGeometry,
		                         int tableIndex, int relatedTableIndex)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));
			Assert.ArgumentNotNull(searchGeometry, nameof(searchGeometry));

			IReadOnlyTable relatedTable = InvolvedTables[relatedTableIndex];

			// access through properties to allow lazy initialization:
			IFeatureClassFilter relatedFilter = SpatialFilters[relatedTableIndex];
			QueryFilterHelper relatedFilterHelper = FilterHelpers[relatedTableIndex];

			relatedFilter.FilterGeometry = searchGeometry;

			var errorCount = 0;
			IReadOnlyRow otherRow = null;
			try
			{
				var anyFound = false;
				foreach (IReadOnlyRow relatedRow in Search(relatedTable,
				                                           relatedFilter,
				                                           relatedFilterHelper))
				{
					anyFound = true;
					otherRow = relatedRow;
					// TODO: try catch and throw TestRowException: "Error testing feature {feature} against feature {relatedRow}: {e.Message}
					errorCount += FindErrors(feature, tableIndex,
					                         relatedRow, relatedTableIndex);
				}

				if (! anyFound && _disjointIsError)
				{
					if (IsDisjoint(searchGeometry,
					               relatedTable, relatedTableIndex,
					               relatedFilterHelper))
					{
						errorCount += FindErrorsNoRelated(feature);
					}
				}
			}
			catch (Exception e)
			{
				string otherRowMsg =
					otherRow == null ? "<null>" : GdbObjectUtils.ToString(otherRow);
				string msg =
					$"Error testing row {GdbObjectUtils.ToString(feature)} against {otherRowMsg}: {e.Message}";

				_msg.Debug(msg, e);
				throw new TestDataException(msg, feature);
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

		protected virtual int FindErrorsNoRelated(IReadOnlyRow row)
		{
			return NoError;
		}

		private bool IsDisjoint([NotNull] IGeometry shape,
		                        [NotNull] IReadOnlyTable relatedTable, int relatedTableIndex,
		                        [NotNull] QueryFilterHelper relatedFilterHelper)
		{
			IFeatureClassFilter intersectsFilter = _spatialFiltersIntersects[relatedTableIndex];
			intersectsFilter.FilterGeometry = shape;

			foreach (
				IReadOnlyRow row in
				Search(relatedTable, intersectsFilter, relatedFilterHelper))
			{
				_msg.VerboseDebug(
					() => $"not disjoint (row found: {GdbObjectUtils.ToString(row)})");

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

		private IFeatureClassFilter[] SpatialFilters
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
			_spatialFilters = new IFeatureClassFilter[TotalClassCount];
			_filterHelpers = new QueryFilterHelper[TotalClassCount];
			_spatialFiltersIntersects = new IFeatureClassFilter[TotalClassCount];

			// there is one table and hence one filter (see constructor)
			// Create copy of this filter and use it for quering crossing lines
			IList<IFeatureClassFilter> spatialFilters;
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

		private static IFeatureClassFilter CreateIntersectionFilter(
			[NotNull] IFeatureClassFilter defaultFilter)
		{
			var filter = (IFeatureClassFilter) defaultFilter.Clone();

			filter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelIntersects;
			filter.SpatialRelDescription = null;

			return filter;
		}

		private void ConfigureSpatialFilter([NotNull] IFeatureClassFilter spatialFilter,
		                                    int tableIndex)
		{
			if (RequiresInvertedRelation(tableIndex))
			{
				switch (Relation)
				{
					case esriSpatialRelEnum.esriSpatialRelContains:
						spatialFilter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelWithin;
						break;
					case esriSpatialRelEnum.esriSpatialRelWithin:
						spatialFilter.SpatialRelationship =
							esriSpatialRelEnum.esriSpatialRelContains;
						break;
					case esriSpatialRelEnum.esriSpatialRelRelation:
						string matrixString = Assert.NotNull(IntersectionMatrix).MatrixString;
						spatialFilter.SpatialRelationship =
							esriSpatialRelEnum.esriSpatialRelRelation;
						spatialFilter.SpatialRelDescription = $"RELATE(G2, G1, '{matrixString}')";
						break;
					default:
						spatialFilter.SpatialRelationship = Relation;
						break;
				}
			}
			else
			{
				spatialFilter.SpatialRelationship = Relation;

				if (Relation == esriSpatialRelEnum.esriSpatialRelRelation)
				{
					string matrixString = Assert.NotNull(IntersectionMatrix).MatrixString;
					spatialFilter.SpatialRelDescription = $"RELATE (G1, G2, '{matrixString}')";
				}
			}
		}
	}
}
