using System.Collections.Generic;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing.AdvancedReshape
{
	public class ReshapeResult
	{
		public IList<ResultFeature> ResultFeatures { get; } =
			new List<ResultFeature>();

		public bool OpenJawReshapeHappened { get; set; }
		public int OpenJawIntersectionCount { get; set; }

		public string FailureMessage { get; set; }
	}
}
