using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker;

public class CrackerResult
{
	#region Result objects produced when storing features

	public IList<CrackedFeature> ResultsByFeature { get; set; } =
		new List<CrackedFeature>();

	#endregion

	#region Result objects produced when storing features

	public IList<Feature> NewCrackPoint { get; } = new List<Feature>();

	//public IList<Feature> AllResultFeatures { get; } = new List<Feature>();

	[NotNull]
	public IList<string> NonStorableMessages { get; } = new List<string>(0);

	public bool HasCrackPoints => ResultsByFeature.Sum(f => f.CrackPoints.Count) > 0;

	#endregion
}
