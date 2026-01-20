using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing
{
	public static class FeatureProcessingUtils
	{
		public static int GetProcessingTimeout(int featureCount)
		{
			string envVarValue =
				Environment.GetEnvironmentVariable("PROSUITE_TOOLS_RPC_DEADLINE_MS");

			if (! string.IsNullOrEmpty(envVarValue) &&
			    int.TryParse(envVarValue, out int deadlineMilliseconds))
			{
				return deadlineMilliseconds;
			}

			int count = Math.Max(1, featureCount);

			int deadline = GetPerFeatureTimeOut() * count;

			return deadline;
		}

		public static int GetPerFeatureTimeOut()
		{
			string envVarValue =
				Environment.GetEnvironmentVariable("PROSUITE_TOOLS_RPC_DEADLINE_PER_FEATURE_MS");

			if (! string.IsNullOrEmpty(envVarValue) &&
			    int.TryParse(envVarValue, out int deadlineMilliseconds))
			{
				return deadlineMilliseconds;
			}

			// Default;
			return 5000;
		}

		public static void AddInputFeatures(
			[NotNull] IEnumerable<Feature> features,
			[NotNull] IDictionary<GdbObjectReference, Feature> toDictionary)
		{
			foreach (Feature selectedFeature in features)
			{
				GdbObjectReference objectReference =
					ProtobufConversionUtils.ToObjectReferenceWithUniqueClassId(selectedFeature);

				toDictionary.TryAdd(objectReference, selectedFeature);
			}
		}
	}
}
