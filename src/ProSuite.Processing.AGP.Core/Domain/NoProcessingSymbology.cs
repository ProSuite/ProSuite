using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Processing.AGP.Core.Utils;
using ProSuite.Processing.Domain;

namespace ProSuite.Processing.AGP.Core.Domain;

public class NoProcessingSymbology : IProcessingSymbology
{
	public Polygon GetDrawingOutline(Feature feature)
	{
		throw new NotImplementedException();
	}

	public Envelope GetDrawingBounds(Feature feature)
	{
		throw new NotImplementedException();
	}

	public Polygon GetDrawingOutline(PseudoFeature feature, IMapContext mapContext,
	                                  DrawingOutline.Options options = null)
	{
		throw new NotImplementedException();
	}

	public Envelope GetDrawingBounds(PseudoFeature feature, IMapContext mapContext,
	                                 DrawingOutline.Options options = null)
	{
		throw new NotImplementedException();
	}
}
