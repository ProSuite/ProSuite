using System;
using System.Collections.Generic;
using System.Data;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public class TrSpatialJoin : TableTransformer<IReadOnlyFeatureClass>
	{
		public enum SearchOption
		{
			Tile,
			All
		}

		private const SearchOption _defaultSearchOption = SearchOption.Tile;

		[DocTr(nameof(DocTrStrings.TrSpatialJoin_0))]
		public TrSpatialJoin(
			[NotNull] [DocTr(nameof(DocTrStrings.TrSpatialJoin_t0))] IReadOnlyFeatureClass t0,
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

			AddFields(transformedFc, T0Attributes, InvolvedTables[0], isGrouped: false);
			AddFields(transformedFc, T1Attributes, InvolvedTables[1], isGrouped: Grouped, T1CalcAttributes);
			return transformedFc;
		}

		private void AddFields([NotNull] TransformedFc transformedFc,
		                       [CanBeNull] IList<string> fieldNames,
							   IReadOnlyTable sourceTable, bool isGrouped,
							   [CanBeNull] IList<string> calcAttributes = null)
		{
			if (fieldNames == null)
			{
				return;
			}

			Dictionary<string, string> expressionDict = ExpressionUtils.GetFieldDict(fieldNames);
			Dictionary<string, string> aliasFieldDict =
				ExpressionUtils.CreateAliases(expressionDict);
			Dictionary<string, string> calcExpressionDict = null;

			if (calcAttributes?.Count > 0)
			{
				calcExpressionDict = ExpressionUtils.GetFieldDict(calcAttributes);
				Dictionary<string, string> calcAliasFieldDict = ExpressionUtils.CreateAliases(calcExpressionDict);

				foreach (KeyValuePair<string, string> pair in aliasFieldDict)
				{
					calcAliasFieldDict.Add(pair.Key, pair.Value);
				}
				aliasFieldDict = calcAliasFieldDict;
			}

			TableView tv =
				TableViewFactory.Create(sourceTable, expressionDict, aliasFieldDict, isGrouped, calcExpressionDict);

			transformedFc.TableViews =
				transformedFc.TableViews ?? new Dictionary<IReadOnlyTable, TableView>();
			transformedFc.TableViews[sourceTable] = tv;

			foreach (string field in expressionDict.Keys)
			{
				transformedFc.AddField(field, tv);
			}
		}

		private class JoinConstraint : RowPairCondition
		{
			public JoinConstraint([NotNull] string constraint, bool caseSensitive)
				: base(constraint, isDirected: true, undefinedConditionIsFulfilled: true,
				       row1Alias: "T0", row2Alias: "T1", caseSensitive: caseSensitive,
				       conciseMessage: true) { }
		}

		private class TransformedFc : TransformedFeatureClass, ITransformedValue
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
				InvolvedTables = new List<IReadOnlyTable> { t0, t1 };

				IGeometryDef geomDef =
					t0.Fields.Field[
						t0.Fields.FindField(t0.ShapeFieldName)].GeometryDef;
				FieldsT.AddFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						t0.ShapeType,
						geomDef.SpatialReference, geomDef.GridSize[0], geomDef.HasZ, geomDef.HasM));
			}

			public Dictionary<IReadOnlyTable, TableView> TableViews { get; set; }
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

			protected override VirtualRow CreateObject(int oid)
			{
				return new TfcFeature(oid, this);
			}

			public Dictionary<TableView, List<FieldInfo>> CalcFields { get; private set; }

			public void AddField(string field, TableView tableView)
			{
				IField f =
					FieldUtils.CreateField(
						field, FieldUtils.GetFieldType(tableView.GetColumn(field).DataType));
				FieldsT.AddFields(f);

				CalcFields = CalcFields ?? new Dictionary<TableView, List<FieldInfo>>();

				if (! CalcFields.TryGetValue(tableView, out List<FieldInfo> fieldInfos))
				{
					fieldInfos = new List<FieldInfo>();
					CalcFields.Add(tableView, fieldInfos);
				}

				fieldInfos.Add(new FieldInfo(field, Fields.FindField(field), -1));
			}

			public IList<IReadOnlyTable> InvolvedTables { get; }

			public ISearchable DataContainer
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
				if (f.Name.StartsWith("t0.") || f.Name.StartsWith("t1."))
				{
					int baseRowsIdx = Table.Fields.FindField(InvolvedRowUtils.BaseRowField);
					IList<IReadOnlyRow> baseRows = (IList<IReadOnlyRow>) get_Value(baseRowsIdx);
					IReadOnlyRow sourceRow = f.Name.StartsWith("t0.") ? baseRows[0] : baseRows[1];

					int idx = sourceRow.Table.FindField(f.Name.Substring(3));
					return sourceRow.get_Value(idx);
				}

				return base.get_Value(index);
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
				TransformedFc r = (TransformedFc)Resulting;
				if (r.NeighborSearchOption == SearchOption.All)
				{
					t1LoadedExtent = (IRelationalOperator)DataContainer.GetLoadedExtent(_t1);
				}


				ISpatialFilter joinFilter = new SpatialFilterClass();
				joinFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;

				TransformedFc res = (TransformedFc) Resulting;
				bool grouped = res.Grouped;
				foreach (var toJoin in DataContainer.Search(
					         _t0, filter, QueryHelpers[0]))
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
							var f = CreateFeature(toJoin, new[] { joined });
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
				[NotNull]ISpatialFilter joinFilter,
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

			private GdbFeature CreateFeature(IReadOnlyRow toJoin, IList<IReadOnlyRow> joineds)
			{
				GdbFeature f = Resulting.CreateFeature();
				f.Shape = ((IReadOnlyFeature) toJoin).Shape;
				f.Store();

				List<IReadOnlyRow> involved = new List<IReadOnlyRow>(joineds.Count + 1);
				involved.Add(toJoin);
				involved.AddRange(joineds);
				f.set_Value(
					Resulting.FindField(InvolvedRowUtils.BaseRowField),
					involved);

				SetValues(f, new[] { toJoin });
				SetValues(f, joineds);

				return f;
			}

			private void SetValues(IFeature feature, IList<IReadOnlyRow> sources)
			{
				TransformedFc r = (TransformedFc) Resulting;

				TableView tv = null;
				DataRow tableRow = null;
				foreach (IReadOnlyRow row in sources)
				{
					if (tv == null)
					{
						r.TableViews?.TryGetValue(row.Table, out tv);
					}

					tableRow = tv?.Add(row);
				}

				if (tableRow != null)
				{
					foreach (FieldInfo fieldInfo in r.CalcFields[tv])
					{
						feature.set_Value(fieldInfo.Index, tableRow[fieldInfo.Name]);
					}
				}

				tv?.ClearRows();
			}
		}
	}
}
