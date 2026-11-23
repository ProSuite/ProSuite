using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// The request handling that every job of the prediction service shares: submit a
	/// multipart upload, poll the job status, read the result, send the API key.
	/// </summary>
	internal static class PredictionServiceHttp
	{
		private const string _apiKeyVariable = "PREDICTION_API_KEY";

		[NotNull]
		public static T PostMultipart<T>([NotNull] string url,
		                                 [NotNull] IEnumerable<MultipartPart> parts,
		                                 DateTime timeoutAt) where T : class
		{
			string boundary = "----ProSuitePredictionService" + Guid.NewGuid().ToString("N");

			HttpWebRequest request = CreateRequest(url, "POST", timeoutAt);
			request.ContentType = "multipart/form-data; boundary=" + boundary;

			using (Stream requestStream = request.GetRequestStream())
			{
				foreach (MultipartPart part in parts)
				{
					part.Write(requestStream, boundary);
				}

				WriteText(requestStream, "--" + boundary + "--\r\n");
			}

			return ReadJson<T>(request);
		}

		[NotNull]
		public static T GetJson<T>([NotNull] string url, DateTime timeoutAt) where T : class
		{
			return ReadJson<T>(CreateRequest(url, "GET", timeoutAt));
		}

		[NotNull]
		public static string BuildJobStatusUrl([NotNull] string apiUrl,
		                                       [NotNull] string jobId)
		{
			return CombineApiPath(apiUrl, "jobs/" + Uri.EscapeDataString(jobId));
		}

		[NotNull]
		public static string BuildJobResultUrl([NotNull] string apiUrl,
		                                       [NotNull] string jobId)
		{
			return CombineApiPath(
				apiUrl, "jobs/" + Uri.EscapeDataString(jobId) + "/result");
		}

		[NotNull]
		public static string CombineApiPath([NotNull] string apiUrl, [NotNull] string path)
		{
			string separator = apiUrl.EndsWith("/", StringComparison.Ordinal)
				                   ? string.Empty
				                   : "/";
			return apiUrl + separator + path;
		}

		public static TimeSpan RemainingTimeout(DateTime timeoutAt)
		{
			TimeSpan remaining = timeoutAt - DateTime.UtcNow;
			if (remaining <= TimeSpan.Zero)
			{
				throw new TimeoutException(
					"Timed out waiting for Prediction service job result");
			}

			return remaining;
		}

		/// <summary>
		/// Waits at most one second, and never beyond <paramref name="timeoutAt"/>.
		/// </summary>
		public static void WaitBeforeNextPoll(DateTime timeoutAt)
		{
			TimeSpan remaining = timeoutAt - DateTime.UtcNow;
			if (remaining <= TimeSpan.Zero)
			{
				return;
			}

			Thread.Sleep(remaining < TimeSpan.FromSeconds(1)
				             ? remaining
				             : TimeSpan.FromSeconds(1));
		}

		[NotNull]
		private static HttpWebRequest CreateRequest([NotNull] string url,
		                                            [NotNull] string method,
		                                            DateTime timeoutAt)
		{
			var request = (HttpWebRequest) WebRequest.Create(url);

			string apiKey = Environment.GetEnvironmentVariable(_apiKeyVariable);
			if (! string.IsNullOrWhiteSpace(apiKey))
			{
				request.Headers["X-API-Key"] = apiKey;
			}

			request.Method = method;
			request.Accept = "application/json";

			int timeoutMilliseconds = ToTimeoutMilliseconds(RemainingTimeout(timeoutAt));
			request.Timeout = timeoutMilliseconds;
			request.ReadWriteTimeout = timeoutMilliseconds;

			return request;
		}

		[NotNull]
		private static T ReadJson<T>([NotNull] HttpWebRequest request) where T : class
		{
			try
			{
				using (var response = (HttpWebResponse) request.GetResponse())
				using (Stream stream = response.GetResponseStream())
				{
					var serializer = new DataContractJsonSerializer(typeof(T));
					var result = (T) serializer.ReadObject(Assert.NotNull(stream));

					if (result == null)
					{
						throw new InvalidOperationException(
							$"Prediction service returned an empty response for {request.RequestUri}");
					}

					return result;
				}
			}
			catch (WebException e) when (e.Response is HttpWebResponse errorResponse)
			{
				// The body holds the service's reason, e.g. that no worker is running.
				string body = ReadBody(errorResponse);
				throw new WebException(
					$"{e.Message} {request.Method} {request.RequestUri}" +
					(string.IsNullOrWhiteSpace(body) ? string.Empty : $": {body}"),
					e, e.Status, e.Response);
			}
		}

		[CanBeNull]
		private static string ReadBody([NotNull] HttpWebResponse response)
		{
			try
			{
				using (Stream stream = response.GetResponseStream())
				{
					if (stream == null)
					{
						return null;
					}

					using (var reader = new StreamReader(stream, Encoding.UTF8))
					{
						return reader.ReadToEnd().Trim();
					}
				}
			}
			catch (IOException)
			{
				return null;
			}
			catch (WebException)
			{
				return null;
			}
		}

		private static int ToTimeoutMilliseconds(TimeSpan timeout)
		{
			if (timeout.TotalMilliseconds >= int.MaxValue)
			{
				return int.MaxValue;
			}

			return Math.Max(1, (int) timeout.TotalMilliseconds);
		}

		private static void WriteText([NotNull] Stream stream, [NotNull] string text)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(text);
			stream.Write(bytes, 0, bytes.Length);
		}

		/// <summary>
		/// One field of a multipart upload, either a file or a text.
		/// </summary>
		public class MultipartPart
		{
			[NotNull] private readonly string _name;
			[CanBeNull] private readonly string _fileName;
			[NotNull] private readonly string _contentType;
			[CanBeNull] private readonly string _filePath;
			[CanBeNull] private readonly string _text;

			private MultipartPart([NotNull] string name,
			                      [CanBeNull] string fileName,
			                      [NotNull] string contentType,
			                      [CanBeNull] string filePath,
			                      [CanBeNull] string text)
			{
				_name = name;
				_fileName = fileName;
				_contentType = contentType;
				_filePath = filePath;
				_text = text;
			}

			[NotNull]
			public static MultipartPart FromFile([NotNull] string name,
			                                     [NotNull] string filePath,
			                                     [NotNull] string contentType)
			{
				Assert.ArgumentNotNullOrEmpty(filePath, nameof(filePath));

				return new MultipartPart(name, Path.GetFileName(filePath), contentType,
				                         filePath, null);
			}

			/// <summary>
			/// A text sent as an uploaded file, for services that read the field as a file.
			/// </summary>
			[NotNull]
			public static MultipartPart FromTextAsFile([NotNull] string name,
			                                           [NotNull] string fileName,
			                                           [NotNull] string text,
			                                           [NotNull] string contentType)
			{
				return new MultipartPart(name, fileName, contentType, null, text);
			}

			[NotNull]
			public static MultipartPart FromText([NotNull] string name, [NotNull] string text)
			{
				return new MultipartPart(name, null, "text/plain; charset=utf-8", null, text);
			}

			public void Write([NotNull] Stream stream, [NotNull] string boundary)
			{
				string disposition = $"Content-Disposition: form-data; name=\"{_name}\"";
				if (_fileName != null)
				{
					disposition += $"; filename=\"{_fileName}\"";
				}

				WriteText(stream,
				          "--" + boundary + "\r\n" +
				          disposition + "\r\n" +
				          "Content-Type: " + _contentType + "\r\n\r\n");

				if (_filePath != null)
				{
					using (FileStream fileStream = File.OpenRead(_filePath))
					{
						fileStream.CopyTo(stream);
					}
				}
				else
				{
					WriteText(stream, _text ?? string.Empty);
				}

				WriteText(stream, "\r\n");
			}
		}
	}
}
