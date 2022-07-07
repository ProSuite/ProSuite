using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public class TrIntersect : TableTransformer<IReadOnlyFeatureClass>
	{
		private readonly IReadOnlyFeatureClass _intersected;
		private readonly IReadOnlyFeatureClass _intersecting;

		[DocTr(nameof(DocTrStrings.TrIntersect_0))]
		public TrIntersect(
			[NotNull, DocTr(nameof(DocTrStrings.TrIntersect_intersected))]
			IReadOnlyFeatureClass intersected,
			[NotNull, DocTr(nameof(DocTrStrings.TrIntersect_intersecting))]
			IReadOnlyFeatureClass intersecting)
			: base(new List<IReadOnlyTable> {intersected, intersecting})
		{
			_intersected = intersected;
			_intersecting = intersecting;
		}

		protected override TransformedFeatureClass GetTransformedCore(string name)
		{
			TransformedFc transformedFc = new TransformedFc(_intersected, _intersecting, name);
			return transformedFc;
		}

		private class TransformedFc : TransformedFeatureClass, ITransformedValue
		{
			public TransformedFc(IReadOnlyFeatureClass intersected,
			                     IReadOnlyFeatureClass intersecting,
			                     string name = null)
				: base(-1, ! string.IsNullOrWhiteSpace(name) ? name : "intersectResult",
				       intersected.ShapeType,
				       createBackingDataset: (t) =>
					       new TransformedDataset((TransformedFc) t, intersected, intersecting),
				       workspace: new GdbWorkspace(new TransformerWorkspace()))
			{
				InvolvedTables = new List<IReadOnlyTable> {intersected, intersecting};

				IGeometryDef geomDef =
					intersected.Fields.Field[
						intersected.Fields.FindField(intersected.ShapeFieldName)].GeometryDef;
				FieldsT.AddFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						intersected.ShapeType, // TODO: only true for intersecting is Polygon FC
						geomDef.SpatialReference, geomDef.GridSize[0], geomDef.HasZ, geomDef.HasM));
			}

			protected override VirtualRow CreateObject(int oid)
			{
				return new TransformedFeature(oid, this);
			}

			public IList<IReadOnlyTable> InvolvedTables { get; }

			public ISearchable DataContainer
			{
				get => BackingDs.DataContainer;
				set => BackingDs.DataContainer = value;
			}

			public TransformedDataset BackingDs => (TransformedDataset) BackingDataset;
		}

		private class TransformedFeature : GdbFeature
		{
			public TransformedFeature(int oid, TransformedFc featureClass)
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
			private const string PartIntersectedField = "PartIntersected";
			private readonly IReadOnlyFeatureClass _intersected;
			private readonly IReadOnlyFeatureClass _intersecting;

			public TransformedDataset(
				[NotNull] TransformedFc gdbTable,
				[NotNull] IReadOnlyFeatureClass intersected,
				[NotNull] IReadOnlyFeatureClass intersecting)
				: base(gdbTable, CastToTables(intersected, intersecting))
			{
				_intersected = intersected;
				_intersecting = intersecting;

				gdbTable.AddField(FieldUtils.CreateDoubleField(PartIntersectedField));

				AddFields(gdbTable, intersected, "t0");
				AddFields(gdbTable, intersecting, "t1");

				Resulting.SpatialReference = intersected.SpatialReference;
			}

			private void AddFields(TransformedFc gdbTable, IReadOnlyFeatureClass fc, string prefix)
			{
				for (int iField = 0; iField < fc.Fields.FieldCount; iField++)
				{
					IField f = fc.Fields.Field[iField];
					gdbTable.FieldsT.AddFields(
						FieldUtils.CreateField($"{prefix}.{f.Name}", f.Type));
				}
			}

			public override IEnvelope Extent => _intersected.Extent;

			public override VirtualRow GetUncachedRow(int id)
			{
				throw new NotImplementedException();
			}

			public override int GetRowCount(IQueryFilter queryFilter)
			{
				// TODO
				return _intersected.RowCount(queryFilter);
			}

			public override IEnumerable<VirtualRow> Search(IQueryFilter filter, bool recycling)
			{
				filter = filter ?? new QueryFilterClass();

				ISpatialFilter intersectingFilter = new SpatialFilterClass();
				intersectingFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;

				int iPartIntersected = Resulting.FindField(PartIntersectedField);

				foreach (var toIntersect in DataContainer.Search(
					         _intersected, filter, QueryHelpers[0]))
				{
					intersectingFilter.Geometry = ((IReadOnlyFeature) toIntersect).Extent;
					foreach (var intersecting in DataContainer.Search(
						         _intersecting, intersectingFilter, QueryHelpers[1]))
					{
						IGeometry intersectingGeom = ((IReadOnlyFeature) intersecting).Shape;
						var op = (ITopologicalOperator) ((IReadOnlyFeature) toIntersect).Shape;
						if (((IRelationalOperator) op).Disjoint(intersectingGeom))
						{
							continue;
						}

						IGeometry intersected = op.Intersect(
							intersectingGeom,
							esriGeometryDimension.esriGeometry1Dimension); // TODO

						if (intersected.IsEmpty)
						{
							continue;
						}

						double fullLength = ((IPolyline) op).Length;
						double partLength = ((IPolyline) intersected).Length;

						GdbFeature f = Resulting.CreateFeature();
						f.Shape = intersected;
						f.set_Value(iPartIntersected, partLength / fullLength);
						f.Store();

						f.set_Value(
							Resulting.FindField(InvolvedRowUtils.BaseRowField),
							new List<IReadOnlyRow> {toIntersect, intersecting}); // TODO

						yield return f;
					}
				}
			}
		}
	}
}
