using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Container;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrIntersect : TableTransformer<TransformedFeatureClass>
	{
		private readonly IReadOnlyFeatureClass _intersected;
		private readonly IReadOnlyFeatureClass _intersecting;

		[DocTr(nameof(DocTrStrings.TrIntersect_0))]
		public TrIntersect(
			[NotNull, DocTr(nameof(DocTrStrings.TrIntersect_intersected))]
			IReadOnlyFeatureClass intersected,
			[NotNull, DocTr(nameof(DocTrStrings.TrIntersect_intersecting))]
			IReadOnlyFeatureClass intersecting)
			: base(new List<IReadOnlyTable> { intersected, intersecting })
		{
			_intersected = intersected;
			_intersecting = intersecting;
		}

		[InternallyUsedTest]
		public TrIntersect(
			[NotNull] TrIntersectDefinition definition)
			: this((IReadOnlyFeatureClass) definition.Intersected,
			       (IReadOnlyFeatureClass) definition.Intersecting) { }

		protected override TransformedFeatureClass GetTransformedCore(string name)
		{
			TransformedFc transformedFc = new TransformedFc(_intersected, _intersecting, name);
			return transformedFc;
		}

		private class TransformedFc : TransformedFeatureClass, IDataContainerAware
		{
			public TransformedFc(IReadOnlyFeatureClass intersected,
			                     IReadOnlyFeatureClass intersecting,
			                     string name = null)
				: base(null, ! string.IsNullOrWhiteSpace(name) ? name : "intersectResult",
				       intersected.ShapeType,
				       createBackingDataset: (t) =>
					       new TransformedDataset((TransformedFc) t, intersected, intersecting),
				       workspace: new GdbWorkspace(new TransformerWorkspace()))
			{
				InvolvedTables = new List<IReadOnlyTable> { intersected, intersecting };
			}

			public IList<IReadOnlyTable> InvolvedTables { get; }

			public IDataContainer DataContainer
			{
				get => BackingDs.DataContainer;
				set => BackingDs.DataContainer = value;
			}

			private TransformedDataset BackingDs => (TransformedDataset) BackingDataset;
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

				IntersectedFields = new TransformedTableFields(_intersected);
				IntersectingFields = new TransformedTableFields(_intersecting);

				IntersectedFields.AddOIDField(gdbTable, "OBJECTID", true);
				IntersectedFields.AddShapeField(gdbTable, "SHAPE", true);

				IntersectedFields.AddAllFields(gdbTable);
				IntersectingFields.AddAllFields(gdbTable);

				PartIntersectedFieldIndex =
					gdbTable.AddFieldT(FieldUtils.CreateDoubleField(PartIntersectedField));

				Resulting.SpatialReference = intersected.SpatialReference;
			}

			private TransformedTableFields IntersectedFields { get; }
			private TransformedTableFields IntersectingFields { get; }

			private int PartIntersectedFieldIndex { get; }

			public override IEnvelope Extent => _intersected.Extent;

			public override VirtualRow GetUncachedRow(long id)
			{
				throw new NotImplementedException();
			}

			public override long GetRowCount(ITableFilter queryFilter)
			{
				// TODO
				return _intersected.RowCount(queryFilter);
			}

			public override IEnumerable<VirtualRow> Search(ITableFilter filter, bool recycling)
			{
				filter = filter ?? new AoTableFilter();

				// Important: Include the TileExtent in the filter to avoid searching in empty areas of the main tile cache!
				IFeatureClassFilter intersectingFilter = (IFeatureClassFilter) filter.Clone();
				intersectingFilter.SpatialRelationship =
					esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;

				bool sameFeatureClass = _intersected.Equals(_intersecting);
				foreach (var toIntersect in DataContainer.Search(
					         _intersected, filter, QueryHelpers[0]))
				{
					intersectingFilter.FilterGeometry = ((IReadOnlyFeature) toIntersect).Extent;
					foreach (var intersecting in DataContainer.Search(
						         _intersecting, intersectingFilter, QueryHelpers[1]))
					{
						if (sameFeatureClass && intersecting.OID >= toIntersect.OID)
						{
							continue;
						}

						IGeometry intersectingGeom = ((IReadOnlyFeature) intersecting).Shape;
						IGeometry toIntersectGeom = ((IReadOnlyFeature) toIntersect).Shape;
						var op = (ITopologicalOperator) toIntersectGeom;
						if (((IRelationalOperator) op).Disjoint(intersectingGeom))
						{
							continue;
						}

						IGeometry intersected = op.Intersect(
							intersectingGeom,
							toIntersectGeom.Dimension);

						if (intersected.IsEmpty)
						{
							continue;
						}

						yield return CreateFeature((IReadOnlyFeature) toIntersect,
						                           (IReadOnlyFeature) intersecting, intersected);
					}
				}
			}

			private GdbFeature CreateFeature([NotNull] IReadOnlyFeature intersectedFeature,
			                                 [NotNull] IReadOnlyFeature intersectingFeature,
			                                 [NotNull] IGeometry intersectionGeometry)
			{
				// Build an aggregate value list consisting of the intersected, the intersecting and
				// the extra calculated values.
				var rowValues = new MultiListValues();

				List<IReadOnlyRow> baseRows = new List<IReadOnlyRow>
				                              { intersectedFeature, intersectingFeature };

				List<CalculatedValue> extraValues = new List<CalculatedValue>();

				// 1. The intersected row, wrapped in a value list:
				var intersectedValues = new ReadOnlyRowBasedValues(intersectedFeature);
				rowValues.AddList(intersectedValues, IntersectedFields.FieldIndexMapping);

				// 2. The intersecting row, wrapped in a value list:
				var intersectingValues = new ReadOnlyRowBasedValues(intersectingFeature);
				rowValues.AddList(intersectingValues, IntersectingFields.FieldIndexMapping);

				extraValues.Add(
					new CalculatedValue(Resulting.ShapeFieldIndex, intersectionGeometry));
				extraValues.Add(
					new CalculatedValue(BaseRowsFieldIndex, baseRows));

				double partIntersected =
					GetPartIntersected(intersectedFeature.Shape, intersectionGeometry);

				extraValues.Add(
					new CalculatedValue(PartIntersectedFieldIndex, partIntersected));

				extraValues.Add(new CalculatedValue(Resulting.OidFieldIndex, null));

				// Add all the collected extra values with their own copy-matrix:
				IValueList simpleList =
					TransformedAttributeUtils.ToSimpleValueList(
						extraValues, out IDictionary<int, int> extraCopyMatrix);

				rowValues.AddList(simpleList, extraCopyMatrix);

				return (GdbFeature) Resulting.CreateObject(rowValues);
			}

			private static double GetPartIntersected(IGeometry toIntersectGeom,
			                                         IGeometry intersected)
			{
				double partIntersected = 1;
				if (toIntersectGeom is IPolyline l)
				{
					double fullLength = l.Length;
					double partLength = ((IPolyline) intersected).Length;
					partIntersected = partLength / fullLength;
				}
				else if (toIntersectGeom is IArea pg)
				{
					double fullArea = pg.Area;
					double partArea = ((IArea) intersected).Area;
					partIntersected = partArea / fullArea;
				}
				else if (toIntersectGeom is IPointCollection mp)
				{
					double fullCount = mp.PointCount;
					double partCount = ((IPointCollection) intersected).PointCount;
					partIntersected = partCount / fullCount;
				}

				return partIntersected;
			}
		}
	}
}
