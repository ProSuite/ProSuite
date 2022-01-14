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
	[UsedImplicitly]
	public class TrIntersect : TableTransformer<IFeatureClass>
	{
		private readonly IFeatureClass _intersected;
		private readonly IFeatureClass _intersecting;

		public TrIntersect([NotNull] IFeatureClass intersected,
											 [NotNull] IFeatureClass intersecting)
			: base(new List<ITable> { (ITable)intersected, (ITable)intersecting })
		{
			_intersected = intersected;
			_intersecting = intersecting;
		}

		protected override IFeatureClass GetTransformedCore(string name)
		{
			TransformedFc transformedFc = new TransformedFc(_intersected, _intersecting, name);
			return transformedFc;
		}

		private class TransformedFc : TransformedFeatureClass, ITransformedValue
		{
			public TransformedFc(IFeatureClass intersected, IFeatureClass intersecting,
													 string name = null)
				: base(-1, !string.IsNullOrWhiteSpace(name) ? name : "intersectResult",
							 intersected.ShapeType,
							 createBackingDataset: (t) =>
								 new TransformedDataset((TransformedFc)t, intersected, intersecting),
							 workspace: new GdbWorkspace(new TransformerWorkspace()))
			{
				InvolvedTables = new List<ITable> { (ITable)intersected, (ITable)intersecting };

				IGeometryDef geomDef =
					intersected.Fields.Field[
						intersected.Fields.FindField(intersected.ShapeFieldName)].GeometryDef;
				FieldsT.AddFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						intersected.ShapeType, // TODO: only true for intersecting is Polygon FC
						geomDef.SpatialReference, geomDef.GridSize[0], geomDef.HasZ, geomDef.HasM));
			}

			public IList<ITable> InvolvedTables { get; }

			public ISearchable DataContainer
			{
				get => BackingDs.DataContainer;
				set => BackingDs.DataContainer = value;
			}

			public TransformedDataset BackingDs => (TransformedDataset)BackingDataset;
		}

		private class TransformedDataset : TransformedBackingDataset
		{
			private const string PartIntersectedField = "PartIntersected";
			private readonly IFeatureClass _intersected;
			private readonly IFeatureClass _intersecting;

			public TransformedDataset(
				[NotNull] TransformedFc gdbTable,
				[NotNull] IFeatureClass intersected,
				[NotNull] IFeatureClass intersecting)
				: base(gdbTable, CastToTables(intersected, intersecting))
			{
				_intersected = intersected;
				_intersecting = intersecting;

				gdbTable.AddField(FieldUtils.CreateDoubleField(PartIntersectedField));

				Resulting.SpatialReference = ((IGeoDataset)intersected).SpatialReference;
			}

			public override IEnvelope Extent => ((IGeoDataset)_intersected).Extent;

			public override IRow GetUncachedRow(int id)
			{
				throw new NotImplementedException();
			}

			public override int GetRowCount(IQueryFilter queryFilter)
			{
				// TODO
				return _intersected.FeatureCount(queryFilter);
			}

			public override IEnumerable<IRow> Search(IQueryFilter filter, bool recycling)
			{
				ISpatialFilter intersectingFilter = new SpatialFilterClass();
				intersectingFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;

				int iPartIntersected = Resulting.FindField(PartIntersectedField);

				foreach (var toIntersect in DataContainer.Search(
									 (ITable)_intersected, filter, QueryHelpers[0]))
				{
					intersectingFilter.Geometry = ((IFeature)toIntersect).Extent;
					foreach (var intersecting in DataContainer.Search(
										 (ITable)_intersecting, intersectingFilter, QueryHelpers[1]))
					{
						IGeometry intersectingGeom = ((IFeature)intersecting).Shape;
						var op = (ITopologicalOperator)((IFeature)toIntersect).Shape;
						if (((IRelationalOperator)op).Disjoint(intersectingGeom))
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

						double fullLength = ((IPolyline)op).Length;
						double partLength = ((IPolyline)intersected).Length;

						IFeature f = Resulting.CreateFeature();
						f.Shape = intersected;
						f.set_Value(iPartIntersected, partLength / fullLength);
						f.Store();

						f.set_Value(
							Resulting.FindField(InvolvedRowUtils.BaseRowField),
							new List<IRow> { toIntersect, intersecting }); // TODO

						yield return f;
					}
				}
			}
		}
	}
}
