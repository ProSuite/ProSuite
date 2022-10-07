using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrMultilineToLine : TrGeometryTransform
	{
		public const string AttrPartIndex = "PartIndex";
		private int? _iAttrPart;

		[DocTr(nameof(DocTrStrings.TrMultilineToLine_0))]
		public TrMultilineToLine(
			[NotNull] [DocTr(nameof(DocTrStrings.TrMultilineToLine_featureClass))]
			IReadOnlyFeatureClass featureClass)
			: base(featureClass, esriGeometryType.esriGeometryPolyline) { }

		protected override IList<int> AddCustomAttributes(TransformedFeatureClass transformedFc)
		{
			return new List<int>(
				transformedFc.FieldsT.AddFields(
					FieldUtils.CreateField(AttrPartIndex,
					                       esriFieldType.esriFieldTypeInteger)));
		}

		protected override IEnumerable<GdbFeature> Transform(IGeometry source)
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

				GdbFeature feature = CreateFeature();
				feature.Shape = line;

				_iAttrPart = _iAttrPart ?? feature.Fields.FindField(AttrPartIndex);
				feature.set_Value(_iAttrPart.Value, i);

				yield return feature;
			}
		}
	}
}
