using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.RemoveOverlaps
{
	[CLSCompliant(false)]
	public class RemoveOverlapsResult
	{
		#region Result objects produced when storing features

		public IList<OverlapResultGeometries> ResultsByFeature { get; } =
			new List<OverlapResultGeometries>();

		[CanBeNull]
		public IDictionary<IFeature, IGeometry> TargetFeaturesToUpdate { get; set; }

		public bool ResultHasMultiparts { get; set; }

		#endregion

		#region Result objects produced when storing features

		public IList<IFeature> NewOverlapFeatures { get; } = new List<IFeature>();

		public IList<IFeature> AllResultFeatures { get; } = new List<IFeature>();

		[NotNull]
		public IList<string> NonStorableMessages { get; } = new List<string>(0);

		#endregion

		public int GetNewFeatureCount()
		{
			int result = AllResultFeatures.Count -
			             ResultsByFeature.Count;

			return result;
		}
	}

	[CLSCompliant(false)]
	public class OverlapResultGeometries
	{
		public OverlapResultGeometries(
			[NotNull] IFeature originalFeature,
			[NotNull] IList<IGeometry> resultGeometries,
			[CanBeNull] IList<IGeometry> overlappingGeometries = null)
		{
			OriginalFeature = originalFeature;
			ResultGeometries = resultGeometries;
			OverlappingGeometries = overlappingGeometries ?? new List<IGeometry>(0);
		}

		private IGeometry _largestResult;
		private IList<IGeometry> _otherResults;

		[NotNull]
		public IFeature OriginalFeature { get; }

		[NotNull]
		public IList<IGeometry> ResultGeometries { get; }

		[NotNull]
		public IList<IGeometry> OverlappingGeometries { get; }

		public IGeometry LargestResult =>
			_largestResult ??
			(_largestResult = GeometryUtils.GetLargestGeometry(ResultGeometries));

		public IList<IGeometry> NonLargestResults
		{
			get
			{
				return _otherResults ??
				       (_otherResults = ResultGeometries
				                        .Where(g => g != LargestResult).ToList());
			}
		}
	}
}
