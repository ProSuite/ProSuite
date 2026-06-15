using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.RemoveOverlaps;

public class OverlapResultGeometries
{
	public OverlapResultGeometries(
		[NotNull] Feature originalFeature,
		[NotNull] Geometry updatedGeometry,
		[CanBeNull] IList<Geometry> insertGeometries = null)
	{
		OriginalFeature = originalFeature;
		UpdatedGeometry = updatedGeometry;
		InsertGeometries = insertGeometries ?? new List<Geometry>(0);
	}

	[NotNull]
	public Feature OriginalFeature { get; }

	[NotNull]
	public Geometry UpdatedGeometry { get; }

	[NotNull]
	public IList<Geometry> InsertGeometries { get; }
}
