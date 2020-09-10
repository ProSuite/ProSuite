using System.Collections;
using System.Collections.Generic;
using ArcGIS.Core.CIM;

namespace ProSuite.AGP.Editing.Selection
{
	public class GeometryTypeComparer : IComparer<esriGeometryType>

	{
		public int Compare(esriGeometryType shapeTypeA, esriGeometryType shapeTypeB)
		{
			switch (shapeTypeA)
			{
				case esriGeometryType.esriGeometryPoint
					when shapeTypeB != esriGeometryType.esriGeometryPoint:
					return -1;
				case esriGeometryType.esriGeometryLine
					when shapeTypeB == esriGeometryType.esriGeometryPolygon:
					return -1;
				case esriGeometryType.esriGeometryPolygon
					when shapeTypeB != esriGeometryType.esriGeometryPolygon:
					return 1;
				default: return 0;
			}
		}
	}
}
