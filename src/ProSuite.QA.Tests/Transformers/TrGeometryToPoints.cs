using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrGeometryToPoints : TrGeometryTransform
	{
		private class UniqueIdKey : IUniqueIdKey
		{
			bool IUniqueIdKey.IsVirtuell => BaseOid < 0;

			public long BaseOid { get; }
			public int PartIdx { get; }
			public int VertexIdx { get; }

			public UniqueIdKey(long baseOid, int partIdx, int vertexIdx)
			{
				BaseOid = baseOid;
				PartIdx = partIdx;
				VertexIdx = vertexIdx;
			}

			public GdbFeature BaseFeature { get; set; }

			public IList<InvolvedRow> GetInvolvedRows()
			{
				return InvolvedRowUtils.GetInvolvedRows(BaseFeature);
			}

			public override string ToString() =>
				$"Oid:{BaseOid}; Part:{PartIdx}; Vertex:{VertexIdx}";
		}

		private class UniqueIdKeyComparer : IEqualityComparer<UniqueIdKey>
		{
			public bool Equals(UniqueIdKey x, UniqueIdKey y)
			{
				if (x == y)
				{
					return true;
				}

				if (x == null || y == null)
				{
					return false;
				}

				return x.BaseOid == y.BaseOid &&
				       x.PartIdx == y.PartIdx &&
				       x.PartIdx == y.PartIdx;
			}

			public int GetHashCode(UniqueIdKey obj)
			{
				unchecked
				{
					int hashCode = obj.BaseOid.GetHashCode();
					hashCode = (hashCode * 397) ^ obj.PartIdx;
					hashCode = (hashCode * 397) ^ obj.VertexIdx;
					return hashCode;
				}
			}
		}

		private readonly GeometryComponent _component;

		public const string AttrPartIndex = "PartIndex";
		public const string AttrVertexIndex = "VertexIndex";

		private readonly SimpleUniqueIdProvider<UniqueIdKey> _uniqueIdProvider =
			new SimpleUniqueIdProvider<UniqueIdKey>(new UniqueIdKeyComparer());

		private int? _iAttrPart;
		private int? _iAttrVertex;

		[DocTr(nameof(DocTrStrings.TrGeometryToPoints_0))]
		public TrGeometryToPoints(
			[NotNull] [DocTr(nameof(DocTrStrings.TrGeometryToPoints_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[DocTr(nameof(DocTrStrings.TrGeometryToPoints_component))]
			GeometryComponent component)
			: base(featureClass, esriGeometryType.esriGeometryPoint)
		{
			_component = component;
		}

		[InternallyUsedTest]
		public TrGeometryToPoints(
			[NotNull] TrGeometryToPointsDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass, definition.Component) { }

		protected override IList<int> AddCustomAttributes(TransformedFeatureClass transformedFc)
		{
			return new List<int>(
				transformedFc.FieldsT.AddFields(
					FieldUtils.CreateField(AttrPartIndex,
					                       esriFieldType.esriFieldTypeInteger),
					FieldUtils.CreateField(AttrVertexIndex,
					                       esriFieldType.esriFieldTypeInteger)));
		}

		protected override IEnumerable<GdbFeature> Transform(IGeometry source, long? sourceOid)
		{
			IGeometry geom = GeometryComponentUtils.GetGeometryComponent(source, _component);
			if (geom is IPoint pnt)
			{
				GdbFeature feature = CreateFeature(sourceOid);
				feature.Shape = pnt;
				yield return feature;
			}
			else if (geom is IPointCollection pts)
			{
				IEnumVertex vertices = pts.EnumVertices;
				vertices.Reset();
				do
				{
					vertices.Next(out IPoint p, out int partIndex, out int vertexIndex);
					if (p == null)
					{
						break;
					}

					UniqueIdKey key = new UniqueIdKey(sourceOid ?? -1, partIndex, vertexIndex);
					GdbFeature feature = CreateFeature(_uniqueIdProvider.GetUniqueId(key));
					key.BaseFeature = feature;
					feature.Shape = p;

					_iAttrPart = _iAttrPart ?? feature.Fields.FindField(AttrPartIndex);
					_iAttrVertex = _iAttrVertex ?? feature.Fields.FindField(AttrVertexIndex);
					feature.set_Value(_iAttrPart.Value, partIndex);
					feature.set_Value(_iAttrVertex.Value, vertexIndex);

					yield return feature;
				} while (true);
			}
		}
	}
}
