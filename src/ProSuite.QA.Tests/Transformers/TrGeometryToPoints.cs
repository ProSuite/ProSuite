using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public class TrGeometryToPoints : TrGeometryTransform
	{
		private readonly GeometryComponent _component;

		public const string AttrPartIndex = "PartIndex";
		public const string AttrVertexIndex = "VertexIndex";
		private int? _iAttrPart;
		private int? _iAttrVertex;

		[Doc(nameof(DocStrings.TrGeometryToPoints_0))]
		public TrGeometryToPoints([NotNull] [Doc(nameof(DocStrings.TrGeometryToPoints_featureClass))]
		                          IFeatureClass featureClass,
		                          [Doc(nameof(DocStrings.TrGeometryToPoints_component))]
		                          GeometryComponent component)
			: base(featureClass, esriGeometryType.esriGeometryPoint)
		{
			_component = component;
		}

		protected override void AddCustomAttributes(GdbFeatureClass transformedFc)
		{
			transformedFc.Fields.AddFields(
				FieldUtils.CreateField(AttrPartIndex, esriFieldType.esriFieldTypeInteger));
			transformedFc.Fields.AddFields(
				FieldUtils.CreateField(AttrVertexIndex, esriFieldType.esriFieldTypeInteger));
		}

		protected override IEnumerable<IFeature> Transform(IGeometry source)
		{
			IGeometry geom = GeometryComponentUtils.GetGeometryComponent(source, _component);
			if (geom is IPoint pnt)
			{
				IFeature feature = CreateFeature();
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

					IFeature feature = CreateFeature();
					feature.Shape = p;

					_iAttrPart = _iAttrPart ?? feature.Fields.FindField(AttrPartIndex);
					_iAttrVertex = _iAttrVertex ?? feature.Fields.FindField(AttrVertexIndex);
					feature.Value[_iAttrPart.Value] = partIndex;
					feature.Value[_iAttrVertex.Value] = vertexIndex;

					yield return feature;
				} while (true);
			}
		}
	}
}
