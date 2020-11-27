using System;
using ArcGIS.Core.Geometry;

namespace ProSuite.Processing.Domain
{
	public class NoProcessingSymbology : IProcessingSymbology
	{
		public Geometry QueryDrawingOutline(long oid, OutlineType outlineType)
		{
			throw new NotImplementedException();
		}
	}
}
