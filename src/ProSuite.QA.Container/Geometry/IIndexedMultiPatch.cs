﻿using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.QA.Container.Geometry
{
	[CLSCompliant(false)]
	public interface IIndexedMultiPatch : IIndexedSegments
	{
		IMultiPatch BaseGeometry { get; }

		List<int> GetPartIndexes(int patchIndex);

		int GetPatchIndex(int partIndex);
	}
}