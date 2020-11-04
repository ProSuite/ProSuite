using System;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	[CLSCompliant(false)]
	public class PolycurveInUnionReshapeInfo
	{
		public PolycurveInUnionReshapeInfo(IPolycurve geometryToReshape,
		                                   int geometryPartToReshape,
		                                   IPath sourceReplacementPath)
		{
			GeometryToReshape = geometryToReshape;
			GeometryPartToReshape = geometryPartToReshape;
			SourceReplacementPath = sourceReplacementPath;
		}

		public IPolycurve GeometryToReshape { get; set; }
		public int GeometryPartToReshape { get; set; }

		public IPath SourceReplacementPath { get; set; }

		public ReshapeInfo ReshapeInfo { get; set; }
		public ReshapeInfo FallbackReshapeInfo { get; set; }

		public bool RequiresUsingFallback { get; set; }
	}
}
