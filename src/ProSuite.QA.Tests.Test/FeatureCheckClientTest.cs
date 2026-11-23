using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class FeatureCheckClientTest
	{
		private const string _resultJson =
			"{\"type\":\"FeatureCollection\"," +
			"\"crs\":{\"type\":\"name\",\"properties\":{\"name\":\"EPSG:2056\"}}," +
			"\"check\":{\"prompt\":\"rule\",\"model\":\"test-model\",\"runs_per_feature\":2,\"cost_usd\":0.0412}," +
			"\"features\":[" +
			"{\"type\":\"Feature\",\"properties\":{\"prosuite_oid\":7,\"check_status\":\"checked\"," +
			"\"check_reported\":true,\"check_category\":\"plastic_tunnel\",\"check_error_probability\":85," +
			"\"check_reason\":\"Folientunnel.\",\"check_runs\":[{\"error_probability\":85},{\"error_probability\":60}]}," +
			"\"geometry\":{\"type\":\"MultiPolygon\",\"coordinates\":[[[[0,0],[1,0],[1,1],[0,0]]]]}}," +
			"{\"type\":\"Feature\",\"properties\":{\"prosuite_oid\":8.0,\"check_status\":\"outside_image\"}," +
			"\"geometry\":{\"type\":\"Polygon\",\"coordinates\":[[[0,0],[1,0],[1,1],[0,0]]]}}," +
			"{\"type\":\"Feature\",\"properties\":{\"prosuite_oid\":9,\"check_status\":\"failed\"," +
			"\"check_runs\":[{\"error\":\"timeout\"},{\"error\":\"timeout\"}]}," +
			"\"geometry\":{\"type\":\"Polygon\",\"coordinates\":[[[0,0],[1,0],[1,1],[0,0]]]}}]}";

		[Test]
		public void SendsAllPartsAndReadsResults()
		{
			string imagePath = Path.GetTempFileName();
			File.WriteAllText(imagePath, "fake tiff content");
			string baseUrl = $"http://localhost:{GetFreeTcpPort()}/";

			try
			{
				string body = null;
				using (var listener = new HttpListener())
				{
					listener.Prefixes.Add(baseUrl);
					listener.Start();

					Task server = Task.Run(() =>
					{
						HttpListenerContext submit = listener.GetContext();
						Assert.AreEqual("POST", submit.Request.HttpMethod);
						Assert.AreEqual("/jobs", submit.Request.Url.AbsolutePath);
						Assert.AreEqual("feature-check", submit.Request.QueryString["target"]);
						body = ReadBody(submit);
						WriteJson(submit, "{\"job_id\":\"job-1\"}", 202);

						HttpListenerContext running = listener.GetContext();
						Assert.AreEqual("/jobs/job-1", running.Request.Url.AbsolutePath);
						WriteJson(running, "{\"job_id\":\"job-1\",\"status\":\"running\"}");

						HttpListenerContext done = listener.GetContext();
						Assert.AreEqual("/jobs/job-1", done.Request.Url.AbsolutePath);
						WriteJson(done, "{\"job_id\":\"job-1\",\"status\":\"done\"}");

						HttpListenerContext result = listener.GetContext();
						Assert.AreEqual("/jobs/job-1/result", result.Request.Url.AbsolutePath);
						WriteJson(result, _resultJson);
					});

					var request = new FeatureCheckRequest(
						imagePath, "{\"features-marker\":1}", "{\"context-marker\":1}",
						"Regel mit Umlaut ä\r\nund zweiter Zeile");

					IList<FeatureCheckJobResult> jobs = new FeatureCheckClient().CheckFeatures(
						new[] { request }, baseUrl, TimeSpan.FromSeconds(20));

					server.GetAwaiter().GetResult();

					Assert.AreEqual(1, jobs.Count);
					FeatureCheckJobResult job = jobs[0];
					Assert.IsTrue(job.Succeeded, job.Error);
					Assert.AreEqual("job-1", job.JobId);
					Assert.AreEqual(0.0412, job.CostUsd);
					Assert.AreEqual("test-model", job.Model);
					Assert.AreEqual(3, job.Results.Count);

					FeatureCheckResult reported = job.Results[0];
					Assert.AreEqual(7, reported.ObjectId);
					Assert.AreEqual(FeatureCheckStatus.Checked, reported.Status);
					Assert.AreEqual(85, reported.ErrorProbability);
					Assert.AreEqual("plastic_tunnel", reported.Category);
					Assert.AreEqual("Folientunnel.", reported.Reason);

					Assert.AreEqual(8, job.Results[1].ObjectId);
					Assert.AreEqual(FeatureCheckStatus.OutsideImage, job.Results[1].Status);
					Assert.IsNull(job.Results[1].ErrorProbability);

					Assert.AreEqual(FeatureCheckStatus.Failed, job.Results[2].Status);
				}

				StringAssert.Contains("name=\"file\"; filename=\"", body);
				StringAssert.Contains("fake tiff content", body);
				StringAssert.Contains("name=\"features\"; filename=\"features.geojson\"", body);
				StringAssert.Contains("{\"features-marker\":1}", body);
				StringAssert.Contains("name=\"context\"; filename=\"context.geojson\"", body);
				StringAssert.Contains("{\"context-marker\":1}", body);
				StringAssert.Contains("name=\"prompt\"\r\n", body);
				StringAssert.Contains("Regel mit Umlaut ä\r\nund zweiter Zeile", body);
			}
			finally
			{
				File.Delete(imagePath);
			}
		}

		[Test]
		public void ReportsTheServiceReasonWhenSubmittingFails()
		{
			string imagePath = Path.GetTempFileName();
			string baseUrl = $"http://localhost:{GetFreeTcpPort()}/";

			try
			{
				using (var listener = new HttpListener())
				{
					listener.Prefixes.Add(baseUrl);
					listener.Start();

					Task server = Task.Run(() =>
					{
						HttpListenerContext submit = listener.GetContext();
						ReadBody(submit);
						WriteJson(submit,
						          "{\"detail\":\"No worker serves 'feature-check'.\"}", 503);
					});

					var request = new FeatureCheckRequest(imagePath, "{}", null, "rule");
					IList<FeatureCheckJobResult> jobs = new FeatureCheckClient().CheckFeatures(
						new[] { request }, baseUrl, TimeSpan.FromSeconds(20));

					server.GetAwaiter().GetResult();

					Assert.IsFalse(jobs[0].Succeeded);
					Assert.IsNull(jobs[0].JobId);
					StringAssert.Contains("No worker serves 'feature-check'.", jobs[0].Error);
				}
			}
			finally
			{
				File.Delete(imagePath);
			}
		}

		[Test]
		public void FailsAllRequestsWhenTheServiceCannotBeReached()
		{
			string imagePath = Path.GetTempFileName();
			try
			{
				// Nothing listens on this port.
				string baseUrl = $"http://localhost:{GetFreeTcpPort()}/";
				var request = new FeatureCheckRequest(imagePath, "{}", null, "rule");

				IList<FeatureCheckJobResult> jobs = new FeatureCheckClient().CheckFeatures(
					new[] { request, request }, baseUrl, TimeSpan.FromSeconds(20));

				Assert.IsFalse(jobs[0].Succeeded);
				Assert.IsFalse(jobs[1].Succeeded);
				StringAssert.Contains("cannot be reached", jobs[1].Error);
			}
			finally
			{
				File.Delete(imagePath);
			}
		}

		[Test]
		public void ReportsAFailedJob()
		{
			string imagePath = Path.GetTempFileName();
			string baseUrl = $"http://localhost:{GetFreeTcpPort()}/";

			try
			{
				using (var listener = new HttpListener())
				{
					listener.Prefixes.Add(baseUrl);
					listener.Start();

					Task server = Task.Run(() =>
					{
						HttpListenerContext submit = listener.GetContext();
						string body = ReadBody(submit);
						StringAssert.DoesNotContain("name=\"context\"", body);
						WriteJson(submit, "{\"job_id\":\"job-2\"}", 202);

						HttpListenerContext status = listener.GetContext();
						WriteJson(status,
						          "{\"job_id\":\"job-2\",\"status\":\"failed\"," +
						          "\"error\":\"None of the 3 features lies fully inside the image.\"}");
					});

					var request = new FeatureCheckRequest(imagePath, "{}", null, "rule");
					IList<FeatureCheckJobResult> jobs = new FeatureCheckClient().CheckFeatures(
						new[] { request }, baseUrl, TimeSpan.FromSeconds(20));

					server.GetAwaiter().GetResult();

					Assert.IsFalse(jobs[0].Succeeded);
					Assert.AreEqual("job-2", jobs[0].JobId);
					StringAssert.Contains("lies fully inside the image", jobs[0].Error);
				}
			}
			finally
			{
				File.Delete(imagePath);
			}
		}

		private static string ReadBody(HttpListenerContext context)
		{
			using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
			{
				return reader.ReadToEnd();
			}
		}

		private static void WriteJson(HttpListenerContext context, string json,
		                              int statusCode = 200)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(json);
			context.Response.StatusCode = statusCode;
			context.Response.ContentType = "application/json";
			context.Response.ContentLength64 = bytes.Length;
			context.Response.OutputStream.Write(bytes, 0, bytes.Length);
			context.Response.Close();
		}

		internal static int GetFreeTcpPort()
		{
			var listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();
			int port = ((IPEndPoint) listener.LocalEndpoint).Port;
			listener.Stop();
			return port;
		}
	}
}
