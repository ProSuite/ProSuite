using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public class TrGeometryToPoints : TrGeometryTransform
	{
		private readonly GeometryComponent _component;

		public TrGeometryToPoints([NotNull] IFeatureClass featureClass,
		                          GeometryComponent component)
			: base(featureClass, esriGeometryType.esriGeometryPoint)
		{
			_component = component;
		}

		protected override IEnumerable<IGeometry> Transform(IGeometry source)
		{
			IGeometry geom = GeometryComponentUtils.GetGeometryComponent(source, _component);
			if (geom is IPoint pnt)
			{
				yield return pnt;
			}
			else if (source is IPointCollection pts)
			{
				IEnumVertex vertices = pts.EnumVertices;
				vertices.Reset();
				do
				{
					vertices.Next(out IPoint p, out _, out _);
					if (p == null)
					{
						break;
					}

					yield return p;
				} while (true);
			}
		}
	}
}
