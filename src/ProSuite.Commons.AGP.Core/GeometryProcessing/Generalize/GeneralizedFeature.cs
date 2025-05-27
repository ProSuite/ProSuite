using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.Generalize
{
	public class GeneralizedFeature
	{
		public GeneralizedFeature(Feature feature,
		                          Multipoint deletablePoints = null,
		                          IList<SegmentInfo> removableSegments = null,
		                          Multipoint protectedPoints = null)
		{
			Feature = feature;
			DeletablePoints = deletablePoints;
			RemovableSegments = removableSegments ?? new List<SegmentInfo>();

			ProtectedPoints = protectedPoints;
		}

		public Feature Feature { get; }

		[CanBeNull]
		public Multipoint DeletablePoints { get; set; }

		[NotNull]
		public IList<SegmentInfo> RemovableSegments { get; }

		[CanBeNull]
		public Multipoint ProtectedPoints { get; set; }

		public GdbObjectReference GdbFeatureReference => new GdbObjectReference(Feature);
	}
}
