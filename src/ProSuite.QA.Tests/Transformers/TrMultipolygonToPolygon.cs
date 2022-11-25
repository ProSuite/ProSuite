using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrMultipolygonToPolygon : TrGeometryTransform
	{
		private class UniqueIdKey : IUniqueIdKey
		{
			bool IUniqueIdKey.IsVirtuell => BaseOid < 0;

			public int BaseOid { get; }
			public int OuterRingIdx { get; }
			public int InnerRingIdx { get; }

			public UniqueIdKey(int baseOid, int outerRingId, int innerRingId)
			{
				BaseOid = baseOid;
				OuterRingIdx = outerRingId;
				InnerRingIdx = innerRingId;
			}

			public GdbFeature BaseFeature { get; set; }

			public IList<InvolvedRow> GetInvolvedRows()
			{
				return InvolvedRowUtils.GetInvolvedRows(BaseFeature);
			}

			public override string ToString() =>
				$"Oid:{BaseOid}; Ext:{OuterRingIdx}; Int:{InnerRingIdx}";
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
				       x.OuterRingIdx == y.OuterRingIdx &&
				       x.InnerRingIdx == y.InnerRingIdx;
			}

			public int GetHashCode(UniqueIdKey obj)
			{
				return obj.BaseOid + 29 * (obj.InnerRingIdx + 37 * obj.InnerRingIdx);
			}
		}

		public enum PolygonPart
		{
			SinglePolygons,
			OuterRings,
			InnerRings,
			AllRings
		}

		public const string AttrOuterRingIndex = "OuterRingIndex";
		public const string AttrInnerRingIndex = "InnerRingIndex";

		public const int OuterRing = -1;
		public const int SinglePolygon = -2;

		private const PolygonPart _defaultPolygonPart = PolygonPart.SinglePolygons;
		private int? _iAttrOuterRing;
		private int? _iAttrInnerRing;


		private readonly SimpleUniqueIdProvider<UniqueIdKey> _uniqueIdProvider =
			new SimpleUniqueIdProvider<UniqueIdKey>(new UniqueIdKeyComparer());


		[DocTr(nameof(DocTrStrings.TrMultipolygonToPolygon_0))]
		public TrMultipolygonToPolygon(
			[NotNull] [DocTr(nameof(DocTrStrings.TrMultipolygonToPolygon_featureClass))]
			IReadOnlyFeatureClass featureClass)
			: base(featureClass, esriGeometryType.esriGeometryPolygon) { }

		protected override IList<int> AddCustomAttributes(TransformedFeatureClass transformedFc)
		{
			return new List<int>(
				transformedFc.FieldsT.AddFields(
					FieldUtils.CreateField(AttrOuterRingIndex, esriFieldType.esriFieldTypeInteger),
					FieldUtils.CreateField(AttrInnerRingIndex,
					                       esriFieldType.esriFieldTypeInteger)));
		}

		[TestParameter(_defaultPolygonPart)]
		[DocTr(nameof(DocTrStrings.TrMultipolygonToPolygon_TransformedParts))]
		public PolygonPart TransformedParts { get; set; }

		protected override IEnumerable<GdbFeature> Transform(IGeometry source, int? sourceOid)
		{
			IPolygon4 poly = (IPolygon4) source;

			IGeometryCollection extRings = (IGeometryCollection) poly.ExteriorRingBag;

			for (int iExtRing = 0; iExtRing < extRings.GeometryCount; iExtRing++)
			{
				IRing extRing = (IRing) extRings.Geometry[iExtRing];
				if (TransformedParts == PolygonPart.OuterRings
				    || TransformedParts == PolygonPart.AllRings)
				{
					IPolygon ring = GeometryFactory.CreatePolygon(GeometryFactory.Clone(extRing));
					UniqueIdKey key = new UniqueIdKey(sourceOid ?? -1, iExtRing, OuterRing);
					GdbFeature feature = CreateFeature(_uniqueIdProvider.GetUniqueId(key));
					key.BaseFeature = feature;
					feature.Shape = ring;
					SetAttr(feature, key);

					yield return feature;
					if (TransformedParts == PolygonPart.OuterRings)
					{
						continue;
					}
				}

				List<IRing> rings = new List<IRing>();
				if (TransformedParts == PolygonPart.SinglePolygons)
				{
					rings.Add(GeometryFactory.Clone(extRing));
				}

				IGeometryCollection innerRings =
					(IGeometryCollection) poly.InteriorRingBag[extRing];
				for (int iInnRing = 0; iInnRing < innerRings.GeometryCount; iInnRing++)
				{
					IRing innRing = (IRing) GeometryFactory.Clone(innerRings.Geometry[iInnRing]);

					if (TransformedParts == PolygonPart.InnerRings
					    || TransformedParts == PolygonPart.AllRings)
					{
						((ICurve) innRing).ReverseOrientation();
						IPolygon ring = GeometryFactory.CreatePolygon(innRing);
						UniqueIdKey key = new UniqueIdKey(sourceOid ?? -1, iExtRing, iInnRing);
						GdbFeature feature = CreateFeature(_uniqueIdProvider.GetUniqueId(key));
						key.BaseFeature = feature;
						feature.Shape = ring;
						SetAttr(feature, key);

						yield return feature;
					}
					else if (TransformedParts == PolygonPart.SinglePolygons)
					{
						rings.Add(innRing);
					}
				}

				if (TransformedParts == PolygonPart.SinglePolygons)
				{
					UniqueIdKey key = new UniqueIdKey(sourceOid ?? -1, iExtRing, SinglePolygon);
					GdbFeature feature = CreateFeature(_uniqueIdProvider.GetUniqueId(key));
					key.BaseFeature = feature;
					feature.Shape = GeometryFactory.CreatePolygon(rings);
					SetAttr(feature, key);
					yield return feature;
				}
			}
		}

		private void SetAttr(GdbFeature feature, UniqueIdKey key)
		{
			_iAttrOuterRing = _iAttrOuterRing ?? feature.Fields.FindField(AttrOuterRingIndex);
			_iAttrInnerRing = _iAttrInnerRing ?? feature.Fields.FindField(AttrInnerRingIndex);
			feature.set_Value(_iAttrOuterRing.Value, key.OuterRingIdx);
			feature.set_Value(_iAttrInnerRing.Value, key.InnerRingIdx);
		}
	}
}
