using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
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
			transformedFc.Constraint = Constraint;
			transformedFc.OuterJoin = OuterJoin;
			transformedFc.Grouped = Grouped;
			transformedFc.NeighborSearchOption = NeighborSearchOption;

			// Suggestion for multi-table transformers: fields are only qualified to avoid duplicates
			// using <input table name>_ 
			TransformedTableFields t0Fields = new TransformedTableFields(InvolvedTables[0])
			                                  {NonUserDefinedFieldPrefix = "t0."};
			TransformedTableFields t1Fields = new TransformedTableFields(InvolvedTables[1])
			                                  {
				                                  NonUserDefinedFieldPrefix = "t1.",
				                                  AreResultRowsGrouped = Grouped
			                                  };

			transformedFc.TableFieldsBySource.Add(InvolvedTables[0], t0Fields);
			transformedFc.TableFieldsBySource.Add(InvolvedTables[1], t1Fields);

			// Add the minimal fields that must always be present:
			t0Fields.AddOIDField(transformedFc, "OBJECTID");
			t0Fields.AddShapeField(transformedFc);

			// Add fields defined by Attribute parameter:
			if (T0Attributes != null)
				t0Fields.AddUserDefinedFields(T0Attributes, transformedFc);

			if (T1Attributes != null)
				t1Fields.AddUserDefinedFields(T1Attributes, transformedFc, T1CalcAttributes);

			//AddFields(transformedFc, T0Attributes, InvolvedTables[0], isGrouped: false);
			//AddFields(transformedFc, T1Attributes, InvolvedTables[1], isGrouped: Grouped, T1CalcAttributes);

			// Add all fields (not already defined by the user?)
			t0Fields.AddAllFields(transformedFc, false);
			if (! Grouped)
			{
				t1Fields.AddAllFields(transformedFc, false);
			}

			//AddFields(transformedFc, InvolvedTables[0], "t0");
			//if (!Grouped)
			//{
			//	AddFields(transformedFc, InvolvedTables[1], "t1");
			//}

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
			private string _constraintSql;
			private JoinConstraint _constraint;
			private readonly TrSpatialJoin _parent;

			public TransformedFc(IReadOnlyFeatureClass t0, IReadOnlyFeatureClass t1,
			                     TrSpatialJoin parent,
			                     string name = null)
				: base(-1, ! string.IsNullOrWhiteSpace(name) ? name : "intersectResult",
				       t0.ShapeType,
				       createBackingDataset: (t) =>
					       new TransformedDataset((TransformedFc) t, t0, t1),
				       workspace: new GdbWorkspace(new TransformerWorkspace()))
			{
				_parent = parent;
				InvolvedTables = new List<IReadOnlyTable> {t0, t1};
			}

			public Dictionary<IReadOnlyTable, TransformedTableFields> TableFieldsBySource { get; }
				= new Dictionary<IReadOnlyTable, TransformedTableFields>();

			public bool Grouped { get; set; }
			public bool OuterJoin { get; set; }
			public SearchOption NeighborSearchOption { get; set; }

			public string Constraint
			{
				get => _constraintSql;
				set
				{
					_constraintSql = value;
					_constraint = null;
				}
			}

			public bool HasFulfilledConstraint(IReadOnlyRow t0, IReadOnlyRow t1)
			{
				_constraint = _constraint
				              ?? new JoinConstraint(Constraint,
				                                    caseSensitive: _parent.GetSqlCaseSensitivity());
				return _constraint.IsFulfilled(t0, 0, t1, 1, out string _);
			}

			public override GdbRow CreateObject(int oid,
			                                    IValueList valueList = null)
			{
				return new TfcFeature(oid, this);
			}

			public IList<IReadOnlyTable> InvolvedTables { get; }

			public IDataContainer DataContainer
			{
				get => BackingDs.DataContainer;
				set => BackingDs.DataContainer = value;
			}

			public TransformedDataset BackingDs => (TransformedDataset) BackingDataset;
		}

		private class TfcFeature : GdbFeature
		{
			public TfcFeature(int oid, TransformedFc featureClass)
				: base(oid, featureClass) { }

			public override object get_Value(int index)
			{
				IField f = Table.Fields.Field[index];
				if (f.Name.StartsWith("t0."))
				{
					return GetBaseValue(0, f.Name.Substring(3));
				}

				if (f.Name.StartsWith("t1."))
				{
					return GetBaseValue(1, f.Name.Substring(3));
				}

				return base.get_Value(index);
			}

			private object GetBaseValue(int baseRowIndex, string attrName)
			{
				int baseRowsIdx = Table.Fields.FindField(InvolvedRowUtils.BaseRowField);
				IList<IReadOnlyRow> baseRows = (IList<IReadOnlyRow>) get_Value(baseRowsIdx);
				IReadOnlyRow baseRow = baseRows[baseRowIndex];

				int idx = baseRow.Table.FindField(attrName);
				return baseRow.get_Value(idx);
			}
		}

		private class TransformedDataset : TransformedBackingDataset
		{
			private readonly IReadOnlyFeatureClass _t0;
			private readonly IReadOnlyFeatureClass _t1;

			public TransformedDataset(
				[NotNull] TransformedFc gdbTable,
				[NotNull] IReadOnlyFeatureClass t0,
				[NotNull] IReadOnlyFeatureClass t1)
				: base(gdbTable, CastToTables(t0, t1))
			{
				_t0 = t0;
				_t1 = t1;

				Resulting.SpatialReference = t0.SpatialReference;
			}

			public override IEnvelope Extent => _t0.Extent;

			public override VirtualRow GetUncachedRow(int id)
			{
				throw new NotImplementedException();
			}

			public override int GetRowCount(IQueryFilter queryFilter)
			{
				// TODO
				return _t0.RowCount(queryFilter);
			}

			public override IEnumerable<VirtualRow> Search(IQueryFilter filter, bool recycling)
			{
				IRelationalOperator t1LoadedExtent = null;
				TransformedFc r = (TransformedFc) Resulting;
				if (r.NeighborSearchOption == SearchOption.All)
				{
					t1LoadedExtent = (IRelationalOperator) DataContainer.GetLoadedExtent(_t1);
				}

				ISpatialFilter joinFilter = new SpatialFilterClass();
				joinFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;

				TransformedFc res = (TransformedFc) Resulting;
				bool grouped = res.Grouped;
				foreach (var toJoin in DataContainer.Search(
					         _t0, filter ?? new QueryFilterClass(), QueryHelpers[0]))
				{
					joinFilter.Geometry = ((IReadOnlyFeature) toJoin).Extent;
					var op = (IRelationalOperator) ((IReadOnlyFeature) toJoin).Shape;

					List<IReadOnlyRow> joineds = new List<IReadOnlyRow>();
					bool outerJoin = res.OuterJoin;
					foreach (var joined in EnumNeighbors(joinFilter, t1LoadedExtent))
					{
						if (! res.HasFulfilledConstraint(toJoin, joined))
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

						if (! grouped)
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

			private IEnumerable<IReadOnlyRow> EnumNeighbors(
				[NotNull] ISpatialFilter joinFilter,
				[CanBeNull] IRelationalOperator loaded)
			{
				foreach (var joined in DataContainer.Search(
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
				var transformedFc = ((TransformedFc) Resulting);

				TransformedTableFields toJoinTableFields = transformedFc.TableFieldsBySource[_t0];
				TransformedTableFields joinedTableFields = transformedFc.TableFieldsBySource[_t1];

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
				rowValues.AddList(toJoinValues, toJoinTableFields.FieldIndexMapping);

				extraValues.AddRange(GetCalculatedValues(toJoin, toJoinTableFields, baseRows));

				// 2. The joined row(s):
				if (joineds.Count == 1)
				{
					var joinedValues = new ReadOnlyRowBasedValues(joineds[0]);
					rowValues.AddList(joinedValues, joinedTableFields.FieldIndexMapping);

					extraValues.AddRange(GetCalculatedValues(joineds[0], joinedTableFields));
				}
				else
				{
					extraValues.AddRange(joinedTableFields.GetCalculatedValues(joineds));
				}

				// Add all the collected extra values with their own copy-matrix:
				IValueList simpleList =
					TransformedAttributeUtils.ToSimpleValueList(
						extraValues, out IDictionary<int, int> extraCopyMatrix);

				rowValues.AddList(simpleList, extraCopyMatrix);

				return new GdbFeature(toJoin.OID, transformedFc, rowValues);
			}

			private IEnumerable<CalculatedValue> GetCalculatedValues(
				[NotNull] IReadOnlyRow sourceRow,
				[NotNull] TransformedTableFields sourceTableFields,
				[CanBeNull] IList<IReadOnlyRow> involvedBaseRowsToAdd = null)
			{
				// Base rows, if requested:
				if (involvedBaseRowsToAdd != null)
				{
					int baseRowsIdx = Resulting.FindField(InvolvedRowUtils.BaseRowField);
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
