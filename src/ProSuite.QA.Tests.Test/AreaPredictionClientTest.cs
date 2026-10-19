using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class AreaPredictionClientTest
	{
		[Test]
		public void PredictionClientPostsTiffAndReadsPolygons()
		{
			string tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "fake tiff content");

			int port = GetFreeTcpPort();
			string baseUrl = $"http://localhost:{port}/";

			using (var listener = new HttpListener())
			{
				listener.Prefixes.Add(baseUrl);
				listener.Start();

				Task requestTask = Task.Run(() => HandlePredictionServiceJobRequests(listener));

				var client = new AreaPredictionClient();
				var predictions = client.GetPredictions(
					tempFile, baseUrl, "roof-model", TimeSpan.FromSeconds(10));

				requestTask.GetAwaiter().GetResult();

				Assert.AreEqual(1, predictions.Count);
				Assert.AreEqual(4, predictions[0].Shell.Count);
				Assert.AreEqual(10, predictions[0].Shell[0].X);
				Assert.AreEqual(20, predictions[0].Shell[0].Y);
			}

			File.Delete(tempFile);
		}

		[Test]
		public void PredictionClientNotifiesWhenIndividualJobCompletes()
		{
			string tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "fake tiff content");
			int port = GetFreeTcpPort();
			string baseUrl = $"http://localhost:{port}/";
			string submittedImage = null;
			string submittedJobId = null;
			AreaPredictionJobResult completedJob = null;

			try
			{
				using (var listener = new HttpListener())
				{
					listener.Prefixes.Add(baseUrl);
					listener.Start();
					Task requestTask = Task.Run(
						() => HandlePredictionServiceJobRequests(listener));

					var client = new AreaPredictionClient();
					IList<AreaPredictionJobResult> jobs = client.GetPredictionJobs(
						new List<string> { tempFile }, baseUrl, "roof-model",
						TimeSpan.FromSeconds(10),
						(imagePath, jobId) =>
						{
							submittedImage = imagePath;
							submittedJobId = jobId;
						},
						job => completedJob = job);

					requestTask.GetAwaiter().GetResult();

					Assert.AreEqual(1, jobs.Count);
					Assert.AreEqual(tempFile, submittedImage);
					Assert.AreEqual("test-job", submittedJobId);
					Assert.AreSame(jobs[0], completedJob);
					Assert.True(completedJob.Succeeded);
				}
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[Test]
		public void PredictionClientPollsAllSubmittedJobsFairly()
		{
			string firstFile = Path.GetTempFileName();
			string secondFile = Path.GetTempFileName();
			File.WriteAllText(firstFile, "first fake tiff");
			File.WriteAllText(secondFile, "second fake tiff");
			int port = GetFreeTcpPort();
			string baseUrl = $"http://localhost:{port}/";
			var completionOrder = new List<string>();

			try
			{
				using (var listener = new HttpListener())
				{
					listener.Prefixes.Add(baseUrl);
					listener.Start();
					Task requestTask = Task.Run(
						() => HandleTwoPredictionServiceJobsWithSecondCompletingFirst(listener));

					var client = new AreaPredictionClient();
					IList<AreaPredictionJobResult> jobs = client.GetPredictionJobs(
						new List<string> { firstFile, secondFile }, baseUrl,
						"roof-model", TimeSpan.FromSeconds(5), null,
						job => completionOrder.Add(job.JobId));

					requestTask.GetAwaiter().GetResult();
					Assert.True(jobs.All(job => job.Succeeded));
					CollectionAssert.AreEqual(
						new[] { "job-1", "job-0" }, completionOrder);
				}
			}
			finally
			{
				File.Delete(firstFile);
				File.Delete(secondFile);
			}
		}

		[Test]
		public void PredictionClientCanUseLocalPredictionDatasetAnnotation()
		{
			string datasetRoot = Environment.GetEnvironmentVariable("PREDICTION_TEST_DATASET");
			if (string.IsNullOrWhiteSpace(datasetRoot))
			{
				Assert.Ignore("PREDICTION_TEST_DATASET is not set.");
			}

			string annotationPath = Path.Combine(datasetRoot, "annotation.json");
			if (! File.Exists(annotationPath))
			{
				Assert.Ignore("Local ML data dump not found: " + annotationPath);
			}

			CocoFixture fixture =
				LoadFirstAnnotatedImage(annotationPath, "Alterswil-32.tif");

			string imagePath = Path.Combine(datasetRoot, "images", fixture.Image.FileName);
			if (! File.Exists(imagePath))
			{
				Assert.Ignore("Local ML image not found: " + imagePath);
			}

			int port = GetFreeTcpPort();
			string baseUrl = $"http://localhost:{port}/";
			string predictionServiceResponse = ToPredictionServiceGeoJsonResponseJson(fixture.Annotations);

			using (var listener = new HttpListener())
			{
				listener.Prefixes.Add(baseUrl);
				listener.Start();

				Task requestTask = Task.Run(
					() => HandlePredictionServiceJobRequests(listener, predictionServiceResponse,
					                              expectedTarget: "buildings"));

				var client = new AreaPredictionClient();
				var predictions = client.GetPredictions(
					imagePath, baseUrl, "roof-model", TimeSpan.FromSeconds(10));

				requestTask.GetAwaiter().GetResult();

				Assert.AreEqual(fixture.Annotations.Count, predictions.Count);
				Assert.AreEqual(189, predictions[0].Shell[0].X);
				Assert.AreEqual(193, predictions[0].Shell[0].Y);
			}
		}

		[Test]
		public void PredictionClientFailsOnMalformedPredictionServiceGeoJson()
		{
			string tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "fake tiff content");

			int port = GetFreeTcpPort();
			string baseUrl = $"http://localhost:{port}/";
			const string malformedGeoJson =
				"{\"type\":\"FeatureCollection\",\"features\":[{\"type\":\"Feature\"," +
				"\"properties\":{}}]}";

			try
			{
				using (var listener = new HttpListener())
				{
					listener.Prefixes.Add(baseUrl);
					listener.Start();

					Task requestTask = Task.Run(
						() => HandlePredictionServiceJobRequests(
							listener, malformedGeoJson,
							expectedTarget: "buildings"));

					var client = new AreaPredictionClient();
					Assert.Throws<InvalidOperationException>(
						() => client.GetPredictions(
							tempFile, baseUrl, "roof-model",
							TimeSpan.FromSeconds(10)));

					requestTask.GetAwaiter().GetResult();
				}
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[Test]
		public void PredictionClientCanSendTileOverlapRatio()
		{
			string tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "fake tiff content");

			int port = GetFreeTcpPort();
			string baseUrl = $"http://localhost:{port}/";

			try
			{
				using (var listener = new HttpListener())
				{
					listener.Prefixes.Add(baseUrl);
					listener.Start();

					Task requestTask = Task.Run(
						() => HandlePredictionServiceJobRequests(
							listener,
							ToGeoJsonResponse(
								new[] { "[[10,20],[30,20],[30,40],[10,20]]" }),
							"buildings",
							assertQuery: query =>
							{
								Assert.AreEqual("0.75",
								                query["tile_overlap_ratio"]);
							}));

					var client = new AreaPredictionClient();
					var predictions = client.GetPredictions(
						tempFile, baseUrl, "roof-model",
						TimeSpan.FromSeconds(10), tileOverlapRatio: 0.75);

					requestTask.GetAwaiter().GetResult();

					Assert.AreEqual(1, predictions.Count);
				}
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[Test]
		public void PredictionClientDoesNotSendUnsupportedPredictionServiceDebugParameter()
		{
			string tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "fake tiff content");

			int port = GetFreeTcpPort();
			string baseUrl = $"http://localhost:{port}/";

			try
			{
				using (var listener = new HttpListener())
				{
					listener.Prefixes.Add(baseUrl);
					listener.Start();

					Task requestTask = Task.Run(
						() => HandlePredictionServiceJobRequests(
							listener,
							ToGeoJsonResponse(
								new[] { "[[10,20],[30,20],[30,40],[10,20]]" }),
							"buildings",
							assertQuery: query =>
							{
								Assert.IsNull(query["debug"]);
							}));

					var client = new AreaPredictionClient();
					var predictions = client.GetPredictions(
						tempFile, baseUrl, "roof-model", TimeSpan.FromSeconds(10));

					requestTask.GetAwaiter().GetResult();

					Assert.AreEqual(1, predictions.Count);
				}
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[Test]
		public void PredictionClientDoesNotSendUnsupportedPredictionServiceTriageParameter()
		{
			string tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "fake tiff content");

			int port = GetFreeTcpPort();
			string baseUrl = $"http://localhost:{port}/";

			try
			{
				using (var listener = new HttpListener())
				{
					listener.Prefixes.Add(baseUrl);
					listener.Start();

					Task requestTask = Task.Run(
						() => HandlePredictionServiceJobRequests(
							listener,
							ToGeoJsonResponse(
								new[] { "[[10,20],[30,20],[30,40],[10,20]]" }),
							"buildings",
							assertQuery: query =>
							{
								Assert.IsNull(query["use_triage"]);
							}));

					var client = new AreaPredictionClient();
					var predictions = client.GetPredictions(
						tempFile, baseUrl, "roof-model", TimeSpan.FromSeconds(10));

					requestTask.GetAwaiter().GetResult();

					Assert.AreEqual(1, predictions.Count);
				}
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[Test]
		public void PredictionClientFailsOnPolygonWithInteriorRing()
		{
			string tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "fake tiff content");

			int port = GetFreeTcpPort();
			string baseUrl = $"http://localhost:{port}/";
			const string polygonWithHole =
				"{\"type\":\"FeatureCollection\",\"features\":[{\"type\":\"Feature\"," +
				"\"geometry\":{\"type\":\"Polygon\",\"coordinates\":[" +
				"[[0,0],[10,0],[10,10],[0,10],[0,0]]," +
				"[[2,2],[2,4],[4,4],[2,2]]]},\"properties\":{}}]}";

			try
			{
				using (var listener = new HttpListener())
				{
					listener.Prefixes.Add(baseUrl);
					listener.Start();

					Task requestTask = Task.Run(
						() => HandlePredictionServiceJobRequests(
							listener, polygonWithHole,
							expectedTarget: "buildings"));

					var client = new AreaPredictionClient();
					InvalidOperationException exception =
						Assert.Throws<InvalidOperationException>(
							() => client.GetPredictions(
								tempFile, baseUrl, "roof-model",
								TimeSpan.FromSeconds(10)));

					Assert.That(exception.Message, Does.Contain("interior rings"));

					requestTask.GetAwaiter().GetResult();
				}
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[Test]
		public void PredictionClientIncludesPredictionServiceDiagnosticsOnFailedJob()
		{
			string tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "fake tiff content");

			int port = GetFreeTcpPort();
			string baseUrl = $"http://localhost:{port}/";

			try
			{
				using (var listener = new HttpListener())
				{
					listener.Prefixes.Add(baseUrl);
					listener.Start();

					Task requestTask = Task.Run(
						() => HandlePredictionServiceFailedJobRequests(listener));

					var client = new AreaPredictionClient();
					InvalidOperationException exception =
						Assert.Throws<InvalidOperationException>(
							() => client.GetPredictions(
								tempFile, baseUrl, "roof-model",
								TimeSpan.FromSeconds(10)));

					Assert.That(exception.Message, Does.Contain("test-job"));
					Assert.That(exception.Message, Does.Contain("failed"));
					Assert.That(exception.Message, Does.Contain("GPU unavailable"));
					Assert.That(exception.Message, Does.Contain("final_polygons: 0"));
					Assert.That(exception.Message, Does.Contain("Inference failed"));

					requestTask.GetAwaiter().GetResult();
				}
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		// Run manually with prediction-service.yml plus:
		// $env:PREDICTION_RUNPOD_ID = "<pod id>"
		// $env:PREDICTION_API_KEY = "<predictionService api key>"
		private static void HandlePredictionServiceJobRequests(HttpListener listener)
		{
			HandlePredictionServiceJobRequests(
				listener,
				ToGeoJsonResponse(new[] { "[[10,20],[30,20],[30,40],[10,20]]" }),
				"buildings",
				assertBody: body =>
				{
					Assert.That(body, Does.Contain("fake tiff content"));
					AssertMultipartFileField(body);
				});
		}

		private static void HandlePredictionServiceJobRequests(
			HttpListener listener,
			string responseJson,
			string expectedTarget,
			Action<string> assertBody = null,
			Action<System.Collections.Specialized.NameValueCollection> assertQuery = null)
		{
			HttpListenerContext createJobContext = listener.GetContext();

			Assert.AreEqual("POST", createJobContext.Request.HttpMethod);
			Assert.AreEqual("/jobs", createJobContext.Request.Url.AbsolutePath);
			Assert.IsNull(createJobContext.Request.QueryString["regularize"]);
			Assert.AreEqual(expectedTarget,
			                createJobContext.Request.QueryString["target"]);
			Assert.IsNull(createJobContext.Request.QueryString["model_name"]);
			assertQuery?.Invoke(createJobContext.Request.QueryString);
			Assert.That(createJobContext.Request.ContentType,
			            Does.Contain("multipart/form-data"));

			using (var reader = new StreamReader(createJobContext.Request.InputStream,
			                                     createJobContext.Request.ContentEncoding))
			{
				string body = reader.ReadToEnd();
				AssertMultipartFileField(body);
				assertBody?.Invoke(body);
			}

			WriteJsonResponse(createJobContext, "{\"job_id\":\"test-job\"}",
			                  statusCode: 202);

			HttpListenerContext statusContext = listener.GetContext();
			Assert.AreEqual("GET", statusContext.Request.HttpMethod);
			Assert.AreEqual("/jobs/test-job", statusContext.Request.Url.AbsolutePath);
			WriteJsonResponse(
				statusContext,
				"{\"job_id\":\"test-job\",\"status\":\"done\"," +
				"\"tiles_done\":4,\"tiles_total\":4,\"filename\":\"tile.tif\"," +
				"\"diagnostics\":{\"model_name\":\"roof-model\"," +
				"\"input_tiles\":4,\"tiles_sent_to_pix2poly\":4," +
				"\"final_polygons\":1," +
				"\"debug_artifacts\":{\"final\":\"/tmp/predictionService_jobs/test-job/debug/final.geojson\"}," +
				"\"warnings\":[]}}");

			HttpListenerContext resultContext = listener.GetContext();
			Assert.AreEqual("GET", resultContext.Request.HttpMethod);
			Assert.AreEqual("/jobs/test-job/result",
			                resultContext.Request.Url.AbsolutePath);
			WriteJsonResponse(resultContext, responseJson);
		}

		private static void HandlePredictionServiceFailedJobRequests(HttpListener listener)
		{
			HttpListenerContext createJobContext = listener.GetContext();

			Assert.AreEqual("POST", createJobContext.Request.HttpMethod);
			Assert.AreEqual("/jobs", createJobContext.Request.Url.AbsolutePath);
			Assert.AreEqual("buildings",
			                createJobContext.Request.QueryString["target"]);
			Assert.IsNull(createJobContext.Request.QueryString["model_name"]);

			using (var reader = new StreamReader(createJobContext.Request.InputStream,
			                                     createJobContext.Request.ContentEncoding))
			{
				AssertMultipartFileField(reader.ReadToEnd());
			}

			WriteJsonResponse(createJobContext, "{\"job_id\":\"test-job\"}",
			                  statusCode: 202);

			HttpListenerContext statusContext = listener.GetContext();
			Assert.AreEqual("GET", statusContext.Request.HttpMethod);
			Assert.AreEqual("/jobs/test-job", statusContext.Request.Url.AbsolutePath);
			WriteJsonResponse(
				statusContext,
				"{\"job_id\":\"test-job\",\"status\":\"failed\"," +
				"\"tiles_done\":0,\"tiles_total\":4,\"filename\":\"tile.tif\"," +
				"\"error\":\"GPU unavailable\"," +
				"\"diagnostics\":{\"model_name\":\"roof-model\"," +
				"\"input_tiles\":4,\"tiles_sent_to_pix2poly\":0," +
				"\"final_polygons\":0," +
				"\"warnings\":[\"Inference failed: GPU unavailable\"]}}");
		}

		private static void HandleTwoPredictionServiceJobsWithSecondCompletingFirst(
			HttpListener listener)
		{
			var submissionCount = 0;
			var firstJobStatusCount = 0;
			for (var requestCount = 0; requestCount < 7; requestCount++)
			{
				HttpListenerContext context = listener.GetContext();
				string path = context.Request.Url.AbsolutePath;
				if (context.Request.HttpMethod == "POST")
				{
					using (var reader = new StreamReader(context.Request.InputStream,
					                                     context.Request.ContentEncoding))
					{
						reader.ReadToEnd();
					}

					WriteJsonResponse(
						context, $"{{\"job_id\":\"job-{submissionCount++}\"}}",
						statusCode: 202);
				}
				else if (path.EndsWith("/result", StringComparison.Ordinal))
				{
					WriteJsonResponse(
						context,
						ToGeoJsonResponse(
							new[] { "[[10,20],[30,20],[30,40],[10,20]]" }));
				}
				else if (path.EndsWith("job-1", StringComparison.Ordinal))
				{
					WriteJsonResponse(
						context, "{\"job_id\":\"job-1\",\"status\":\"done\"}");
				}
				else
				{
					string status = firstJobStatusCount++ == 0 ? "running" : "done";
					WriteJsonResponse(
						context,
						$"{{\"job_id\":\"job-0\",\"status\":\"{status}\"}}");
				}
			}
		}

		private static void WriteJsonResponse(HttpListenerContext context,
		                                      string responseJson,
		                                      int statusCode = 200)
		{
			byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);

			context.Response.StatusCode = statusCode;
			context.Response.ContentType = "application/json";
			context.Response.ContentLength64 = responseBytes.Length;
			context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
			context.Response.Close();
		}

		private static void AssertMultipartFileField(string body)
		{
			Assert.That(body, Does.Contain("name=file")
			                      .Or.Contain("name=\"file\""));
			Assert.That(body, Does.Contain("filename="));
		}

		private static int GetFreeTcpPort()
		{
			var listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();
			int port = ((IPEndPoint) listener.LocalEndpoint).Port;
			listener.Stop();

			return port;
		}

		private static CocoFixture LoadFirstAnnotatedImage(string annotationPath,
		                                                   string fileName)
		{
			using (var stream = File.OpenRead(annotationPath))
			{
				var serializer = new DataContractJsonSerializer(typeof(CocoDataset));
				var dataset = (CocoDataset) serializer.ReadObject(stream);

				CocoImage image = dataset.Images.Single(i => i.FileName == fileName);
				List<CocoAnnotation> annotations = dataset.Annotations
				                                          .Where(a => a.ImageId == image.Id &&
				                                                      a.Area > 0)
				                                          .ToList();

				Assert.That(annotations, Is.Not.Empty);

				return new CocoFixture(image, annotations);
			}
		}

		private static string ToPredictionServiceGeoJsonResponseJson(
			IEnumerable<CocoAnnotation> annotations)
		{
			var polygons = new List<string>();

			foreach (CocoAnnotation annotation in annotations)
			{
				List<double> shell = annotation.Segmentation.First();
				var points = new List<string>();

				for (var i = 0; i < shell.Count - 1; i += 2)
				{
					points.Add($"[{shell[i]},{shell[i + 1]}]");
				}

				polygons.Add("[" + string.Join(",", points) + "]");
			}

			return ToGeoJsonResponse(polygons);
		}

		private static string ToGeoJsonResponse(IEnumerable<string> polygonShellsJson)
		{
			var features = new List<string>();
			foreach (string polygonShellJson in polygonShellsJson)
			{
				features.Add(
					"{\"type\":\"Feature\",\"geometry\":{\"type\":\"Polygon\"," +
					"\"coordinates\":[" + polygonShellJson +
					"]},\"properties\":{}}");
			}

			return "{\"type\":\"FeatureCollection\",\"features\":[" +
			       string.Join(",", features) + "]}";
		}

		private class CocoFixture
		{
			public CocoFixture(CocoImage image, List<CocoAnnotation> annotations)
			{
				Image = image;
				Annotations = annotations;
			}

			public CocoImage Image { get; }

			public List<CocoAnnotation> Annotations { get; }
		}

		[DataContract]
		private class CocoDataset
		{
			[DataMember(Name = "images")]
			public List<CocoImage> Images { get; set; }

			[DataMember(Name = "annotations")]
			public List<CocoAnnotation> Annotations { get; set; }
		}

		[DataContract]
		private class CocoImage
		{
			[DataMember(Name = "id")]
			public int Id { get; set; }

			[DataMember(Name = "file_name")]
			public string FileName { get; set; }
		}

		[DataContract]
		private class CocoAnnotation
		{
			[DataMember(Name = "image_id")]
			public int ImageId { get; set; }

			[DataMember(Name = "segmentation")]
			public List<List<double>> Segmentation { get; set; }

			[DataMember(Name = "area")]
			public double Area { get; set; }
		}

	}
}
