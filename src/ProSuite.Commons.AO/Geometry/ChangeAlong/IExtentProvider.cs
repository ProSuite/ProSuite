﻿using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	[CLSCompliant(false)]
	public interface IExtentProvider
	{
		[NotNull]
		IEnvelope GetCurrentExtent();

		[NotNull]
		IEnumerable<IEnvelope> GetVisibleLensWindowExtents();
	}
}