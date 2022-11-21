using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;

namespace ProSuite.QA.Tests.Transformers
{
	public interface IGeometryTransformer
	{
		IEnumerable<GdbFeature> Transform(IGeometry source, int? sourceOid);
	}
}
