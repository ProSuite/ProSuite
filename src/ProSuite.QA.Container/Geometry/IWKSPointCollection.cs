using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.Geometry
{
	internal interface IWKSPointCollection
	{
		[NotNull]
		IList<WKSPointZ> Points { get; }

		ISpatialReference SpatialReference { get; }
	}
}
