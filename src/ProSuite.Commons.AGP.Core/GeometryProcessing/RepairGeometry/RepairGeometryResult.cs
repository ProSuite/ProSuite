using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.RepairGeometry;

public class RepairGeometryResult
{
	public IList<RepairableFeature> ResultsByFeature { get; set; } =
		new List<RepairableFeature>();

	[NotNull]
	public IList<string> NonStorableMessages { get; } = new List<string>(0);

	public bool HasRepairableFeatures => ResultsByFeature.Count > 0;
}

public class RepairableFeature
{
	public RepairableFeature(Feature feature)
	{
		Feature = feature;
		InvalidSegments = new List<InvalidSegment>();
	}

	public Feature Feature { get; }

	[CanBeNull]
	public Multipoint PointsToDelete { get; set; }

	[CanBeNull]
	public Multipoint CrackPointsToAdd { get; set; }

	[NotNull]
	public IList<InvalidSegment> InvalidSegments { get; }

	public GdbObjectReference GdbFeatureReference => new GdbObjectReference(Feature);
}

public class InvalidSegment
{
	public InvalidSegment(Segment segment, int absoluteIndex, int partIndex, int relativeIndex)
	{
		Segment = segment;
		AbsoluteIndex = absoluteIndex;
		PartIndex = partIndex;
		RelativeIndex = relativeIndex;
	}

	public Segment Segment { get; }
	public int AbsoluteIndex { get; }
	public int PartIndex { get; }
	public int RelativeIndex { get; }
}
