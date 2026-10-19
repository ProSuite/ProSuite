using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using NUnit.Framework;

namespace ProSuite.QA.Tests.Test
{
	internal class PredictionServiceTestConfig
	{
		private const string _configFileName = "prediction-service.yml";

		private PredictionServiceTestConfig(string apiUrl,
		                               string apiKey,
		                               string modelName,
		                               string imagePath)
		{
			ApiUrl = apiUrl;
			ApiKey = apiKey;
			ModelName = modelName;
			ImagePath = imagePath;
		}

		public string ApiUrl { get; }

		public string ApiKey { get; }

		public string ModelName { get; }

		public string ImagePath { get; }

		public static PredictionServiceTestConfig LoadOrIgnore(bool requireMatchingFeatures = false)
		{
			string configPath = FindConfigPath();
			if (configPath == null)
			{
				Assert.Ignore("Prediction service config file not found: " + _configFileName);
			}

			Dictionary<string, string> settings = ReadFlatYaml(configPath);

			string apiKey = Environment.GetEnvironmentVariable("PREDICTION_API_KEY");
			if (string.IsNullOrWhiteSpace(apiKey))
			{
				Assert.Ignore("PREDICTION_API_KEY is not set.");
			}

			string apiUrl = GetRequiredSetting(settings, "api_url", configPath).TrimEnd('/');

			string modelName = GetRequiredSetting(settings, "model_name", configPath);
			string imagePath = GetRequiredSetting(settings, "test_tif", configPath);
			if (! File.Exists(imagePath))
			{
				Assert.Ignore("Test TIFF not found: " + imagePath);
			}

			var result = new PredictionServiceTestConfig(
				apiUrl, apiKey, modelName, imagePath);
			if (requireMatchingFeatures)
			{
				result.ShapefilePath = GetRequiredSetting(settings, "test_shp", configPath);
				Assert.That(File.Exists(result.ShapefilePath), Is.True,
				            "Configured reference shapefile must exist.");
				result.MatchedFeatureOid = GetRequiredNonNegativeIntSetting(
					settings, "matched_feature_oid", configPath);
				result.MissingFeatureOid = GetRequiredNonNegativeIntSetting(
					settings, "missing_feature_oid", configPath);
				result.ExpectedNewPredictionCount = GetRequiredNonNegativeIntSetting(
					settings, "expected_new_predictions", configPath);
				Assert.AreNotEqual(result.MatchedFeatureOid, result.MissingFeatureOid);
			}
			return result;
		}

		public string ShapefilePath { get; private set; }
		public int MatchedFeatureOid { get; private set; }
		public int MissingFeatureOid { get; private set; }
		public int ExpectedNewPredictionCount { get; private set; }

		private static int GetRequiredNonNegativeIntSetting(
			Dictionary<string, string> settings, string key, string configPath)
		{
			string value = GetRequiredSetting(settings, key, configPath);
			Assert.That(int.TryParse(value, out int result) && result >= 0, Is.True,
			            $"Setting '{key}' must be a non-negative integer.");
			return result;
		}

		public void AssertHealthy()
		{
			var request = (HttpWebRequest) WebRequest.Create(ApiUrl + "/health");
			request.Method = "GET";
			request.Accept = "application/json";
			request.Headers["X-API-Key"] = ApiKey;
			request.Timeout = ToMilliseconds(TimeSpan.FromSeconds(30));
			request.ReadWriteTimeout = request.Timeout;

			using (var response = (HttpWebResponse) request.GetResponse())
			using (Stream responseStream = response.GetResponseStream())
			using (var reader = new StreamReader(responseStream))
			{
				string responseJson = reader.ReadToEnd();

				Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
				Assert.That(responseJson, Does.Contain("\"redis\":\"ok\"")
				                             .Or.Contain("\"redis\": \"ok\""));
				Assert.That(responseJson, Does.Contain("\"gpu_available\":true")
				                             .Or.Contain("\"gpu_available\": true"));
			}
		}

		private static string FindConfigPath()
		{
			string directory = TestContext.CurrentContext.TestDirectory;
			while (! string.IsNullOrEmpty(directory))
			{
				string path = Path.Combine(directory, _configFileName);
				if (File.Exists(path))
				{
					return path;
				}

				directory = Directory.GetParent(directory)?.FullName;
			}

			return null;
		}

		private static Dictionary<string, string> ReadFlatYaml(string path)
		{
			var result = new Dictionary<string, string>(
				StringComparer.OrdinalIgnoreCase);

			foreach (string rawLine in File.ReadAllLines(path))
			{
				string line = rawLine.Trim();
				if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
				{
					continue;
				}

				int separatorIndex = line.IndexOf(':');
				if (separatorIndex <= 0)
				{
					continue;
				}

				string key = line.Substring(0, separatorIndex).Trim();
				string value = line.Substring(separatorIndex + 1).Trim();
				int commentIndex = value.IndexOf(" #", StringComparison.Ordinal);
				if (commentIndex >= 0)
				{
					value = value.Substring(0, commentIndex).Trim();
				}

				result[key] = value.Trim('"', '\'');
			}

			return result;
		}

		private static string GetRequiredSetting(Dictionary<string, string> settings,
		                                         string key,
		                                         string configPath)
		{
			if (settings.TryGetValue(key, out string value) &&
			    ! string.IsNullOrWhiteSpace(value))
			{
				return value;
			}

			Assert.Fail($"Missing required setting '{key}' in {configPath}.");
			return null;
		}

		private static int ToMilliseconds(TimeSpan timeout)
		{
			return timeout.TotalMilliseconds >= int.MaxValue
				       ? int.MaxValue
				       : Math.Max(1, (int) timeout.TotalMilliseconds);
		}
	}
}
