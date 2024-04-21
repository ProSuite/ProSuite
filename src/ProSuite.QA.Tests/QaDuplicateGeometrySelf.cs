using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.SpatialRelations;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaDuplicateGeometrySelf : QaSpatialRelationSelfBase
	{
		private readonly IReadOnlyFeatureClass _featureClass;
		private readonly bool _reportSingleErrorPerDuplicateSet;
		private readonly string _shapeFieldName;
		private readonly string _validRelationConstraintSql;

		private readonly Dictionary<long, HashSet<long>> _duplicatesPerOid =
			new Dictionary<long, HashSet<long>>();

		private readonly List<HashSet<long>> _duplicateSets = new List<HashSet<long>>();

		// TODO option to ignore Z and M (only compare xy of vertices)
		// TODO option to disregard point ordering (test for congruence only)

		private IValidRelationConstraint _validRelationConstraint;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string GeometriesEqualInXY = "GeometriesEqualInXY";

			public const string GeometriesEqualInXY_ConstraintNotFulfilled =
				"GeometriesEqualInXY.ConstraintNotFulfilled";

			public Code() : base("Duplicates") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_0))]
		public QaDuplicateGeometrySelf(
			[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass)
			: this(featureClass, string.Empty) { }

		[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_1))]
		public QaDuplicateGeometrySelf(
				[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_featureClass))] [NotNull]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_validDuplicateConstraint))]
				[CanBeNull]
				string
					validDuplicateConstraint)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, validDuplicateConstraint, false) { }

		[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_2))]
		public QaDuplicateGeometrySelf(
			[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_validDuplicateConstraint))] [CanBeNull]
			string
				validDuplicateConstraint,
			[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_reportSingleErrorPerDuplicateSet))]
			bool
				reportSingleErrorPerDuplicateSet)
			: base(featureClass, esriSpatialRelEnum.esriSpatialRelIntersects)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			_featureClass = featureClass;
			_reportSingleErrorPerDuplicateSet = reportSingleErrorPerDuplicateSet;
			_shapeFieldName = featureClass.ShapeFieldName;
			_validRelationConstraintSql = StringUtils.IsNotEmpty(validDuplicateConstraint)
				                              ? validDuplicateConstraint
				                              : null;
			AddCustomQueryFilterExpression(validDuplicateConstraint);
		}

		[InternallyUsedTest]
		public QaDuplicateGeometrySelf(
			[NotNull] QaDuplicateGeometrySelfDefinition definition)
			: this((IReadOnlyFeatureClass)definition.FeatureClass,
			       definition.ValidDuplicateConstraint, definition.ReportSingleErrorPerDuplicateSet)
		{ }

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			// if no duplicate is found in the first tile, then there is none
			return false;
		}

		protected override int FindErrors(IReadOnlyRow row1, int tableIndex1,
		                                  IReadOnlyRow row2, int tableIndex2)
		{
			if (_validRelationConstraint == null)
			{
				const bool constraintIsDirected = false;
				_validRelationConstraint = new ValidRelationConstraint(
					_validRelationConstraintSql, constraintIsDirected,
					GetSqlCaseSensitivity());
			}

			if (! _reportSingleErrorPerDuplicateSet)
			{
				return QaSpatialRelationUtils.ReportDuplicates(row1, tableIndex1,
				                                               row2, tableIndex2,
				                                               this, GetIssueCode(),
				                                               _validRelationConstraint);
			}

			long oid1 = row1.OID;
			long oid2 = row2.OID;

			if (! IsKnownDuplicate(oid1, oid2))
			{
				if (QaSpatialRelationUtils.AreDuplicates(
					    row1, tableIndex1,
					    row2, tableIndex2,
					    _validRelationConstraint,
					    out string _))
				{
					AddDuplicate(oid1, oid2);
				}
			}

			// duplicates will be reported at end
			return NoError;
		}

		private IssueCode GetIssueCode()
		{
			return _validRelationConstraintSql == null
				       ? Codes[Code.GeometriesEqualInXY]
				       : Codes[Code.GeometriesEqualInXY_ConstraintNotFulfilled];
		}

		private bool IsKnownDuplicate(long oid1, long oid2)
		{
			if (_validRelationConstraintSql != null)
			{
				// there is a constraint, must do pairwise evaluation
				return false;
			}

			HashSet<long> row1Duplicates;
			if (_duplicatesPerOid.TryGetValue(oid1, out row1Duplicates) &&
			    row1Duplicates.Contains(oid2))
			{
				return true;
			}

			HashSet<long> row2Duplicates;
			return _duplicatesPerOid.TryGetValue(oid2, out row2Duplicates) &&
			       row2Duplicates.Contains(oid1);
		}

		private void AddDuplicate(long oid1, long oid2)
		{
			HashSet<long> row1Duplicates;
			HashSet<long> row2Duplicates;

			if (! _duplicatesPerOid.TryGetValue(oid1, out row1Duplicates))
			{
				// no duplicate set yet assigned to row1

				if (_duplicatesPerOid.TryGetValue(oid2, out row2Duplicates))
				{
					// there is an existing set for row2
					// --> add oid1 to existing set, assign the set to row1
					row2Duplicates.Add(oid1);
					_duplicatesPerOid.Add(oid1, row2Duplicates);
				}
				else
				{
					// no duplicate set yet for neither row
					// -> create new set, assign the set to row1 and row2
					var duplicates = new HashSet<long> { oid1, oid2 };
					_duplicateSets.Add(duplicates);

					_duplicatesPerOid.Add(oid1, duplicates);
					_duplicatesPerOid.Add(oid2, duplicates);
				}
			}
			else
			{
				// duplicate set assigned to row1

				if (_duplicatesPerOid.TryGetValue(oid2, out row2Duplicates))
				{
					// duplicate sets are assigned to both row1 and row2
					if (row1Duplicates != row2Duplicates)
					{
						// different set instances, merge them and remove set for row2
						MergeSets(row1Duplicates, row2Duplicates);
					}

					// else: same set. Both oids must already be in the set
				}
				else
				{
					// existing duplicate set for row1, but no duplicate set yet for row2
					// -> add to existing set, assign the set to row2
					row1Duplicates.Add(oid2);
					_duplicatesPerOid.Add(oid2, row1Duplicates);
				}
			}
		}

		private void MergeSets([NotNull] HashSet<long> targetSet,
		                       [NotNull] HashSet<long> setToMerge)
		{
			foreach (long oid in setToMerge)
			{
				_duplicatesPerOid[oid] = targetSet;
				targetSet.Add(oid);
			}

			_duplicateSets.Remove(setToMerge);
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			if (! _reportSingleErrorPerDuplicateSet)
			{
				return NoError;
			}

			if (args.State != TileState.Final)
			{
				return NoError;
			}

			int errorCount = 0;

			Dictionary<long, HashSet<long>> duplicatesByFirstOid =
				GetDuplicatesByFirstOid(_duplicateSets);

			string tableName = _featureClass.Name;
			const bool recycle = true;
			foreach (IReadOnlyRow r in TableFilterUtils.GetRows(
				         _featureClass, duplicatesByFirstOid.Keys, recycle))
			{
				IReadOnlyFeature feature = (IReadOnlyFeature) r;
				HashSet<long> duplicates = duplicatesByFirstOid[feature.OID];

				string errorDescription = _validRelationConstraintSql == null
					                          ? "Geometries are equal"
					                          : "Geometries are equal and constraint is not fulfilled";

				IGeometry errorGeometry = feature.ShapeCopy;
				errorCount += ReportError(errorDescription, GetInvolvedRows(tableName, duplicates),
				                          errorGeometry, GetIssueCode(), _shapeFieldName);
			}

			return errorCount;
		}

		[NotNull]
		private static Dictionary<long, HashSet<long>> GetDuplicatesByFirstOid(
			[NotNull] IEnumerable<HashSet<long>> duplicateSets)
		{
			var result = new Dictionary<long, HashSet<long>>();

			foreach (HashSet<long> duplicateSet in duplicateSets)
			{
				Assert.True(duplicateSet.Count >= 2, "Invalid set size: {0}", duplicateSet.Count);

				var list = new List<long>(duplicateSet);
				long firstOid = list[0];

				result.Add(firstOid, duplicateSet);
			}

			return result;
		}

		[NotNull]
		private static InvolvedRows GetInvolvedRows(
			[NotNull] string tableName,
			[NotNull] IEnumerable<long> oids)
		{
			InvolvedRows result = new InvolvedRows();
			result.AddRange(oids.Select(oid => new InvolvedRow(tableName, oid)));

			return result;
		}
	}
}
