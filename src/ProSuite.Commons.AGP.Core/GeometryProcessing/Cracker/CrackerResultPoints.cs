using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker {
	public class CrackerResultPoints(
		[NotNull] Feature originalFeature,
		[CanBeNull] CrackPoints insertCrackPoints)
	{
		[NotNull]
		public Feature OriginalFeature { get; } = originalFeature;

		[CanBeNull]
		public CrackPoints InsertCrackPoints { get; } = insertCrackPoints;
	}
}
