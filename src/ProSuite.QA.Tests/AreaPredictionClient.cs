using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using IOPath = System.IO.Path;

namespace ProSuite.QA.Tests
{
	public class AreaPredictionClient : IAreaPredictionClient,
		IAreaPredictionStreamingBatchClient
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public AreaPredictionClient() { }

		public IList<AreaPrediction> GetPredictions(string imagePath,
		                                             string apiUrl,
		                                             string modelName,
		                                             TimeSpan timeout,
		                                             double? tileOverlapRatio = null)
		{
			IList<AreaPredictionJobResult> jobs = GetPredictionJobs(
				new List<string> { imagePath }, apiUrl, modelName, timeout,
				tileOverlapRatio);
			_msg.Info(FormatSingleJobSummary(jobs[0]));
			if (! jobs[0].Succeeded)
			{
				throw new InvalidOperationException(jobs[0].Error);
			}

			return jobs[0].Predictions;
		}

		public IList<AreaPredictionJobResult> GetPredictionJobs(
			IList<string> imagePaths,
			string apiUrl,
			string modelName,
			TimeSpan timeout,
			double? tileOverlapRatio = null)
		{
			return GetPredictionJobs(
				imagePaths, apiUrl, modelName, timeout,
				null, null, tileOverlapRatio);
		}

		public IList<AreaPredictionJobResult> GetPredictionJobs(
			IList<string> imagePaths,
			string apiUrl,
			string modelName,
			TimeSpan timeout,
			Action<string, string> jobSubmitted,
			Action<AreaPredictionJobResult> jobCompleted,
			double? tileOverlapRatio = null)
		{
			Assert.ArgumentNotNull(imagePaths, nameof(imagePaths));
			Assert.ArgumentCondition(imagePaths.Count > 0,
			                         "At least one TIFF path is required");
			Assert.ArgumentNotNullOrEmpty(apiUrl, nameof(apiUrl));
			Assert.ArgumentNotNullOrEmpty(modelName, nameof(modelName));

			DateTime timeoutAt = DateTime.UtcNow.Add(timeout);
			var pendingJobs = new List<PendingPredictionServiceJob>(imagePaths.Count);
			var result = new AreaPredictionJobResult[imagePaths.Count];
			for (var i = 0; i < imagePaths.Count; i++)
			{
				string imagePath = imagePaths[i];
				try
				{
					PendingPredictionServiceJob pendingJob = SubmitJob(
						i, imagePath, apiUrl, tileOverlapRatio, timeoutAt);
					pendingJobs.Add(pendingJob);
					_msg.InfoFormat(
						"Submitted Prediction service TIFF {0}/{1}: file {2} -> job {3}",
						i + 1, imagePaths.Count, IOPath.GetFileName(imagePath),
						pendingJob.JobId);
					NotifyJobSubmitted(
						jobSubmitted, imagePath, pendingJob.JobId);
				}
				catch (Exception ex)
				{
					string message =
						$"Failed to submit Prediction service TIFF {IOPath.GetFileName(imagePath)}: {ex.Message}";
					_msg.Warn(message);
					result[i] = FailedJob(imagePath, null, message);
					NotifyJobCompleted(jobCompleted, result[i]);
				}
			}

			var outstandingJobs = new List<PendingPredictionServiceJob>(pendingJobs);
			while (outstandingJobs.Count > 0)
			{
				for (var index = outstandingJobs.Count - 1; index >= 0; index--)
				{
					PendingPredictionServiceJob pendingJob = outstandingJobs[index];
					AreaPredictionJobResult completedResult = null;
					try
					{
						if (! TryGetCompletedResult(
							    apiUrl, pendingJob, timeoutAt, out completedResult))
						{
							continue;
						}
					}
					catch (Exception ex)
					{
						_msg.WarnFormat(
							"Prediction service job {0} for file {1} failed: {2}",
							pendingJob.JobId, IOPath.GetFileName(pendingJob.ImagePath),
							ex.Message);
						completedResult = FailedJob(
							pendingJob.ImagePath, pendingJob.JobId, ex.Message);
					}

					result[pendingJob.Index] = completedResult;
					outstandingJobs.RemoveAt(index);
					NotifyJobCompleted(jobCompleted, completedResult);
				}

				if (outstandingJobs.Count > 0)
				{
					TimeSpan remaining = timeoutAt - DateTime.UtcNow;
					if (remaining <= TimeSpan.Zero)
					{
						continue;
					}

					Thread.Sleep(remaining < TimeSpan.FromSeconds(1)
						             ? remaining
						             : TimeSpan.FromSeconds(1));
				}
			}

			return result;
		}

		private static void NotifyJobSubmitted(
			[CanBeNull] Action<string, string> callback,
			[NotNull] string imagePath,
			[NotNull] string jobId)
		{
			if (callback == null)
			{
				return;
			}

			try
			{
				callback(imagePath, jobId);
			}
			catch (Exception e)
			{
				_msg.WarnFormat(
					"Prediction service job-submitted callback failed for {0}: {1}",
					jobId, e.Message);
			}
		}

		private static void NotifyJobCompleted(
			[CanBeNull] Action<AreaPredictionJobResult> callback,
			[NotNull] AreaPredictionJobResult job)
		{
			if (callback == null)
			{
				return;
			}

			try
			{
				callback(job);
			}
			catch (Exception e)
			{
				_msg.WarnFormat(
					"Prediction service job-completed callback failed for {0}: {1}",
					job.JobId ?? "<unknown>", e.Message);
			}
		}

		[NotNull]
		private static AreaPredictionJobResult FailedJob(
			[NotNull] string imagePath,
			[CanBeNull] string jobId,
			[NotNull] string error)
		{
			return new AreaPredictionJobResult(
				imagePath, jobId, new List<AreaPrediction>(), null, error);
		}

		[NotNull]
		private PendingPredictionServiceJob SubmitJob(int index,
		                                   string imagePath,
		                                   string apiUrl,
		                                   double? tileOverlapRatio,
		                                   DateTime timeoutAt)
		{
			string requestUrl = BuildJobUrl(apiUrl, tileOverlapRatio);
			string boundary = "----ProSuitePredictionService" + Guid.NewGuid().ToString("N");

			var request = (HttpWebRequest) WebRequest.Create(requestUrl);
			AddPredictionServiceApiKeyHeader(request);
			request.Method = "POST";
			request.ContentType = "multipart/form-data; boundary=" + boundary;
			request.Accept = "application/json";
			SetRequestTimeout(request, RemainingTimeout(timeoutAt));

			using (Stream requestStream = request.GetRequestStream())
			{
				WriteMultipartTiff(requestStream, boundary, imagePath);
			}

			using (var response = (HttpWebResponse) request.GetResponse())
			using (Stream responseStream = response.GetResponseStream())
			{
				var serializer =
					new DataContractJsonSerializer(typeof(PredictionServiceJobResponse));
				var jobResponse =
					(PredictionServiceJobResponse) serializer.ReadObject(responseStream);

				string jobId = jobResponse?.JobId;
				if (string.IsNullOrEmpty(jobId))
				{
					throw new InvalidOperationException(
						"Prediction service response contains no job_id");
				}

				return new PendingPredictionServiceJob(index, imagePath, jobId);
			}
		}

		private bool TryGetCompletedResult(
			[NotNull] string apiUrl,
			[NotNull] PendingPredictionServiceJob pendingJob,
			DateTime timeoutAt,
			[CanBeNull] out AreaPredictionJobResult completedResult)
		{
			completedResult = null;
			string statusUrl = BuildJobStatusUrl(apiUrl, pendingJob.JobId);
			var request = (HttpWebRequest) WebRequest.Create(statusUrl);
			AddPredictionServiceApiKeyHeader(request);
			request.Method = "GET";
			request.Accept = "application/json";
			SetRequestTimeout(request, RemainingTimeout(timeoutAt));

			PredictionServiceJobStatus status;
			using (var response = (HttpWebResponse) request.GetResponse())
			using (Stream stream = response.GetResponseStream())
			{
				var serializer =
					new DataContractJsonSerializer(typeof(PredictionServiceJobStatus));
				status = (PredictionServiceJobStatus) serializer.ReadObject(stream);
			}

			if (status == null)
			{
				throw new InvalidOperationException(
					$"Prediction service job {pendingJob.JobId} returned no status");
			}

			if (string.Equals(status.Status, "done",
			                  StringComparison.OrdinalIgnoreCase))
			{
				PredictionServiceGeoJsonResult predictionResult = GetResult(
					BuildJobResultUrl(apiUrl, pendingJob.JobId), timeoutAt);
				completedResult = new AreaPredictionJobResult(
					pendingJob.ImagePath, pendingJob.JobId,
					ToPredictions(predictionResult),
					FormatDiagnostics(status.Diagnostics));
				return true;
			}

			if (string.Equals(status.Status, "failed",
			                  StringComparison.OrdinalIgnoreCase) ||
			    string.Equals(status.Status, "cancelled",
			                  StringComparison.OrdinalIgnoreCase))
			{
				string message = FormatTerminalStatus(status, pendingJob.JobId);
				_msg.Warn(message);
				throw new InvalidOperationException(message);
			}

			return false;
		}

		[NotNull]
		private static string FormatStatus([CanBeNull] PredictionServiceJobStatus status,
		                                   [NotNull] string fallbackJobId)
		{
			if (status == null)
			{
				return $"Prediction service job {fallbackJobId}: status response was empty";
			}

			string jobId = string.IsNullOrEmpty(status.JobId)
				               ? fallbackJobId
				               : status.JobId;
			string result = $"Prediction service job {jobId}: status {status.Status ?? "<null>"}";

			if (status.TilesDone.HasValue || status.TilesTotal.HasValue)
			{
				result += $" ({FormatNullable(status.TilesDone)}/" +
				          $"{FormatNullable(status.TilesTotal)} tiles)";
			}

			if (! string.IsNullOrEmpty(status.Filename))
			{
				result += $", file {status.Filename}";
			}

			return result;
		}

		[NotNull]
		private static string FormatTerminalStatus([CanBeNull] PredictionServiceJobStatus status,
		                                           [NotNull] string fallbackJobId)
		{
			string message = FormatStatus(status, fallbackJobId);

			if (status != null && ! string.IsNullOrEmpty(status.Error))
			{
				message += $", error: {status.Error}";
			}

			string diagnostics = FormatDiagnostics(status?.Diagnostics);
			if (! string.IsNullOrEmpty(diagnostics))
			{
				message += Environment.NewLine + diagnostics;
			}

			return message;
		}

		[NotNull]
		private static string FormatSingleJobSummary(
			[NotNull] AreaPredictionJobResult job)
		{
			string result = $"Prediction service job {job.JobId ?? "<unknown>"} " +
			                $"{(job.Succeeded ? "completed" : "failed")}, " +
			                $"file {IOPath.GetFileName(job.ImagePath)}, " +
			                $"predictions {job.Predictions.Count}";
			if (! job.Succeeded && ! string.IsNullOrEmpty(job.Error))
			{
				result += Environment.NewLine + "error: " + job.Error;
			}

			if (! string.IsNullOrEmpty(job.Diagnostics))
			{
				result += Environment.NewLine + job.Diagnostics;
			}

			return result;
		}

		[CanBeNull]
		private static string FormatDiagnostics([CanBeNull] PredictionServiceDiagnostics diagnostics)
		{
			if (diagnostics == null)
			{
				return null;
			}

			var lines = new List<string> { "Prediction service diagnostics:" };

			AddDiagnosticLine(lines, "model_name", diagnostics.ModelName);
			AddDiagnosticLine(lines, "input_tiles", diagnostics.InputTiles);
			AddDiagnosticLine(lines, "triage_enabled", diagnostics.TriageEnabled);
			AddDiagnosticLine(lines, "triage_skipped_tiles",
			                  diagnostics.TriageSkippedTiles);
			AddDiagnosticLine(lines, "tiles_sent_to_model",
			                  diagnostics.TilesSentToModel);
			AddDiagnosticLine(lines, "failed_tiles", diagnostics.FailedTiles);
			AddDiagnosticLine(lines, "raw_polygons", diagnostics.RawPolygons);
			AddDiagnosticLine(lines, "polygons_removed_overlap_validation",
			                  diagnostics.PolygonsRemovedOverlapValidation);
			AddDiagnosticLine(lines, "polygons_removed_edge_validation",
			                  diagnostics.PolygonsRemovedEdgeValidation);
			AddDiagnosticLine(lines, "merged_contours", diagnostics.MergedContours);
			AddDiagnosticLine(lines, "polygons_removed_min_area",
			                  diagnostics.PolygonsRemovedMinArea);
			AddDiagnosticLine(lines, "polygons_removed_invalid_contour",
			                  diagnostics.PolygonsRemovedInvalidContour);
			AddDiagnosticLine(lines, "polygons_before_regularization",
			                  diagnostics.PolygonsBeforeRegularization);
			AddDiagnosticLine(lines, "polygons_removed_regularization",
			                  diagnostics.PolygonsRemovedRegularization);
			AddDiagnosticLine(lines, "polygons_after_postprocessing",
			                  diagnostics.PolygonsAfterPostprocessing);
			AddDiagnosticLine(lines, "polygons_removed_invalid_output",
			                  diagnostics.PolygonsRemovedInvalidOutput);
			AddDiagnosticLine(lines, "final_polygons", diagnostics.FinalPolygons);

			if (diagnostics.DebugArtifacts != null)
			{
				var artifactLines = new List<string>();
				diagnostics.DebugArtifacts.AddLines(artifactLines);
				if (artifactLines.Count > 0)
				{
					lines.Add("  debug_artifacts:");
					foreach (string artifactLine in artifactLines)
					{
						lines.Add("    " + artifactLine);
					}
				}
			}

			if (diagnostics.Warnings != null && diagnostics.Warnings.Length > 0)
			{
				lines.Add("  warnings:");
				foreach (string warning in diagnostics.Warnings)
				{
					lines.Add("    - " + warning);
				}
			}

			return string.Join(Environment.NewLine, lines);
		}

		private static void AddDiagnosticLine([NotNull] ICollection<string> lines,
		                                      [NotNull] string name,
		                                      [CanBeNull] string value)
		{
			if (! string.IsNullOrEmpty(value))
			{
				lines.Add($"  {name}: {value}");
			}
		}

		private static void AddDiagnosticLine([NotNull] ICollection<string> lines,
		                                      [NotNull] string name,
		                                      int? value)
		{
			if (value.HasValue)
			{
				lines.Add($"  {name}: {value.Value}");
			}
		}

		private static void AddDiagnosticLine([NotNull] ICollection<string> lines,
		                                      [NotNull] string name,
		                                      bool? value)
		{
			if (value.HasValue)
			{
				lines.Add($"  {name}: {value.Value}");
			}
		}

		[NotNull]
		private static string FormatNullable(int? value)
		{
			return value.HasValue ? value.Value.ToString() : "?";
		}

		private PredictionServiceGeoJsonResult GetResult(string resultUrl,
		                                      DateTime timeoutAt)
		{
			var request = (HttpWebRequest) WebRequest.Create(resultUrl);
			AddPredictionServiceApiKeyHeader(request);
			request.Method = "GET";
			request.Accept = "application/json";
			SetRequestTimeout(request, RemainingTimeout(timeoutAt));

			using (var response = (HttpWebResponse) request.GetResponse())
			using (Stream stream = response.GetResponseStream())
			{
				var serializer =
					new DataContractJsonSerializer(typeof(PredictionServiceGeoJsonResult));
				return (PredictionServiceGeoJsonResult) serializer.ReadObject(stream);
			}
		}

		private static void AddPredictionServiceApiKeyHeader(HttpWebRequest request)
		{
			string apiKey = Environment.GetEnvironmentVariable("PREDICTION_API_KEY");

			if (! string.IsNullOrWhiteSpace(apiKey))
			{
				request.Headers["X-API-Key"] = apiKey;
			}
		}

		private static void WriteMultipartTiff(Stream requestStream,
		                                       string boundary,
		                                       string imagePath)
		{
			string header =
				"--" + boundary + "\r\n" +
				"Content-Disposition: form-data; name=\"file\"; filename=\"" +
				IOPath.GetFileName(imagePath) + "\"\r\n" +
				"Content-Type: image/tiff\r\n\r\n";
			byte[] headerBytes = Encoding.UTF8.GetBytes(header);
			requestStream.Write(headerBytes, 0, headerBytes.Length);

			using (System.IO.FileStream fileStream = File.OpenRead(imagePath))
			{
				fileStream.CopyTo(requestStream);
			}

			byte[] footerBytes =
				Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
			requestStream.Write(footerBytes, 0, footerBytes.Length);
		}

		private static void SetRequestTimeout(HttpWebRequest request, TimeSpan timeout)
		{
			int timeoutMilliseconds = ToTimeoutMilliseconds(timeout);
			request.Timeout = timeoutMilliseconds;
			request.ReadWriteTimeout = timeoutMilliseconds;
		}

		private static int ToTimeoutMilliseconds(TimeSpan timeout)
		{
			if (timeout <= TimeSpan.Zero)
			{
				return 1;
			}

			if (timeout.TotalMilliseconds >= int.MaxValue)
			{
				return int.MaxValue;
			}

			return Math.Max(1, (int) timeout.TotalMilliseconds);
		}

		private static TimeSpan RemainingTimeout(DateTime timeoutAt)
		{
			TimeSpan remaining = timeoutAt - DateTime.UtcNow;
			if (remaining <= TimeSpan.Zero)
			{
				throw new TimeoutException("Timed out waiting for Prediction service job result");
			}

			return remaining;
		}

		private static string BuildJobUrl(string apiUrl,
		                                  double? tileOverlapRatio)
		{
			string separator = apiUrl.EndsWith("/", StringComparison.Ordinal)
				                   ? string.Empty
				                   : "/";

			string result = apiUrl + separator + "jobs?target=buildings";

			if (tileOverlapRatio.HasValue)
			{
				result += "&tile_overlap_ratio=" +
				          tileOverlapRatio.Value.ToString(
					          CultureInfo.InvariantCulture);
			}

			return result;
		}

		private static string BuildJobStatusUrl(string apiUrl, string jobId)
		{
			return CombineApiPath(apiUrl, "jobs/" + Uri.EscapeDataString(jobId));
		}

		private static string BuildJobResultUrl(string apiUrl, string jobId)
		{
			return CombineApiPath(
				apiUrl, "jobs/" + Uri.EscapeDataString(jobId) + "/result");
		}

		private static string CombineApiPath(string apiUrl, string path)
		{
			string separator = apiUrl.EndsWith("/", StringComparison.Ordinal)
				                   ? string.Empty
				                   : "/";
			return apiUrl + separator + path;
		}

		private static IList<AreaPrediction> ToPredictions(
			[CanBeNull] PredictionServiceGeoJsonResult response)
		{
			var result = new List<AreaPrediction>();

			if (response == null)
			{
				throw new InvalidOperationException("Prediction service returned no GeoJSON result");
			}

			if (! string.Equals(response.Type, "FeatureCollection",
			                    StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException(
					$"Prediction service returned unsupported GeoJSON type: {response.Type ?? "<null>"}");
			}

			if (response.Features == null)
			{
				throw new InvalidOperationException(
					"Prediction service GeoJSON result contains no features array");
			}

			foreach (PredictionServiceGeoJsonFeature feature in response.Features)
			{
				if (feature == null)
				{
					throw new InvalidOperationException(
						"Prediction service GeoJSON result contains a null feature");
				}

				PredictionServiceGeoJsonGeometry geometry = feature.Geometry;
				if (geometry == null)
				{
					throw new InvalidOperationException(
						"Prediction service GeoJSON feature contains no geometry");
				}

				if (! string.Equals(geometry.Type, "Polygon",
				                    StringComparison.OrdinalIgnoreCase))
				{
					throw new InvalidOperationException(
						$"Prediction service returned unsupported geometry type: {geometry.Type ?? "<null>"}");
				}

				if (geometry.Coordinates == null || geometry.Coordinates.Length == 0 ||
				    geometry.Coordinates[0] == null)
				{
					throw new InvalidOperationException(
						"Prediction service Polygon geometry contains no exterior ring");
				}

				if (HasInteriorRing(geometry.Coordinates))
				{
					throw new InvalidOperationException(
						"Prediction service Polygon geometries with interior rings are not supported");
				}

				double[][] polygon = geometry.Coordinates[0];
				var points = new List<PredictionPoint>();
				foreach (double[] point in polygon)
				{
					if (point == null || point.Length < 2)
					{
						throw new InvalidOperationException(
							"Prediction service Polygon exterior ring contains an invalid coordinate");
					}

					if (! IsFinite(point[0]) || ! IsFinite(point[1]))
					{
						throw new InvalidOperationException(
							$"Prediction service returned a non-finite coordinate: {point[0]}, {point[1]}");
					}

					points.Add(new PredictionPoint(point[0], point[1]));
				}

				result.Add(new AreaPrediction(points));
			}

			return result;
		}

		private static bool HasInteriorRing(double[][][] polygonCoordinates)
		{
			for (var i = 1; i < polygonCoordinates.Length; i++)
			{
				if (polygonCoordinates[i] != null && polygonCoordinates[i].Length > 0)
				{
					return true;
				}
			}

			return false;
		}

		private static bool IsFinite(double value)
		{
			return ! double.IsNaN(value) && ! double.IsInfinity(value);
		}

		[DataContract]
		private class PredictionServiceJobResponse
		{
			[DataMember(Name = "job_id")]
			public string JobId { get; set; }
		}

		private class PendingPredictionServiceJob
		{
			public PendingPredictionServiceJob(int index,
			                        [NotNull] string imagePath,
			                        [NotNull] string jobId)
			{
				Index = index;
				ImagePath = imagePath;
				JobId = jobId;
			}

			public int Index { get; }
			[NotNull] public string ImagePath { get; }
			[NotNull] public string JobId { get; }
		}

		[DataContract]
		private class PredictionServiceJobStatus
		{
			[DataMember(Name = "job_id")]
			public string JobId { get; set; }

			[DataMember(Name = "status")]
			public string Status { get; set; }

			[DataMember(Name = "tiles_done")]
			public int? TilesDone { get; set; }

			[DataMember(Name = "tiles_total")]
			public int? TilesTotal { get; set; }

			[DataMember(Name = "filename")]
			public string Filename { get; set; }

			[DataMember(Name = "error")]
			public string Error { get; set; }

			[DataMember(Name = "diagnostics")]
			public PredictionServiceDiagnostics Diagnostics { get; set; }
		}

		[DataContract]
		private class PredictionServiceDiagnostics
		{
			[DataMember(Name = "model_name")]
			public string ModelName { get; set; }

			[DataMember(Name = "input_tiles")]
			public int? InputTiles { get; set; }

			[DataMember(Name = "triage_enabled")]
			public bool? TriageEnabled { get; set; }

			[DataMember(Name = "triage_skipped_tiles")]
			public int? TriageSkippedTiles { get; set; }

			[DataMember(Name = "tiles_sent_to_pix2poly")]
			public int? TilesSentToModel { get; set; }

			[DataMember(Name = "failed_tiles")]
			public int? FailedTiles { get; set; }

			[DataMember(Name = "raw_polygons")]
			public int? RawPolygons { get; set; }

			[DataMember(Name = "polygons_removed_overlap_validation")]
			public int? PolygonsRemovedOverlapValidation { get; set; }

			[DataMember(Name = "polygons_removed_edge_validation")]
			public int? PolygonsRemovedEdgeValidation { get; set; }

			[DataMember(Name = "merged_contours")]
			public int? MergedContours { get; set; }

			[DataMember(Name = "polygons_removed_min_area")]
			public int? PolygonsRemovedMinArea { get; set; }

			[DataMember(Name = "polygons_removed_invalid_contour")]
			public int? PolygonsRemovedInvalidContour { get; set; }

			[DataMember(Name = "polygons_before_regularization")]
			public int? PolygonsBeforeRegularization { get; set; }

			[DataMember(Name = "polygons_removed_regularization")]
			public int? PolygonsRemovedRegularization { get; set; }

			[DataMember(Name = "polygons_after_postprocessing")]
			public int? PolygonsAfterPostprocessing { get; set; }

			[DataMember(Name = "polygons_removed_invalid_output")]
			public int? PolygonsRemovedInvalidOutput { get; set; }

			[DataMember(Name = "final_polygons")]
			public int? FinalPolygons { get; set; }

			[DataMember(Name = "warnings")]
			public string[] Warnings { get; set; }

			[DataMember(Name = "debug_artifacts")]
			public PredictionServiceDebugArtifacts DebugArtifacts { get; set; }
		}

		[DataContract]
		private class PredictionServiceDebugArtifacts
		{
			[DataMember(Name = "input_image")]
			public string InputImage { get; set; }

			[DataMember(Name = "raw_tile_predictions")]
			public string RawTilePredictions { get; set; }

			[DataMember(Name = "raw_tile_predictions_overlay")]
			public string RawTilePredictionsOverlay { get; set; }

			[DataMember(Name = "after_overlap_validation")]
			public string AfterOverlapValidation { get; set; }

			[DataMember(Name = "after_edge_validation")]
			public string AfterEdgeValidation { get; set; }

			[DataMember(Name = "after_edge_validation_overlay")]
			public string AfterEdgeValidationOverlay { get; set; }

			[DataMember(Name = "tile_visualization")]
			public string TileVisualization { get; set; }

			[DataMember(Name = "bitmap_visualization")]
			public string BitmapVisualization { get; set; }

			[DataMember(Name = "before_regularization")]
			public string BeforeRegularization { get; set; }

			[DataMember(Name = "after_postprocessing")]
			public string AfterPostprocessing { get; set; }

			[DataMember(Name = "after_postprocessing_overlay")]
			public string AfterPostprocessingOverlay { get; set; }

			[DataMember(Name = "final")]
			public string Final { get; set; }

			public void AddLines([NotNull] ICollection<string> lines)
			{
				AddArtifactLine(lines, "input_image", InputImage);
				AddArtifactLine(lines, "raw_tile_predictions", RawTilePredictions);
				AddArtifactLine(lines, "raw_tile_predictions_overlay",
				                RawTilePredictionsOverlay);
				AddArtifactLine(lines, "after_overlap_validation",
				                AfterOverlapValidation);
				AddArtifactLine(lines, "after_edge_validation",
				                AfterEdgeValidation);
				AddArtifactLine(lines, "after_edge_validation_overlay",
				                AfterEdgeValidationOverlay);
				AddArtifactLine(lines, "tile_visualization", TileVisualization);
				AddArtifactLine(lines, "bitmap_visualization", BitmapVisualization);
				AddArtifactLine(lines, "before_regularization",
				                BeforeRegularization);
				AddArtifactLine(lines, "after_postprocessing", AfterPostprocessing);
				AddArtifactLine(lines, "after_postprocessing_overlay",
				                AfterPostprocessingOverlay);
				AddArtifactLine(lines, "final", Final);
			}

			private static void AddArtifactLine([NotNull] ICollection<string> lines,
			                                    [NotNull] string name,
			                                    [CanBeNull] string value)
			{
				if (! string.IsNullOrEmpty(value))
				{
					lines.Add($"{name}: {value}");
				}
			}
		}

		[DataContract]
		private class PredictionServiceGeoJsonResult
		{
			[DataMember(Name = "type")]
			public string Type { get; set; }

			[DataMember(Name = "features")]
			public PredictionServiceGeoJsonFeature[] Features { get; set; }
		}

		[DataContract]
		private class PredictionServiceGeoJsonFeature
		{
			[DataMember(Name = "geometry")]
			public PredictionServiceGeoJsonGeometry Geometry { get; set; }
		}

		[DataContract]
		private class PredictionServiceGeoJsonGeometry
		{
			[DataMember(Name = "type")]
			public string Type { get; set; }

			[DataMember(Name = "coordinates")]
			public double[][][] Coordinates { get; set; }
		}
	}
}
