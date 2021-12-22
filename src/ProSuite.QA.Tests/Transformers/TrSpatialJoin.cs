using System;
using System.Collections.Generic;
using System.Data;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public class TrSpatialJoin : TableTransformer<IFeatureClass>
	{
		[Doc(nameof(DocStrings.TrSpatialJoin_0))]
		public TrSpatialJoin([NotNull] [Doc(nameof(DocStrings.TrSpatialJoin_t0))] IFeatureClass t0,
		                     [NotNull] [Doc(nameof(DocStrings.TrSpatialJoin_t1))]
		                     IFeatureClass t1)
			: base(CastToTables(t0, t1)) { }

		[TestParameter]
		[Doc(nameof(DocStrings.TrSpatialJoin_Constraint))]
		public string Constraint { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.TrSpatialJoin_OuterJoin))]
		public bool OuterJoin { get; set; }

		// Remark: Grouped must come in Code before T1Attributes !
		[TestParameter]
		[Doc(nameof(DocStrings.TrSpatialJoin_Grouped))]
		public bool Grouped { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.TrSpatialJoin_T0Attributes))]
		public IList<string> T0Attributes { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.TrSpatialJoin_T1Attributes))]
		public IList<string> T1Attributes { get; set; }

		protected override IFeatureClass GetTransformedCore(string name)
		{
			TransformedFc transformedFc = new TransformedFc((IFeatureClass) InvolvedTables[0],
			                                                (IFeatureClass) InvolvedTables[1], this,
			                                                name);
			transformedFc.Constraint = Constraint;
			transformedFc.OuterJoin = OuterJoin;
			transformedFc.Grouped = Grouped;
			AddFields(transformedFc, T0Attributes, InvolvedTables[0], isGrouped: false);
			AddFields(transformedFc, T1Attributes, InvolvedTables[1], isGrouped: Grouped);
			return transformedFc;
		}

		private void AddFields([NotNull] TransformedFc transformedFc,
		                       [CanBeNull] IList<string> fieldNames,
		                       ITable sourceTable, bool isGrouped)
		{
			if (fieldNames == null)
			{
				return;
			}

			Dictionary<string, string> expressionDict = ExpressionUtils.GetFieldDict(fieldNames);
			Dictionary<string, string> aliasFieldDict =
				ExpressionUtils.CreateAliases(expressionDict);

			TableView tv =
				TableViewFactory.Create(sourceTable, expressionDict, aliasFieldDict, isGrouped);

			transformedFc.TableViews =
				transformedFc.TableViews ?? new Dictionary<ITable, TableView>();
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

		private class TransformedFc : GdbFeatureClass, ITransformedValue
		{
			private string _constraintSql;
			private JoinConstraint _constraint;
			private readonly TrSpatialJoin _parent;

			public TransformedFc(IFeatureClass t0, IFeatureClass t1, TrSpatialJoin parent,
			                     string name = null)
				: base(-1, ! string.IsNullOrWhiteSpace(name) ? name : "intersectResult",
				       t0.ShapeType,
				       createBackingDataset: (t) =>
					       new TransformedDataset((TransformedFc) t, t0, t1),
				       workspace: new GdbWorkspace(new TransformerWorkspace()))
			{
				_parent = parent;
				InvolvedTables = new List<ITable> { (ITable) t0, (ITable) t1 };

				IGeometryDef geomDef =
					t0.Fields.Field[
						t0.Fields.FindField(t0.ShapeFieldName)].GeometryDef;
				Fields.AddFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						t0.ShapeType,
						geomDef.SpatialReference, geomDef.GridSize[0], geomDef.HasZ, geomDef.HasM));
			}

			public Dictionary<ITable, TableView> TableViews { get; set; }
			public bool Grouped { get; set; }
			public bool OuterJoin { get; set; }

			public string Constraint
			{
				get => _constraintSql;
				set
				{
					_constraintSql = value;
					_constraint = null;
				}
			}

			public bool HasFulfilledConstraint(IRow t0, IRow t1)
			{
				_constraint = _constraint
				              ?? new JoinConstraint(Constraint,
				                                    caseSensitive: _parent.GetSqlCaseSensitivity());
				return _constraint.IsFulfilled(t0, 0, t1, 1, out string conditionMessage);
			}

			protected override IObject CreateObject(int oid)
			{
				return new TfcFeature(oid, this);
			}

			public Dictionary<TableView, List<FieldInfo>> CalcFields { get; private set; }

			public void AddField(string field, TableView tableView)
			{
				IField f =
					FieldUtils.CreateField(
						field, FieldUtils.GetFieldType(tableView.GetColumn(field).DataType));
				Fields.AddFields(f);

				CalcFields = CalcFields ?? new Dictionary<TableView, List<FieldInfo>>();

				if (! CalcFields.TryGetValue(tableView, out List<FieldInfo> fieldInfos))
				{
					fieldInfos = new List<FieldInfo>();
					CalcFields.Add(tableView, fieldInfos);
				}

				fieldInfos.Add(new FieldInfo(field, Fields.FindField(field), -1));
			}

			public IList<ITable> InvolvedTables { get; }

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
					IList<IRow> baseRows = (IList<IRow>) get_Value(baseRowsIdx);
					IRow sourceRow = f.Name.StartsWith("t0.") ? baseRows[0] : baseRows[1];

					int idx = sourceRow.Table.FindField(f.Name.Substring(3));
					return sourceRow.get_Value(idx);
				}

				return base.get_Value(index);
			}
		}

		private class TransformedDataset : TransformedBackingDataset
		{
			private readonly IFeatureClass _t0;
			private readonly IFeatureClass _t1;

			public TransformedDataset(
				[NotNull] TransformedFc gdbTable,
				[NotNull] IFeatureClass t0,
				[NotNull] IFeatureClass t1)
				: base(gdbTable, CastToTables(t0, t1))
			{
				_t0 = t0;
				_t1 = t1;

				Resulting.SpatialReference = ((IGeoDataset) t0).SpatialReference;
			}

			public override IEnvelope Extent => ((IGeoDataset) _t0).Extent;

			public override IRow GetRow(int id)
			{
				throw new NotImplementedException();
			}

			public override int GetRowCount(IQueryFilter queryFilter)
			{
				// TODO
				return _t0.FeatureCount(queryFilter);
			}

			public override IEnumerable<IRow> Search(IQueryFilter filter, bool recycling)
			{
				ISpatialFilter joinFilter = new SpatialFilterClass();
				joinFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;

				TransformedFc res = (TransformedFc) this.Resulting;
				bool grouped = res.Grouped;
				foreach (var toJoin in DataContainer.Search(
					         (ITable) _t0, filter, QueryHelpers[0]))
				{
					joinFilter.Geometry = ((IFeature) toJoin).Extent;
					var op = (IRelationalOperator) ((IFeature) toJoin).Shape;

					List<IRow> joineds = new List<IRow>();
					bool outerJoin = res.OuterJoin;
					foreach (var joined in DataContainer.Search(
						         (ITable) _t1, joinFilter, QueryHelpers[1]))
					{
						if (! res.HasFulfilledConstraint(toJoin, joined))
						{
							continue;
						}

						outerJoin = false;

						IGeometry joinedGeom = ((IFeature) joined).Shape;
						// TODO implement different relations
						if (op.Disjoint(joinedGeom))
						{
							continue;
						}

						if (! grouped)
						{
							GdbFeature f = CreateFeature(toJoin, new[] { joined });
							res.CreateFeature();
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

			private GdbFeature CreateFeature(IRow toJoin, IList<IRow> joineds)
			{
				GdbFeature f = Resulting.CreateFeature();
				f.Shape = ((IFeature) toJoin).Shape;
				f.Store();

				List<IRow> involved = new List<IRow>(joineds.Count + 1);
				involved.Add(toJoin);
				involved.AddRange(joineds);
				f.set_Value(
					Resulting.FindField(InvolvedRowUtils.BaseRowField),
					involved);

				SetValues(f, new[] { toJoin });
				SetValues(f, joineds);

				return f;
			}

			private void SetValues(GdbFeature feature, IList<IRow> sources)
			{
				TransformedFc r = (TransformedFc) Resulting;

				TableView tv = null;
				DataRow tableRow = null;
				foreach (IRow row in sources)
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
