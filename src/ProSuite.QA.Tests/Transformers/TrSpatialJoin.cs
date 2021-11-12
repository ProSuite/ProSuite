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
	public class TrSpatialJoin : ITableTransformer<IFeatureClass>
	{
		private readonly IFeatureClass _t0;
		private readonly IFeatureClass _t1;
		private readonly Tfc _transformedFc;

		private IList<string> _t0Attributes;
		private IList<string> _t1Attributes;

		public IList<ITable> InvolvedTables { get; }

		[Doc(nameof(DocStrings.TrSpatialJoin_0))]
		public TrSpatialJoin([NotNull] [Doc(nameof(DocStrings.TrSpatialJoin_t0))]
		                     IFeatureClass t0,
		                     [NotNull] [Doc(nameof(DocStrings.TrSpatialJoin_t1))]
		                     IFeatureClass t1)
		{
			_t0 = t0;
			_t1 = t1;
			InvolvedTables = new List<ITable> {(ITable) t0, (ITable) t1};

			_transformedFc = new Tfc(_t0, _t1);
		}

		// Remark: Grouped must come in Code before T1Attributes !
		[TestParameter]
		[Doc(nameof(DocStrings.TrSpatialJoin_Grouped))]
		public bool Grouped
		{
			get => _transformedFc.Grouped;
			set => _transformedFc.Grouped = value;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.TrSpatialJoin_OuterJoin))]
		public bool OuterJoin
		{
			get => _transformedFc.OuterJoin;
			set => _transformedFc.OuterJoin = value;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.TrSpatialJoin_T0Attributes))]
		public IList<string> T0Attributes
		{
			get => _t0Attributes;
			set
			{
				_t0Attributes = value;
				AddFields(value, InvolvedTables[0], isGrouped: false);
			}
		}

		[TestParameter]
		[Doc(nameof(DocStrings.TrSpatialJoin_T1Attributes))]
		public IList<string> T1Attributes
		{
			get => _t1Attributes;
			set
			{
				_t1Attributes = value;
				AddFields(value, InvolvedTables[1], isGrouped: _transformedFc.Grouped);
			}
		}

		private void AddFields([CanBeNull] IList<string> fieldNames, ITable sourceTable,
		                       bool isGrouped)
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

			_transformedFc.TableViews =
				_transformedFc.TableViews ?? new Dictionary<ITable, TableView>();
			_transformedFc.TableViews[sourceTable] = tv;

			foreach (string field in expressionDict.Keys)
			{
				_transformedFc.AddField(field, tv);
			}
		}

		public IFeatureClass GetTransformed() => _transformedFc;

		object ITableTransformer.GetTransformed() => GetTransformed();

		void IInvolvesTables.SetConstraint(int tableIndex, string condition)
		{
			_transformedFc.BackingDs.SetConstraint(tableIndex, condition);
		}

		void IInvolvesTables.SetSqlCaseSensitivity(int tableIndex, bool useCaseSensitiveQaSql)
		{
			_transformedFc.BackingDs.SetSqlCaseSensitivity(tableIndex, useCaseSensitiveQaSql);
		}

		private class Tfc : GdbFeatureClass, ITransformedValue
		{
			public Dictionary<ITable, TableView> TableViews { get; set; }

			public Tfc(IFeatureClass t0, IFeatureClass t1)
				: base(-1, "intersectResult", t0.ShapeType,
				       createBackingDataset: (t) => new Transformed((Tfc) t, t0, t1),
				       workspace: new GdbWorkspace(new TransformedWs()))
			{
				InvolvedTables = new List<ITable> {(ITable) t0, (ITable) t1};

				IGeometryDef geomDef =
					t0.Fields.Field[
						t0.Fields.FindField(t0.ShapeFieldName)].GeometryDef;
				Fields.AddFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						t0.ShapeType,
						geomDef.SpatialReference, geomDef.GridSize[0], geomDef.HasZ, geomDef.HasM));
			}

			public bool Grouped { get; set; }
			public bool OuterJoin { get; set; }

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

			public TransformedFeatureClass BackingDs => (Transformed) BackingDataset;
		}

		private class TfcFeature : GdbFeature
		{
			public TfcFeature(int oid, Tfc featureClass)
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

		private class TransformedWs : BackingDataStore
		{
			public override void ExecuteSql(string sqlStatement)
			{
				throw new NotImplementedException();
			}

			public override IEnumerable<IDataset> GetDatasets(esriDatasetType datasetType)
			{
				throw new NotImplementedException();
			}

			public override ITable OpenQueryTable(string relationshipClassName)
			{
				throw new NotImplementedException();
			}

			public override ITable OpenTable(string name)
			{
				throw new NotImplementedException();
			}
		}

		private class Transformed : TransformedFeatureClass
		{
			private readonly IFeatureClass _t0;
			private readonly IFeatureClass _t1;

			public Transformed(
				[NotNull] Tfc gdbTable,
				[NotNull] IFeatureClass t0,
				[NotNull] IFeatureClass t1)
				: base(gdbTable, ProcessBase.CastToTables(t0, t1))
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

				bool grouped = ((Tfc) Resulting).Grouped;
				foreach (var toJoin in DataContainer.Search(
					(ITable) _t0, filter, QueryHelpers[0]))
				{
					joinFilter.Geometry = ((IFeature) toJoin).Extent;
					var op = (IRelationalOperator) ((IFeature) toJoin).Shape;

					List<IRow> joineds = new List<IRow>();
					bool outerJoin = ((Tfc) Resulting).OuterJoin;
					foreach (var joined in DataContainer.Search(
						(ITable) _t1, joinFilter, QueryHelpers[1]))
					{
						outerJoin = false;
						IGeometry joinedGeom = ((IFeature) joined).Shape;
						// TODO implement different relations
						if (op.Disjoint(joinedGeom))
						{
							continue;
						}

						if (! grouped)
						{
							GdbFeature f = CreateFeature(toJoin, new[] {joined});
							Resulting.CreateFeature();
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

				SetValues(f, new[] {toJoin});
				SetValues(f, joineds);

				return f;
			}

			private void SetValues(GdbFeature feature, IList<IRow> sources)
			{
				Tfc r = (Tfc) Resulting;

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
