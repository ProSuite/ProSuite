using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[TableTransformer]
	public class TrSpatialJoin : TableTransformer<TransformedFeatureClass>
	{
		public enum SearchOption
		{
			Tile,
			All
		}

		private const SearchOption _defaultSearchOption = SearchOption.Tile;

		[DocTr(nameof(DocTrStrings.TrSpatialJoin_0))]
		public TrSpatialJoin(
			[NotNull] [DocTr(nameof(DocTrStrings.TrSpatialJoin_t0))]
			IReadOnlyFeatureClass t0,
			[NotNull] [DocTr(nameof(DocTrStrings.TrSpatialJoin_t1))]
			IReadOnlyFeatureClass t1)
			: base(CastToTables(t0, t1)) { }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrSpatialJoin_Constraint))]
		public string Constraint { get; set; }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrSpatialJoin_OuterJoin))]
		public bool OuterJoin { get; set; }

		[TestParameter(_defaultSearchOption)]
		[DocTr(nameof(DocTrStrings.TrSpatialJoin_NeighborSearchOption))]
		public SearchOption NeighborSearchOption { get; set; }

		// Remark: Grouped must come in Code before T1Attributes !
		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrSpatialJoin_Grouped))]
		public bool Grouped { get; set; }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrSpatialJoin_T0Attributes))]
		public IList<string> T0Attributes { get; set; }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrSpatialJoin_T1Attributes))]
		public IList<string> T1Attributes { get; set; }

		[TestParameter]
		[DocTr(nameof(DocTrStrings.TrSpatialJoin_T1CalcAttributes))]
		public IList<string> T1CalcAttributes { get; set; }

		protected override TransformedFeatureClass GetTransformedCore(string name)
		{
			TransformedFc transformedFc = new TransformedFc(
				(IReadOnlyFeatureClass) InvolvedTables[0],
				(IReadOnlyFeatureClass) InvolvedTables[1], this,
				name);

			var t0 = (IReadOnlyFeatureClass) InvolvedTables[0];
			var t1 = (IReadOnlyFeatureClass) InvolvedTables[1];

			TrSpatialJoinDataset backingDataset =
				(TrSpatialJoinDataset) transformedFc.BackingDataset;

			Assert.NotNull(backingDataset).SetJoinConstraint(Constraint, GetSqlCaseSensitivity());

			backingDataset.OuterJoin = OuterJoin;
			backingDataset.Grouped = Grouped;
			backingDataset.NeighborSearchOption = NeighborSearchOption;

			TransformedTableFields t0Fields = new TransformedTableFields(t0);
			TransformedTableFields t1Fields = new TransformedTableFields(t1)
			                                  {
				                                  AreResultRowsGrouped = Grouped
			                                  };

			backingDataset.ToJoinTableFields = t0Fields;
			backingDataset.JoinedTableFields = t1Fields;

			// Add the minimal fields that must always be present:
			t0Fields.AddOIDField(transformedFc, "OBJECTID");
			t0Fields.AddShapeField(transformedFc);

			// Add fields defined by Attribute parameter:
			if (T0Attributes != null)
			{
				t0Fields.AddUserDefinedFields(T0Attributes, transformedFc);
			}
			else
			{
				t0Fields.AddAllFields(transformedFc);
			}

			if (T1Attributes != null)
			{
				t1Fields.AddUserDefinedFields(T1Attributes, transformedFc, T1CalcAttributes);
			}
			else if (! Grouped)
			{
				t1Fields.PreviouslyAddedFields.Add(t0Fields);
				t1Fields.AddAllFields(transformedFc);
			}

			return transformedFc;
		}

		private class JoinConstraint : RowPairCondition
		{
			public JoinConstraint([NotNull] string constraint, bool caseSensitive)
				: base(constraint, isDirected: true, undefinedConditionIsFulfilled: true,
				       row1Alias: "T0", row2Alias: "T1", caseSensitive: caseSensitive,
				       conciseMessage: true) { }
		}

		private class TransformedFc : TransformedFeatureClass, IDataContainerAware
		{
			public TransformedFc(IReadOnlyFeatureClass t0, IReadOnlyFeatureClass t1,
			                     TrSpatialJoin parent,
			                     string name = null)
				: base(null, ! string.IsNullOrWhiteSpace(name) ? name : "intersectResult",
				       t0.ShapeType,
				       createBackingDataset: (t) =>
					       new TrSpatialJoinDataset((TransformedFc) t, t0, t1),
				       workspace: new GdbWorkspace(new TransformerWorkspace()))
			{
				InvolvedTables = new List<IReadOnlyTable> {t0, t1};
			}

			public IList<IReadOnlyTable> InvolvedTables { get; }

			public IDataContainer DataContainer
			{
				get => BackingDs.DataContainer;
				set
				{
					// TODO: Decide on one (Note that DataContainer conflicts with property on InvolvedTablesBase)
					BackingDs.DataContainer = value;
					BackingDs.DataSearchContainer = value;
				}
			}

			private TrSpatialJoinDataset BackingDs => (TrSpatialJoinDataset) BackingDataset;
		}

		private class TrSpatialJoinDataset : TransformedBackingDataset
		{
			private readonly IReadOnlyFeatureClass _t0;
			private readonly IReadOnlyFeatureClass _t1;

			private JoinConstraint _constraint;

			public TrSpatialJoinDataset(
				[NotNull] TransformedFc resultFeatureClass,
				[NotNull] IReadOnlyFeatureClass t0,
				[NotNull] IReadOnlyFeatureClass t1)
				: base(resultFeatureClass, CastToTables(t0, t1))
			{
				_t0 = t0;
				_t1 = t1;

				ResultFeatureClass = resultFeatureClass;
				ResultFeatureClass.SpatialReference = t0.SpatialReference;
			}

			[NotNull]
			private GdbFeatureClass ResultFeatureClass { get; }

			public TransformedTableFields ToJoinTableFields { get; set; }
			public TransformedTableFields JoinedTableFields { get; set; }

			public bool Grouped { get; set; }
			public bool OuterJoin { get; set; }
			public SearchOption NeighborSearchOption { get; set; }

			public void SetJoinConstraint(string constraintClause,
			                              bool isCaseSensitive)
			{
				_constraint = new JoinConstraint(constraintClause, isCaseSensitive);
			}

			public override IEnvelope Extent => _t0.Extent;

			public override VirtualRow GetUncachedRow(long id)
			{
				throw new NotImplementedException();
			}

			public override long GetRowCount(IQueryFilter queryFilter)
			{
				// TODO
				return _t0.RowCount(queryFilter);
			}

			public override IEnumerable<VirtualRow> Search(IQueryFilter filter, bool recycling)
			{
				IRelationalOperator t1LoadedExtent = null;

				if (NeighborSearchOption == SearchOption.All)
				{
					t1LoadedExtent = (IRelationalOperator) DataSearchContainer.GetLoadedExtent(_t1);
				}

				ISpatialFilter joinFilter = new SpatialFilterClass();
				joinFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;

				foreach (var toJoin in DataSearchContainer.Search(
					         _t0, filter ?? new QueryFilterClass(), QueryHelpers[0]))
				{
					joinFilter.Geometry = ((IReadOnlyFeature) toJoin).Extent;
					var op = (IRelationalOperator) ((IReadOnlyFeature) toJoin).Shape;

					List<IReadOnlyRow> joineds = new List<IReadOnlyRow>();
					bool outerJoin = OuterJoin;
					foreach (var joined in EnumNeighbors(joinFilter, t1LoadedExtent))
					{
						if (! HasFulfilledConstraint(toJoin, joined))
						{
							continue;
						}

						outerJoin = false;

						IGeometry joinedGeom = ((IReadOnlyFeature) joined).Shape;
						// TODO implement different relations
						if (op.Disjoint(joinedGeom))
						{
							continue;
						}

						if (! Grouped)
						{
							var f = CreateFeature(toJoin, new[] {joined});
							//res.CreateFeature();
							yield return f;
						}
						else
						{
							joineds.Add(joined);
						}
					}

					if (joineds.Count > 0 || outerJoin)
					{
						GdbFeature f = CreateFeature(toJoin, joineds);
						yield return f;
					}
				}
			}

			private bool HasFulfilledConstraint(IReadOnlyRow t0, IReadOnlyRow t1)
			{
				return _constraint == null ||
				       _constraint.IsFulfilled(t0, 0, t1, 1, out string _);
			}

			private IEnumerable<IReadOnlyRow> EnumNeighbors(
				[NotNull] ISpatialFilter joinFilter,
				[CanBeNull] IRelationalOperator loaded)
			{
				foreach (var joined in DataSearchContainer.Search(
					         _t1, joinFilter, QueryHelpers[1]))
				{
					yield return joined;
				}

				if (loaded == null)
				{
					yield break;
				}

				if (loaded.Contains(joinFilter.Geometry))
				{
					yield break;
				}

				IEnvelope queryGeom = joinFilter.Geometry.Envelope;
				double tolerance = GeometryUtils.GetXyTolerance(queryGeom);
				queryGeom.Expand(tolerance, tolerance, false);

				foreach (var row in _t1.EnumRows(joinFilter, recycle: false))
				{
					var neighbor = (IReadOnlyFeature) row;
					if (loaded.Disjoint(neighbor.Extent))
					{
						yield return neighbor;
					}
				}
			}

			private GdbFeature CreateFeature([NotNull] IReadOnlyRow toJoin,
			                                 [NotNull] IList<IReadOnlyRow> joineds)
			{
				// Build an aggregate value list consisting of the toJoin row, the baseRows and
				// the extra calculated values;
				var rowValues = new MultiListValues(joineds.Count + 2)
				                {
					                AllowMissingFieldMapping = true
				                };

				List<IReadOnlyRow> baseRows = new List<IReadOnlyRow>(joineds.Count + 1);
				baseRows.Add(toJoin);
				baseRows.AddRange(joineds);

				List<CalculatedValue> extraValues = new List<CalculatedValue>();

				// 1. The toJoin row, wrapped in a value list:
				var toJoinValues = new ReadOnlyRowBasedValues(toJoin);
				rowValues.AddList(toJoinValues, ToJoinTableFields.FieldIndexMapping);

				extraValues.AddRange(GetCalculatedValues(toJoin, ToJoinTableFields, baseRows));

				// 2. The joined row(s):
				if (joineds.Count == 1)
				{
					var joinedValues = new ReadOnlyRowBasedValues(joineds[0]);
					rowValues.AddList(joinedValues, JoinedTableFields.FieldIndexMapping);

					extraValues.AddRange(GetCalculatedValues(joineds[0], JoinedTableFields));
				}
				else
				{
					extraValues.AddRange(JoinedTableFields.GetCalculatedValues(joineds));
				}

				// Add all the collected extra values with their own copy-matrix:
				IValueList simpleList =
					TransformedAttributeUtils.ToSimpleValueList(
						extraValues, out IDictionary<int, int> extraCopyMatrix);

				rowValues.AddList(simpleList, extraCopyMatrix);

				return GdbFeature.Create(toJoin.OID, ResultFeatureClass, rowValues);
			}

			private IEnumerable<CalculatedValue> GetCalculatedValues(
				[NotNull] IReadOnlyRow sourceRow,
				[NotNull] TransformedTableFields sourceTableFields,
				[CanBeNull] IList<IReadOnlyRow> involvedBaseRowsToAdd = null)
			{
				// Base rows, if requested:
				if (involvedBaseRowsToAdd != null)
				{
					int baseRowsIdx = ResultFeatureClass.FindField(InvolvedRowUtils.BaseRowField);
					yield return new CalculatedValue(baseRowsIdx, involvedBaseRowsToAdd);
				}

				if (sourceTableFields.CalculatedFields != null)
				{
					var sources = new List<IReadOnlyRow> {sourceRow};

					foreach (CalculatedValue calculatedValue in sourceTableFields
						         .GetCalculatedValues(sources))
					{
						yield return calculatedValue;
					}
				}
			}
		}
	}
}
