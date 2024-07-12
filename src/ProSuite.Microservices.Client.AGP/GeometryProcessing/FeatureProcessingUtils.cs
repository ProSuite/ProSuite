using System;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing
{
	public static class FeatureProcessingUtils
	{
		public static int GetPerFeatureTimeOut()
		{
			string envVarValue =
				Environment.GetEnvironmentVariable("PROSUITE_TOOLS_RPC_DEADLINE_MS");

			if (! string.IsNullOrEmpty(envVarValue) &&
			    int.TryParse(envVarValue, out int deadlineMilliseconds))
			{
				return deadlineMilliseconds;
			}

			// Default;
			return 5000;
		}
	}
}
