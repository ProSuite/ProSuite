using System.Collections.Generic;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.AdvancedReshape;

public class ReshapeResult
{
	public IList<ResultFeature> ResultFeatures { get; } =
		new List<ResultFeature>();

	public bool OpenJawReshapeHappened { get; set; }
	public int OpenJawIntersectionCount { get; set; }

	public string FailureMessage { get; set; }

	public void Add(IEnumerable<ResultFeature> resultFeatures)
	{
		foreach (ResultFeature resultFeature in resultFeatures)
		{
			ResultFeatures.Add(resultFeature);
		}
	}
}
