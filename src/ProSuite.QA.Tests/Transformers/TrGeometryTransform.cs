using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers
{
	public interface IGeometryTransformer
	{
		IGeometry Transform(IGeometry source);
	}

	[UsedImplicitly]
	public abstract class TrGeometryTransform : ITableTransformer<IFeatureClass>,
	                                            IGeometryTransformer
	{
		private readonly IFeatureClass _fc;
		private readonly Tfc _transformedFc;

		private IList<string> _attributes;

		public IList<ITable> InvolvedTables { get; }

		protected TrGeometryTransform([NotNull] IFeatureClass fc, esriGeometryType derivedShapeType)
		{
			_fc = fc;
			InvolvedTables = new List<ITable> {(ITable) fc};

			_transformedFc = new Tfc(_fc, derivedShapeType, this);
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

		IGeometry IGeometryTransformer.Transform(IGeometry source) => Transform(source);

		protected abstract IGeometry Transform(IGeometry source);

		private class Tfc : GdbFeatureClass, ITransformedValue
		{
			public Tfc(IFeatureClass fc, esriGeometryType derivedShapeType,
			           IGeometryTransformer transformer)
				: base(-1, "derivedGeometry", derivedShapeType,
				       createBackingDataset: (t) => new Transformed((Tfc) t, fc),
				       workspace: new GdbWorkspace(new TransformedWs()))
			{
				Transformer = transformer;
				InvolvedTables = new List<ITable> {(ITable) fc};

				IGeometryDef geomDef =
					fc.Fields.Field[
						fc.Fields.FindField(fc.ShapeFieldName)].GeometryDef;
				Fields.AddFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						derivedShapeType,
						geomDef.SpatialReference, geomDef.GridSize[0], geomDef.HasZ, geomDef.HasM));

				AddFields(fc);
			}

			public IGeometryTransformer Transformer { get; }

			protected override IObject CreateObject(int oid)
			{
				return new TfcFeature(oid, this);
			}

			private void AddFields(IFeatureClass fc)
			{
				for (int iField = 0; iField < fc.Fields.FieldCount; iField++)
				{
					IField f = fc.Fields.Field[iField];
					Fields.AddFields(FieldUtils.CreateField($"t0.{f.Name}", f.Type));
				}
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
				if (f.Name.StartsWith("t0."))
				{
					int baseRowsIdx = Table.Fields.FindField(InvolvedRowUtils.BaseRowField);
					IList<IRow> baseRows = (IList<IRow>) get_Value(baseRowsIdx);
					IRow sourceRow = baseRows[0];

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
			private readonly Tfc _tfc; // == Resulting
			private readonly IFeatureClass _t0;

			public Transformed(
				[NotNull] Tfc tfc,
				[NotNull] IFeatureClass t0)
				: base(tfc, ProcessBase.CastToTables(t0))
			{
				_tfc = tfc;
				_t0 = t0;
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
				foreach (var row in DataContainer.Search(
					(ITable) _t0, filter, QueryHelpers[0]))
				{
					GdbFeature f = Resulting.CreateFeature();
					f.Shape = _tfc.Transformer.Transform(((IFeature) row).Shape);
					f.Store();

					List<IRow> involved = new List<IRow> {row};
					f.set_Value(
						Resulting.FindField(InvolvedRowUtils.BaseRowField),
						involved); // TODO

					yield return f;
				}
			}
		}
	}
}
