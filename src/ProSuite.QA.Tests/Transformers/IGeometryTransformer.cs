using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.QA.Tests.Transformers
{
	public interface IGeometryTransformer
	{
		IEnumerable<IFeature> Transform(IGeometry source);
	}
}
