using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;

namespace ProSuite.QA.Container.Geometry
{
	[CLSCompliant(false)]
	public interface IIndexedSegments : IDisposable
	{
		[NotNull]
		IEnvelope Envelope { get; }

		bool AllowIndexing { get; set; }

		[NotNull]
		IEnumerable<SegmentProxy> GetSegments();

		[NotNull]
		IEnumerable<SegmentProxy> GetSegments([NotNull] IBox box);

		[NotNull]
		SegmentProxy GetSegment(int partIndex, int segmentIndex);

		bool IsPartClosed(int part);

		int GetPartsCount();

		int GetPartSegmentCount(int part);

		[NotNull]
		IPolyline GetSubpart(int partIndex, int startSegmentIndex, double startFraction,
		                     int endSegmentIndex, double endFraction);

		[ContractAnnotation("=>true, neighborhoods:notnull;=>false, neighborhoods:canbenull"
		)]
		bool TryGetSegmentNeighborhoods(
			[NotNull] IIndexedSegments neighborSegments,
			[NotNull] IBox commonBox, double searchDistance,
			[CanBeNull] out IEnumerable<SegmentProxyNeighborhood> neighborhoods);
	}
}
