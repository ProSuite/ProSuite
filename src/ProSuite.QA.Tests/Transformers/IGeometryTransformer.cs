using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.QA.Tests.Transformers
{
	public interface IGeometryTransformer
	{
		IEnumerable<IGeometry> Transform(IGeometry source);
	}
}
