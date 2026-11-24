using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using IOPath = System.IO.Path;

namespace ProSuite.QA.Tests
{
	public interface IFeatureCheckClient
	{
		/// <summary>
		/// Submits all requests, then waits until every job has finished or the timeout
		/// has passed. The result list has one entry per request, in the same order.
		/// </summary>
		[NotNull]
		IList<FeatureCheckJobResult> CheckFeatures(
			[NotNull] IList<FeatureCheckRequest> requests,
			[NotNull] string apiUrl,
			TimeSpan timeout);
	}

	/// <summary>
	/// Client for the "feature-check" target of the prediction service: a language model
	/// judges each sent feature against a rule written in text, using the image.
	/// </summary>
	public class FeatureCheckClient : IFeatureCheckClient
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// The GeoJSON property that carries the object identifier, so that results are
		/// matched back by identifier and not by geometry.
		/// </summary>
		public const string ObjectIdProperty = "prosuite_oid";

		public IList<FeatureCheckJobResult> CheckFeatures(
			IList<FeatureCheckRequest> requests,
			string apiUrl,
			TimeSpan timeout)
		{
			Assert.ArgumentNotNull(requests, nameof(requests));
			Assert.ArgumentNotNullOrEmpty(apiUrl, nameof(apiUrl));

			DateTime timeoutAt = DateTime.UtcNow.Add(timeout);
			var result = new FeatureCheckJobResult[requests.Count];
			var pendingIndexes = new List<int>();
			var jobIds = new string[requests.Count];

			string unreachableMessage = null;
			for (var i = 0; i < requests.Count; i++)
			{
				if (unreachableMessage != null)
				{
					result[i] = FeatureCheckJobResult.Failed(requests[i], null, unreachableMessage);
					continue;
				}

				try
				{
					jobIds[i] = SubmitJob(requests[i], apiUrl, timeoutAt);
					pendingIndexes.Add(i);
					_msg.InfoFormat("Submitted feature check {0}/{1}: file {2} -> job {3}",
					                i + 1, requests.Count,
					                IOPath.GetFileName(requests[i].ImagePath), jobIds[i]);
				}
				catch (Exception e)
				{
					string message =
						$"Failed to submit feature check for {IOPath.GetFileName(requests[i].ImagePath)}: {e.Message}";
					_msg.Warn(message);
					result[i] = FeatureCheckJobResult.Failed(requests[i], null, message);

					// Each further attempt would wait for the same connection timeout.
					if (IsServiceUnreachable(e))
					{
						unreachableMessage = $"The service {apiUrl} cannot be reached: {e.Message}";
					}
				}
			}

			while (pendingIndexes.Count > 0)
			{
				for (int p = pendingIndexes.Count - 1; p >= 0; p--)
				{
					int i = pendingIndexes[p];
					FeatureCheckJobResult completed;
					try
					{
						completed = TryGetCompletedResult(
							requests[i], apiUrl, jobIds[i], timeoutAt);
					}
					catch (Exception e)
					{
						_msg.WarnFormat("Feature check job {0} failed: {1}", jobIds[i], e.Message);
						completed = FeatureCheckJobResult.Failed(requests[i], jobIds[i], e.Message);
					}

					if (completed != null)
					{
						result[i] = completed;
						pendingIndexes.RemoveAt(p);
					}
				}

				if (pendingIndexes.Count > 0)
				{
					PredictionServiceHttp.WaitBeforeNextPoll(timeoutAt);
				}
			}

			return result;
		}

		private static bool IsServiceUnreachable([NotNull] Exception exception)
		{
			for (Exception e = exception; e != null; e = e.InnerException)
			{
				if (e is WebException webException &&
				    (webException.Status == WebExceptionStatus.ConnectFailure ||
				     webException.Status == WebExceptionStatus.NameResolutionFailure ||
				     webException.Status == WebExceptionStatus.ProxyNameResolutionFailure))
				{
					return true;
				}

				// .NET (not .NET Framework) reports the socket error only as inner exception.
				if (e is SocketException socketException)
				{
					switch (socketException.SocketErrorCode)
					{
						case SocketError.ConnectionRefused:
						case SocketError.HostNotFound:
						case SocketError.HostUnreachable:
						case SocketError.NetworkUnreachable:
						case SocketError.TimedOut:
							return true;
					}
				}
			}

			return false;
		}

		[NotNull]
		private static string SubmitJob([NotNull] FeatureCheckRequest request,
		                                [NotNull] string apiUrl,
		                                DateTime timeoutAt)
		{
			var parts = new List<PredictionServiceHttp.MultipartPart>
			            {
				            PredictionServiceHttp.MultipartPart.FromFile(
					            "file", request.ImagePath, "image/tiff"),
				            PredictionServiceHttp.MultipartPart.FromTextAsFile(
					            "features", "features.geojson", request.FeaturesGeoJson,
					            "application/geo+json"),
				            PredictionServiceHttp.MultipartPart.FromText(
					            "prompt", request.Rule)
			            };

			if (request.ContextGeoJson != null)
			{
				parts.Add(PredictionServiceHttp.MultipartPart.FromTextAsFile(
					          "context", "context.geojson", request.ContextGeoJson,
					          "application/geo+json"));
			}

			var response = PredictionServiceHttp.PostMultipart<JobResponse>(
				PredictionServiceHttp.CombineApiPath(apiUrl, "jobs?target=feature-check"),
				parts, timeoutAt);

			if (string.IsNullOrEmpty(response.JobId))
			{
				throw new InvalidOperationException(
					"Prediction service response contains no job_id");
			}

			return response.JobId;
		}

		/// <returns>null while the job is still running</returns>
		[CanBeNull]
		private static FeatureCheckJobResult TryGetCompletedResult(
			[NotNull] FeatureCheckRequest request,
			[NotNull] string apiUrl,
			[NotNull] string jobId,
			DateTime timeoutAt)
		{
			var status = PredictionServiceHttp.GetJson<JobStatus>(
				PredictionServiceHttp.BuildJobStatusUrl(apiUrl, jobId), timeoutAt);

			if (string.Equals(status.Status, "done", StringComparison.OrdinalIgnoreCase))
			{
				var collection = PredictionServiceHttp.GetJson<ResultCollection>(
					PredictionServiceHttp.BuildJobResultUrl(apiUrl, jobId), timeoutAt);

				return ToJobResult(request, jobId, collection);
			}

			if (string.Equals(status.Status, "failed", StringComparison.OrdinalIgnoreCase) ||
			    string.Equals(status.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
			{
				return FeatureCheckJobResult.Failed(
					request, jobId,
					$"Feature check job {jobId}: status {status.Status}" +
					(string.IsNullOrEmpty(status.Error) ? string.Empty : $", error: {status.Error}"));
			}

			return null;
		}

		[NotNull]
		private static FeatureCheckJobResult ToJobResult(
			[NotNull] FeatureCheckRequest request,
			[NotNull] string jobId,
			[NotNull] ResultCollection collection)
		{
			if (collection.Features == null)
			{
				throw new InvalidOperationException(
					"Feature check result contains no features array");
			}

			var results = new List<FeatureCheckResult>(collection.Features.Length);
			foreach (ResultFeature feature in collection.Features)
			{
				ResultProperties properties = feature?.Properties;
				if (properties?.ObjectId == null)
				{
					throw new InvalidOperationException(
						$"Feature check result contains a feature without {ObjectIdProperty}");
				}

				results.Add(new FeatureCheckResult(
					            (long) Math.Round(properties.ObjectId.Value),
					            ParseStatus(properties.Status),
					            properties.ErrorProbability.HasValue
						            ? (int?) Math.Round(properties.ErrorProbability.Value)
						            : null,
					            properties.Category,
					            properties.Reason));
			}

			return new FeatureCheckJobResult(request, jobId, results, null,
			                                 collection.Check?.CostUsd,
			                                 collection.Check?.Model);
		}

		private static FeatureCheckStatus ParseStatus([CanBeNull] string status)
		{
			switch (status)
			{
				case "checked":
					return FeatureCheckStatus.Checked;
				case "outside_image":
					return FeatureCheckStatus.OutsideImage;
				case "failed":
					return FeatureCheckStatus.Failed;
				default:
					throw new InvalidOperationException(
						string.Format(CultureInfo.InvariantCulture,
						              "Feature check result has an unknown check_status '{0}'",
						              status ?? "<null>"));
			}
		}

		#region Service contracts

		[DataContract]
		private class JobResponse
		{
			[DataMember(Name = "job_id")]
			public string JobId { get; set; }
		}

		[DataContract]
		private class JobStatus
		{
			[DataMember(Name = "status")]
			public string Status { get; set; }

			[DataMember(Name = "error")]
			public string Error { get; set; }
		}

		[DataContract]
		private class ResultCollection
		{
			[DataMember(Name = "features")]
			public ResultFeature[] Features { get; set; }

			[DataMember(Name = "check")]
			public CheckSummary Check { get; set; }
		}

		[DataContract]
		private class CheckSummary
		{
			[DataMember(Name = "model")]
			public string Model { get; set; }

			[DataMember(Name = "cost_usd")]
			public double? CostUsd { get; set; }
		}

		[DataContract]
		private class ResultFeature
		{
			[DataMember(Name = "properties")]
			public ResultProperties Properties { get; set; }
		}

		[DataContract]
		private class ResultProperties
		{
			// Numbers are read as double: a GeoJSON writer may write 12 as 12.0.
			[DataMember(Name = ObjectIdProperty)]
			public double? ObjectId { get; set; }

			[DataMember(Name = "check_status")]
			public string Status { get; set; }

			[DataMember(Name = "check_category")]
			public string Category { get; set; }

			[DataMember(Name = "check_error_probability")]
			public double? ErrorProbability { get; set; }

			[DataMember(Name = "check_reason")]
			public string Reason { get; set; }
		}

		#endregion
	}

	public class FeatureCheckRequest
	{
		public FeatureCheckRequest([NotNull] string imagePath,
		                           [NotNull] string featuresGeoJson,
		                           [CanBeNull] string contextGeoJson,
		                           [NotNull] string rule)
		{
			Assert.ArgumentNotNullOrEmpty(imagePath, nameof(imagePath));
			Assert.ArgumentNotNullOrEmpty(featuresGeoJson, nameof(featuresGeoJson));
			Assert.ArgumentNotNullOrEmpty(rule, nameof(rule));

			ImagePath = imagePath;
			FeaturesGeoJson = featuresGeoJson;
			ContextGeoJson = contextGeoJson;
			Rule = rule;
		}

		[NotNull]
		public string ImagePath { get; }

		[NotNull]
		public string FeaturesGeoJson { get; }

		[CanBeNull]
		public string ContextGeoJson { get; }

		[NotNull]
		public string Rule { get; }
	}

	public class FeatureCheckJobResult
	{
		public FeatureCheckJobResult([NotNull] FeatureCheckRequest request,
		                             [CanBeNull] string jobId,
		                             [NotNull] IList<FeatureCheckResult> results,
		                             [CanBeNull] string error,
		                             double? costUsd = null,
		                             [CanBeNull] string model = null)
		{
			Request = request;
			JobId = jobId;
			Results = results;
			Error = error;
			CostUsd = costUsd;
			Model = model;
		}

		[NotNull]
		public static FeatureCheckJobResult Failed([NotNull] FeatureCheckRequest request,
		                                           [CanBeNull] string jobId,
		                                           [NotNull] string error)
		{
			return new FeatureCheckJobResult(request, jobId, new List<FeatureCheckResult>(),
			                                 error);
		}

		[NotNull]
		public FeatureCheckRequest Request { get; }

		[CanBeNull]
		public string JobId { get; }

		[NotNull]
		public IList<FeatureCheckResult> Results { get; }

		[CanBeNull]
		public string Error { get; }

		public bool Succeeded => Error == null;

		public double? CostUsd { get; }

		[CanBeNull]
		public string Model { get; }
	}

	public enum FeatureCheckStatus
	{
		Checked,
		OutsideImage,
		Failed
	}

	public class FeatureCheckResult
	{
		public FeatureCheckResult(long objectId,
		                          FeatureCheckStatus status,
		                          int? errorProbability,
		                          [CanBeNull] string category,
		                          [CanBeNull] string reason)
		{
			ObjectId = objectId;
			Status = status;
			ErrorProbability = errorProbability;
			Category = category;
			Reason = reason;
		}

		public long ObjectId { get; }

		public FeatureCheckStatus Status { get; }

		/// <summary>
		/// 0 to 100: the higher of the two runs. Null unless the status is
		/// <see cref="FeatureCheckStatus.Checked"/>.
		/// </summary>
		public int? ErrorProbability { get; }

		[CanBeNull]
		public string Category { get; }

		/// <summary>One sentence, in German, that names what the model saw.</summary>
		[CanBeNull]
		public string Reason { get; }
	}
}
