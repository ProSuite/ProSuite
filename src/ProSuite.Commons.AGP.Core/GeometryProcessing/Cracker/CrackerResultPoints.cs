using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker
{
	public class CrackerResultPoints
	{
		public CrackerResultPoints(
			[NotNull] Feature originalFeature,
			[NotNull] Geometry updatedGeometry,
			[CanBeNull] IList<Geometry> insertCrackPoints = null)
		{
			OriginalFeature = originalFeature;
			UpdatedGeometry = updatedGeometry;
			InsertCrackPoints = insertCrackPoints ?? new List<Geometry>(0);
		}

		[NotNull]
		public Feature OriginalFeature { get; }

		[NotNull]
		public Geometry UpdatedGeometry { get; }

		[NotNull]
		public IList<Geometry> InsertCrackPoints { get; }
	}
}
