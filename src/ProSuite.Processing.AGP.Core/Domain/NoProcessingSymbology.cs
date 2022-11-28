using System;
using ArcGIS.Core.Geometry;
using ProSuite.Processing.AGP.Core.Utils;

namespace ProSuite.Processing.AGP.Core.Domain
{
	public class NoProcessingSymbology : IProcessingSymbology
	{
		public Geometry QueryDrawingOutline(long oid, OutlineType outlineType)
		{
			throw new NotImplementedException();
		}

		public Geometry QueryDrawingOutline(PseudoFeature feature, OutlineType outlineType)
		{
			throw new NotImplementedException();
		}
	}
}
