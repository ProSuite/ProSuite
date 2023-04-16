using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using System.Collections.Generic;

namespace ProSuite.Commons.AO.Geometry.Proxy
{
	public interface IWKSPointCollection
	{
		[NotNull]
		IList<WKSPointZ> Points { get; }

		ISpatialReference SpatialReference { get; }
	}
}
