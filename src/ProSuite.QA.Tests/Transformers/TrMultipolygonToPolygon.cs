using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public class TrMultipolygonToPolygon : TrGeometryTransform
	{
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

		[Doc(nameof(DocStrings.TrMultipolygonToPolygon_0))]
		public TrMultipolygonToPolygon(
			[NotNull] [Doc(nameof(DocStrings.TrMultipolygonToPolygon_featureClass))]
			IFeatureClass featureClass)
			: base(featureClass, esriGeometryType.esriGeometryPolygon) { }

		protected override void AddCustomAttributes(GdbFeatureClass transformedFc)
		{
			transformedFc.Fields.AddFields(
				FieldUtils.CreateField(AttrOuterRingIndex, esriFieldType.esriFieldTypeInteger));
			transformedFc.Fields.AddFields(
				FieldUtils.CreateField(AttrInnerRingIndex, esriFieldType.esriFieldTypeInteger));
		}

		[TestParameter(_defaultPolygonPart)]
		[Doc(nameof(DocStrings.TrMultipolygonToPolygon_TransformedParts))]
		public PolygonPart TransformedParts { get; set; }

		protected override IEnumerable<IFeature> Transform(IGeometry source)
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
					IFeature feature = CreateFeature();
					feature.Shape = ring;
					SetAttr(feature, iExtRing, OuterRing);

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
						IFeature feature = CreateFeature();
						feature.Shape = ring;
						SetAttr(feature, iExtRing, iInnRing);

						yield return feature;
					}
					else if (TransformedParts == PolygonPart.SinglePolygons)
					{
						rings.Add(innRing);
					}
				}

				if (TransformedParts == PolygonPart.SinglePolygons)
				{
					IFeature feature = CreateFeature();
					feature.Shape = GeometryFactory.CreatePolygon(rings);
					SetAttr(feature, iExtRing, SinglePolygon);
					yield return feature;
				}
			}
		}

		private void SetAttr(IFeature feature, int outRing, int inRing)
		{
			_iAttrOuterRing = _iAttrOuterRing ?? feature.Fields.FindField(AttrOuterRingIndex);
			_iAttrInnerRing = _iAttrInnerRing ?? feature.Fields.FindField(AttrInnerRingIndex);
			feature.Value[_iAttrOuterRing.Value] = outRing;
			feature.Value[_iAttrInnerRing.Value] = inRing;
		}
	}
}
