using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
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

		public static long GetProcessingTimeout(int featureCount,
		                                        int totalVertexCount,
		                                        double extraFactor = 1.0)
		{
			string envVarValue =
				Environment.GetEnvironmentVariable("PROSUITE_TOOLS_RPC_DEADLINE_MS");

			if (! string.IsNullOrEmpty(envVarValue) &&
			    long.TryParse(envVarValue, out long deadlineMilliseconds))
			{
				return deadlineMilliseconds;
			}

			long featureBasedDeadline = GetPerFeatureTimeOut() * Math.Max(1, featureCount);

			long vertexBasedDeadline = GetPerVertexTimeOut() * Math.Max(0, totalVertexCount);

			long deadline = Math.Max(featureBasedDeadline, vertexBasedDeadline);

			return (long) (deadline * extraFactor);
		}

		/// <summary>
		/// The total number of vertices of the specified features. Must be called on the main CIM
		/// thread because it reads the shapes.
		/// </summary>
		public static int GetTotalVertexCount([NotNull] IEnumerable<Feature> features)
		{
			var result = 0;

			foreach (Feature feature in features)
			{
				if (feature.GetShape() is Multipart multipart)
				{
					result += multipart.PointCount;
				}
			}

			return result;
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

		public static long GetPerVertexTimeOut()
		{
			string envVarValue =
				Environment.GetEnvironmentVariable("PROSUITE_TOOLS_RPC_DEADLINE_PER_VERTEX_MS");

			if (! string.IsNullOrEmpty(envVarValue) &&
			    long.TryParse(envVarValue, out long deadlineMilliseconds))
			{
				return deadlineMilliseconds;
			}

			// Default;
			return 1;
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
