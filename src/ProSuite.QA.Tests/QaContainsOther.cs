using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaContainsOther : ContainerTest
	{
		private readonly bool _reportIndividualParts;
		private readonly int _containsClassesCount; // # of contains layer
		private readonly int _totalClassesCount; // total # of layers (contains + isWithin)
		private ISpatialFilter[] _queryFilter;
		private ISpatialFilter[] _intersectsFilter;
		private QueryFilterHelper[] _helper;
		private readonly List<esriGeometryType> _shapeTypes;

		private readonly string _isContainingConditionSql;
		private IsContainingCondition _isContainingCondition;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NoContainingFeature = "NoContainingFeature";

			public const string NoContainingFeature_WithFulfilledConstraint =
				"NoContainingFeature.WithFulfilledConstraint";

			public Code() : base("ContainsOther") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaContainsOther_0))]
		public QaContainsOther(
				[Doc(nameof(DocStrings.QaContainsOther_contains_0))] [NotNull]
				IList<IFeatureClass> contains,
				[Doc(nameof(DocStrings.QaContainsOther_isWithin_0))] [NotNull]
				IList<IFeatureClass> isWithin)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(contains, isWithin, null, false) { }

		[Doc(nameof(DocStrings.QaContainsOther_1))]
		public QaContainsOther(
			[Doc(nameof(DocStrings.QaContainsOther_contains_1))] [NotNull]
			IFeatureClass contains,
			[Doc(nameof(DocStrings.QaContainsOther_isWithin_1))] [NotNull]
			IFeatureClass isWithin)
			: this(new[] {contains}, new[] {isWithin}, null, false) { }

		[Doc(nameof(DocStrings.QaContainsOther_2))]
		public QaContainsOther(
			[Doc(nameof(DocStrings.QaContainsOther_contains_0))] [NotNull]
			IList<IFeatureClass> contains,
			[Doc(nameof(DocStrings.QaContainsOther_isWithin_0))] [NotNull]
			IList<IFeatureClass> isWithin,
			[Doc(nameof(DocStrings.QaContainsOther_isContainingCondition))] [CanBeNull]
			string
				isContainingCondition,
			[Doc(nameof(DocStrings.QaContainsOther_reportIndividualParts))]
			bool reportIndividualParts)
			: base(CastToTables(contains, isWithin))
		{
			Assert.ArgumentNotNull(contains, nameof(contains));
			Assert.ArgumentNotNull(isWithin, nameof(isWithin));

			_isContainingConditionSql = isContainingCondition;
			_reportIndividualParts = reportIndividualParts;

			_containsClassesCount = contains.Count;
			_totalClassesCount = _containsClassesCount + isWithin.Count;

			_shapeTypes = new List<esriGeometryType>(
				GetShapeTypesByTableIndex(contains, isWithin));
		}

		[Doc(nameof(DocStrings.QaContainsOther_3))]
		public QaContainsOther(
			[Doc(nameof(DocStrings.QaContainsOther_contains_1))] [NotNull]
			IFeatureClass contains,
			[Doc(nameof(DocStrings.QaContainsOther_isWithin_1))] [NotNull]
			IFeatureClass isWithin,
			[Doc(nameof(DocStrings.QaContainsOther_isContainingCondition))] [CanBeNull]
			string
				isContainingCondition,
			[Doc(nameof(DocStrings.QaContainsOther_reportIndividualParts))]
			bool reportIndividualParts)
			: this(new[] {contains},
			       new[] {isWithin},
			       isContainingCondition,
			       reportIndividualParts) { }

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			// Currently, rows that are on equal layers are tested twice in the same direction,
			// as Test.CheckConstraint just looks for first constraint in layer
			if (tableIndex < _containsClassesCount)
			{
				return NoError;
			}

			ISpatialFilter[] filters = GetQueryFilters();

			IGeometry shape = ((IFeature) row).Shape;

			if (_isContainingCondition == null)
			{
				_isContainingCondition = new IsContainingCondition(
					_isContainingConditionSql,
					GetSqlCaseSensitivity());
			}

			bool anyContainingFound = false;
			for (int containsClassIndex = 0;
			     containsClassIndex < _containsClassesCount;
			     containsClassIndex++)
			{
				filters[containsClassIndex].Geometry = shape;

				foreach (IRow containingRow in GetContainingRows(containsClassIndex))
				{
					anyContainingFound = true;

					if (! _isContainingCondition.IsFulfilled(
						    containingRow, containsClassIndex, row, tableIndex))
					{
						// condition is not fulfilled, ignore containing row
						continue;
					}

					// a feature is found that contains the shape --> no error
					return NoError;
				}
			}

			IGeometry errorGeometry = GetErrorGeometry(shape, row, tableIndex);

			if (errorGeometry == null || errorGeometry.IsEmpty)
			{
				return NoError;
			}

			string description = _reportIndividualParts
				                     ? "Feature part is not completely within any containing feature"
				                     : "Feature is not completely within any containing feature";

			IssueCode issueCode = anyContainingFound
				                      ? Codes[Code.NoContainingFeature_WithFulfilledConstraint]
				                      : Codes[Code.NoContainingFeature];

			return ReportError(description, errorGeometry,
			                   issueCode, null,
			                   _reportIndividualParts, row);
		}

		[NotNull]
		private ISpatialFilter[] GetQueryFilters()
		{
			if (_queryFilter != null)
			{
				return _queryFilter;
			}

			InitFilter();
			return Assert.NotNull(_queryFilter, "_queryFilter");
		}

		[NotNull]
		private static IEnumerable<esriGeometryType> GetShapeTypesByTableIndex(
			[NotNull] IEnumerable<IFeatureClass> covering,
			[NotNull] IEnumerable<IFeatureClass> covered)
		{
			foreach (IFeatureClass featureClass in covering)
			{
				yield return featureClass.ShapeType;
			}

			foreach (IFeatureClass featureClass in covered)
			{
				yield return featureClass.ShapeType;
			}
		}

		[NotNull]
		private IEnumerable<IRow> GetContainingRows(int containsClassIndex)
		{
			return Search(InvolvedTables[containsClassIndex],
			              _queryFilter[containsClassIndex],
			              _helper[containsClassIndex]);
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		[CanBeNull]
		private IGeometry GetErrorGeometry([NotNull] IGeometry geometry,
		                                   [NotNull] IRow row,
		                                   int tableIndex)
		{
			var differences = new List<IGeometry>();

			for (int containsClassIndex = 0;
			     containsClassIndex < _containsClassesCount;
			     containsClassIndex++)
			{
				foreach (IRow intersectingRow in
					SearchIntersectingFeatures(containsClassIndex, geometry))
				{
					var intersectingFeature = (IFeature) intersectingRow;

					if (! _isContainingCondition.IsFulfilled(
						    intersectingFeature, containsClassIndex,
						    row, tableIndex))
					{
						// condition is not fulfilled, ignore containing row
						continue;
					}

					esriGeometryType containingShapeType = _shapeTypes[containsClassIndex];
					IGeometry geometryToSubtract =
						containingShapeType == esriGeometryType.esriGeometryMultiPatch
							? GeometryFactory.CreatePolygon((IMultiPatch) intersectingFeature.Shape)
							: intersectingFeature.Shape;

					var topoOp = (ITopologicalOperator) geometry;
					IGeometry difference = topoOp.Difference(geometryToSubtract);

					const bool allowReorder = true;
					GeometryUtils.Simplify(difference, allowReorder);

					if (difference.IsEmpty)
					{
						// there is no difference --> geometry is contained!
						return null;
					}

					differences.Add(difference);
				}
			}

			if (differences.Count == 0)
			{
				// no intersecting geometries --> report entire shape
				return geometry;
			}

			IGeometry unionedDifferences = GeometryUtils.UnionGeometries(differences);

			GeometryUtils.Simplify(unionedDifferences, true);

			return unionedDifferences;
		}

		[NotNull]
		private IEnumerable<IRow> SearchIntersectingFeatures(int classIndex,
		                                                     [NotNull] IGeometry geometry)
		{
			_intersectsFilter[classIndex].Geometry = geometry;

			return Search(InvolvedTables[classIndex],
			              _intersectsFilter[classIndex],
			              _helper[classIndex]);
		}

		/// <summary>
		/// create a filter that gets the lines crossing the current row,
		/// with the same attribute constraints as the table
		/// </summary>
		private void InitFilter()
		{
			IList<ISpatialFilter> pFilters;
			IList<QueryFilterHelper> pHelpers;
			_queryFilter = new ISpatialFilter[_totalClassesCount];
			_intersectsFilter = new ISpatialFilter[_totalClassesCount];
			_helper = new QueryFilterHelper[_totalClassesCount];

			// Create copy of this filter and use it for quering features
			CopyFilters(out pFilters, out pHelpers);
			for (int i = 0; i < _totalClassesCount; i++)
			{
				_queryFilter[i] = pFilters[i];
				_helper[i] = pHelpers[i];

				_queryFilter[i].SpatialRel = i < _containsClassesCount
					                             ? esriSpatialRelEnum.esriSpatialRelWithin
					                             : esriSpatialRelEnum.esriSpatialRelContains;

				_intersectsFilter[i] = (ISpatialFilter) ((IClone) pFilters[i]).Clone();
				_intersectsFilter[i].SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
			}
		}

		private class IsContainingCondition : RowPairCondition
		{
			private const bool _isDirected = true;
			private const bool _undefinedConstraintIsFulfilled = true;

			public IsContainingCondition([CanBeNull] string constraint, bool caseSensitive)
				: base(constraint, _isDirected, _undefinedConstraintIsFulfilled, caseSensitive) { }
		}
	}
}