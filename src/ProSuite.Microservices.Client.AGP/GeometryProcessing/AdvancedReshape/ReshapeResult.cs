using System.Collections.Generic;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing.AdvancedReshape
{
	public class ReshapeResult
	{
		public IList<ReshapeResultFeature> ResultFeatures { get; } =
			new List<ReshapeResultFeature>();

		public bool OpenJawReshapeHappened { get; set; }
		public int OpenJawIntersectionCount { get; set; }

		public string FailureMessage { get; set; }
	}
}
