using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrGeometryToPoints : TrGeometryTransform
	{
		private readonly GeometryComponent _component;

		public const string AttrPartIndex = "PartIndex";
		public const string AttrVertexIndex = "VertexIndex";
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

		protected override void AddCustomAttributes(TransformedFeatureClass transformedFc)
		{
			transformedFc.FieldsT.AddFields(
				FieldUtils.CreateField(AttrPartIndex, esriFieldType.esriFieldTypeInteger));
			transformedFc.FieldsT.AddFields(
				FieldUtils.CreateField(AttrVertexIndex, esriFieldType.esriFieldTypeInteger));
		}

		protected override IEnumerable<GdbFeature> Transform(IGeometry source)
		{
			IGeometry geom = GeometryComponentUtils.GetGeometryComponent(source, _component);
			if (geom is IPoint pnt)
			{
				GdbFeature feature = CreateFeature();
				feature.Shape = pnt;
				yield return feature;
			}
			else if (source is IPointCollection pts)
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

					GdbFeature feature = CreateFeature();
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
