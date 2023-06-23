using ESRI.ArcGIS.Geometry;
using System.Collections.Generic;

namespace ProSuite.Commons.AO.Geometry.Proxy
{
	public interface IIndexedMultiPatch : IIndexedSegments
	{
		IMultiPatch BaseGeometry { get; }

		List<int> GetPartIndexes(int patchIndex);

		int GetPatchIndex(int partIndex);

		IPolygon GetFootprint();
	}
}
