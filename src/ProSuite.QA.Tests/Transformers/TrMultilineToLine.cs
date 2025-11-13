using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrMultilineToLine : TrGeometryTransform
	{
		private class UniqueIdKey : IUniqueIdKey
		{
			bool IUniqueIdKey.IsVirtuell => BaseOid < 0;

			public long BaseOid { get; }
			public int PartIdx { get; }

			public UniqueIdKey(long baseOid, int partIdx)
			{
				BaseOid = baseOid;
				PartIdx = partIdx;
			}

			public GdbFeature BaseFeature { get; set; }

			public IList<InvolvedRow> GetInvolvedRows()
			{
				return InvolvedRowUtils.GetInvolvedRows(BaseFeature);
			}

			public override string ToString() =>
				$"Oid:{BaseOid}; Part:{PartIdx};";
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
				       x.PartIdx == y.PartIdx;
			}

			public int GetHashCode(UniqueIdKey obj)
			{
				unchecked
				{
					return (obj.BaseOid.GetHashCode() * 397) ^ obj.PartIdx;
				}
			}
		}

		public const string AttrPartIndex = "PartIndex";
		private int? _iAttrPart;

		private readonly SimpleUniqueIdProvider<UniqueIdKey> _uniqueIdProvider =
			new SimpleUniqueIdProvider<UniqueIdKey>(new UniqueIdKeyComparer());

		[DocTr(nameof(DocTrStrings.TrMultilineToLine_0))]
		public TrMultilineToLine(
			[NotNull] [DocTr(nameof(DocTrStrings.TrMultilineToLine_featureClass))]
			IReadOnlyFeatureClass featureClass)
			: base(featureClass, esriGeometryType.esriGeometryPolyline) { }

		[InternallyUsedTest]
		public TrMultilineToLine(
			[NotNull] TrMultilineToLineDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass) { }

		protected override IList<int> AddCustomAttributes(TransformedFeatureClass transformedFc)
		{
			return new List<int>(
				transformedFc.FieldsT.AddFields(
					FieldUtils.CreateField(AttrPartIndex,
					                       esriFieldType.esriFieldTypeInteger)));
		}

		protected override IEnumerable<GdbFeature> Transform(IGeometry source, long? sourceOid)
		{
			IGeometry transform = source;
			if (source is IPolygon poly)
			{
				transform = ((ITopologicalOperator) poly).Boundary;
			}

			IGeometryCollection geom = (IGeometryCollection) transform;
			for (int i = 0; i < geom.GeometryCount; i++)
			{
				IGeometry singleLine = GeometryFactory.Clone(geom.Geometry[i]);
				IPolyline line = GeometryFactory.CreatePolyline(singleLine);

				UniqueIdKey key = new UniqueIdKey(sourceOid ?? -1, i);
				GdbFeature feature = CreateFeature(_uniqueIdProvider.GetUniqueId(key));
				feature.Shape = line;

				_iAttrPart = _iAttrPart ?? feature.Fields.FindField(AttrPartIndex);
				feature.set_Value(_iAttrPart.Value, i);

				yield return feature;
			}
		}
	}
}
