using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing
{
	public static class FeatureProcessingUtils
	{
		public static long GetProcessingTimeout(int featureCount,
		                                        double extraFactor = 1.0)
		{
			string envVarValue =
				Environment.GetEnvironmentVariable("PROSUITE_TOOLS_RPC_DEADLINE_MS");

			if (! string.IsNullOrEmpty(envVarValue) &&
			    long.TryParse(envVarValue, out long deadlineMilliseconds))
			{
				return deadlineMilliseconds;
			}

			long count = Math.Max(1, featureCount);

			long deadline = GetPerFeatureTimeOut() * count;

			return (long) (deadline * extraFactor);
		}

		public static long GetPerFeatureTimeOut()
		{
			string envVarValue =
				Environment.GetEnvironmentVariable("PROSUITE_TOOLS_RPC_DEADLINE_PER_FEATURE_MS");

			if (! string.IsNullOrEmpty(envVarValue) &&
			    long.TryParse(envVarValue, out long deadlineMilliseconds))
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
