using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public class TrSpatialJoin : ITableTransformer<IFeatureClass>
	{
		private readonly IFeatureClass _t0;
		private readonly IFeatureClass _t1;
		private readonly Tfc _transformedFc;

		private IList<string> _attributes;

		public IList<ITable> InvolvedTables { get; }

		public TrSpatialJoin([NotNull] IFeatureClass t0,
		                     [NotNull] IFeatureClass t1)
		{
			_t0 = t0;
			_t1 = t1;
			InvolvedTables = new List<ITable> {(ITable) t0, (ITable) t1};

			_transformedFc = new Tfc(_t0, _t1);
		}

		[TestParameter]
		public bool Grouped
		{
			get => _transformedFc.Grouped;
			set => _transformedFc.Grouped = value;
		}

		[TestParameter]
		public IList<string> Attributes
		{
			get => _attributes;
			set
			{
				_attributes = value;
				AddFields(value, addUniqueField: true);
			}
		}

		private void AddFields([CanBeNull] IList<string> fieldNames, bool addUniqueField)
		{
			if (fieldNames == null)
			{
				return;
			}

			ITable fc = InvolvedTables[1];

			foreach (string fieldName in fieldNames)
			{
				int iField = fc.FindField(fieldName);
				if (iField < 0)
				{
					throw new InvalidOperationException(
						$"Unkown field '{fieldName}' in '{DatasetUtils.GetName(fc)}'");
				}

				_transformedFc.AddField(fc.Fields.Field[iField], iField,
				                        addUniqueField);
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
			private List<FieldInfo> _joinFields;

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
						t0.ShapeType, // TODO: only true for intersecting is Polygon FC
						geomDef.SpatialReference, geomDef.GridSize[0], geomDef.HasZ, geomDef.HasM));

				AddFields(t0, "t0");
				AddFields(t1, "t1");
			}

			public List<FieldInfo> JoinFields =>
				_joinFields ?? (_joinFields = new List<FieldInfo>());

			public bool Grouped { get; set; }

			protected override IObject CreateObject(int oid)
			{
				return new TfcFeature(oid, this);
			}

			private void AddFields(IFeatureClass fc, string prefix)
			{
				for (int iField = 0; iField < fc.Fields.FieldCount; iField++)
				{
					IField f = fc.Fields.Field[iField];
					Fields.AddFields(FieldUtils.CreateField($"{prefix}.{f.Name}", f.Type));
				}
			}

			public void AddField(IField field, int sourceIndex, bool addUniqueField)
			{
				Fields.AddFields(FieldUtils.CreateField(field.Name, field.Type));
				FieldInfo fi = new FieldInfo(field.Name, Fields.FindField(field.Name), sourceIndex);
				if (addUniqueField)
				{
					string uniqueName = $"{field.Name}_unique";
					Fields.AddFields(
						FieldUtils.CreateField(uniqueName,
						                       esriFieldType.esriFieldTypeSmallInteger));
					fi.SetUnique(uniqueName, Fields.FindField(uniqueName));
				}

				JoinFields.Add(fi);
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
					foreach (var joined in DataContainer.Search(
						(ITable) _t1, joinFilter, QueryHelpers[1]))
					{
						IGeometry joinedGeom = ((IFeature) joined).Shape;
						// TODO implement different relations
						if (op.Disjoint(joinedGeom))
						{
							continue;
						}

						if (! grouped)
						{
							GdbFeature f = Resulting.CreateFeature();
							f.Shape = ((IFeature) toJoin).Shape;
							f.Store();

							f.set_Value(
								Resulting.FindField(InvolvedRowUtils.BaseRowField),
								new List<IRow> {toJoin, joined}); // TODO

							yield return f;
						}
						else
						{
							joineds.Add(joined);
						}
					}

					if (joineds.Count > 0)
					{
						GdbFeature f = Resulting.CreateFeature();
						f.Shape = ((IFeature) toJoin).Shape;
						f.Store();

						List<IRow> involved = new List<IRow>(joineds.Count + 1);
						involved.Add(toJoin);
						involved.AddRange(joineds);
						f.set_Value(
							Resulting.FindField(InvolvedRowUtils.BaseRowField),
							involved); // TODO

						FieldInfo.SetGroupValue(f, joineds, ((Tfc) Resulting).JoinFields);
						yield return f;
					}
				}
			}
		}
	}
}
