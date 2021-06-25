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
	public class TrDissolve : ITableTransformer<IFeatureClass>
	{
		private readonly Tfc _dissolvedFc;

		public IList<ITable> InvolvedTables { get; }

		public TrDissolve([NotNull] IFeatureClass featureclass)
		{
			InvolvedTables = new List<ITable> {(ITable) featureclass};

			_dissolvedFc = new Tfc(featureclass);
		}

		[TestParameter]
		public double Search { get; set; }

		[TestParameter]
		public bool CompleteMissingParts { get; set; } // TODO: implement behavior

		public IFeatureClass GetTransformed() => _dissolvedFc;

		object ITableTransformer.GetTransformed() => GetTransformed();

		void IInvolvesTables.SetConstraint(int tableIndex, string condition)
		{
			_dissolvedFc.BackingDs.SetConstraint(tableIndex, condition);
		}

		void IInvolvesTables.SetSqlCaseSensitivity(int tableIndex, bool useCaseSensitiveQaSql)
		{
			_dissolvedFc.BackingDs.SetSqlCaseSensitivity(tableIndex, useCaseSensitiveQaSql);
		}

		private class Tfc : GdbFeatureClass, ITransformedValue
		{
			public Tfc(IFeatureClass dissolve)
				: base(1, "dissolveResult", dissolve.ShapeType,
				       createBackingDataset: (t) => new Transformed(t, dissolve),
				       workspace: new GdbWorkspace(new TransformedWs()))
			{
				InvolvedTables = new List<ITable> {(ITable) dissolve};

				IGeometryDef geomDef =
					dissolve.Fields.Field[
						dissolve.Fields.FindField(dissolve.ShapeFieldName)].GeometryDef;
				Fields.AddFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						dissolve.ShapeType,
						geomDef.SpatialReference, geomDef.GridSize[0], geomDef.HasZ, geomDef.HasM));
			}

			public IList<ITable> InvolvedTables { get; }

			public ISearchable DataContainer
			{
				get => BackingDs.DataContainer;
				set => BackingDs.DataContainer = value;
			}

			public TransformedFeatureClass BackingDs => (Transformed) BackingDataset;
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
			private readonly IFeatureClass _dissolve;

			public Transformed(
				[NotNull] GdbTable gdbTable,
				[NotNull] IFeatureClass dissolve) :
				base(gdbTable, ProcessBase.CastToTables(dissolve))
			{
				_dissolve = dissolve;

				Resulting.SpatialReference = ((IGeoDataset) _dissolve).SpatialReference;
			}

			public override IEnvelope Extent => ((IGeoDataset) _dissolve).Extent;

			public override IRow GetRow(int id)
			{
				throw new NotImplementedException();
			}

			public override int GetRowCount(IQueryFilter queryFilter)
			{
				// TODO
				return _dissolve.FeatureCount(queryFilter);
			}

			public override IEnumerable<IRow> Search(IQueryFilter filter, bool recycling)
			{
				List<IRow> tileRows = new List<IRow>();
				foreach (var baseRow in DataContainer.Search(
					(ITable) _dissolve, filter, QueryHelpers[0]))
				{
					tileRows.Add(baseRow);
				}

				if (tileRows.Count == 0)
				{
					yield break;
				}

				// TODO: dissolve
				foreach (IRow tileRow in tileRows)
				{
					GdbFeature dissolved = Resulting.CreateFeature();
					dissolved.Shape = ((IFeature) tileRow).Shape;
					dissolved.Store();

					dissolved.set_Value(
						Resulting.FindField(InvolvedRowUtils.BaseRowField),
						new List<IRow> {tileRow}); // TODO

					yield return dissolved;
				}
			}
		}
	}
}
