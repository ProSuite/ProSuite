using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker

{
	public class CrackerResult
	{
		#region Result objects produced when storing features

		public IList<CrackPoints> ResultsByFeature { get; } =
			new List<CrackPoints>();

		[CanBeNull]
		public IDictionary<Feature, Geometry> TargetFeaturesToUpdate { get; set; }

		public bool ResultHasMultiparts { get; set; }

		#endregion

		#region Result objects produced when storing features

		public IList<Feature> NewCrackPoint { get; } = new List<Feature>();

		public IList<Feature> AllResultFeatures { get; } = new List<Feature>();

		[NotNull]
		public IList<string> NonStorableMessages { get; } = new List<string>(0);

		#endregion
	}
}
