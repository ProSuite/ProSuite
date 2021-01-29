using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Path = System.IO.Path;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaValidUrlsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private int _webRequestCounts;
		private IFeatureWorkspace _testWs;
		private int _sleepMilliseconds;

		[SetUp]
		public void SetUp()
		{
			_webRequestCounts = 0;
			_sleepMilliseconds = 0;
		}

		[TearDown]
		public void TearDown()
		{
			_webRequestCounts = 0;
			_sleepMilliseconds = 0;
		}

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);

			_testWs = TestWorkspaceUtils.CreateInMemoryWorkspace("QaValidUrlsTest");
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CreateWebRequestPerformance()
		{
			var watch = new Stopwatch();

			watch.Start();

			const int count = 100000;
			for (var i = 0; i < count; i++)
			{
				WebRequest.Create("http://blah.blah/blah.html");
			}

			watch.Stop();

			Console.WriteLine(@"{0:N0} requests: {1:N0} ms ({2:N3} ms per request)",
			                  count,
			                  watch.ElapsedMilliseconds,
			                  watch.ElapsedMilliseconds / (double) count);
		}

		[Test]
		public void CanCheckFeatureClassParallel()
		{
			const string urlRoot = "http://localhost:8080/";
			const string urlInfixExisting = "test/";
			const string existingPrefix = urlRoot + urlInfixExisting;
			const string urlFieldName = "URL_SUFFIX";

			const string testName = "CanCheckFeatureClassParallel";

			IFeatureClass featureClass = CreateFeatureClass(
				testName, esriGeometryType.esriGeometryPolyline, urlFieldName);

			int urlFieldIndex = featureClass.FindField(urlFieldName);

			AddFeatures(featureClass, urlFieldIndex,
			            string.Concat(existingPrefix, "duplicate1.html"),
			            string.Concat(existingPrefix, "duplicate1.html"),
			            string.Concat(existingPrefix, "duplicate1.html"),
			            string.Concat(existingPrefix, "exists1.html"),
			            string.Concat(existingPrefix, "exists2.html"),
			            string.Concat(existingPrefix, "exists3.html"),
			            string.Concat(existingPrefix, "exists4.html"),
			            string.Concat(existingPrefix, "exists5.html"),
			            string.Concat(existingPrefix, "exists6.html"),
			            string.Concat(existingPrefix, "exists7.html"),
			            string.Concat(existingPrefix, "exists8.html"),
			            string.Concat(existingPrefix, "exists9.html"),
			            string.Concat(existingPrefix, "exists10.html"),
			            string.Concat(existingPrefix, "exists11.html"),
			            string.Concat(existingPrefix, "exists12.html"),
			            string.Concat(existingPrefix, "exists13.html"),
			            string.Concat(existingPrefix, "exists14.html"),
			            string.Concat(existingPrefix, "exists15.html"),
			            string.Concat(existingPrefix, "exists16.html"),
			            string.Concat(existingPrefix, "exists17.html"),
			            string.Concat(existingPrefix, "exists18.html"),
			            string.Concat(existingPrefix, "exists19.html"),
			            string.Concat(existingPrefix, "exists20.html"),
			            string.Concat(existingPrefix, "exists21.html"),
			            string.Concat(existingPrefix, "exists22.html"),
			            string.Concat(existingPrefix, "exists23.html"),
			            string.Concat(existingPrefix, "exists24.html"),
			            string.Concat(existingPrefix, "exists25.html"),
			            string.Concat(existingPrefix, "exists26.html"),
			            string.Concat(existingPrefix, "exists27.html"),
			            string.Concat(existingPrefix, "exists28.html"),
			            string.Concat(existingPrefix, "exists29.html"),
			            string.Concat(existingPrefix, "exists30.html"),
			            string.Concat(existingPrefix, "duplicate2.html"),
			            string.Concat(existingPrefix, "duplicate2.html"),
			            string.Concat(existingPrefix, "duplicate2.html"),
			            string.Concat(urlRoot, "doesnotexist1.html"),
			            string.Concat(urlRoot, "doesnotexist2.html"),
			            string.Concat(urlRoot, "doesnotexist3.html"),
			            string.Concat(urlRoot, "doesnotexist4.html"));

			var test = new QaValidUrls((ITable) featureClass, urlFieldName);
			test.MaximumParallelTasks = 8;

			// small tile size, lots of crossing features
			var runner = new QaContainerTestRunner(500, test);

			_sleepMilliseconds = 100;
			using (var webServer = new WebServer(SendHeadResponse, existingPrefix))
			{
				webServer.Run();

				Stopwatch stopWatch = Stopwatch.StartNew();

				runner.Execute();

				stopWatch.Stop();

				Console.WriteLine(@"{0:N0} ms", stopWatch.ElapsedMilliseconds);
			}

			Assert.AreEqual(4, runner.Errors.Count);
			Assert.AreEqual(32, _webRequestCounts); // 404 responses are not counted
		}

		[Test]
		public void CanCheckTableParallel()
		{
			const string urlRoot = "http://localhost:8080/";
			const string urlInfixExisting = "test/";
			const string existingPrefix = urlRoot + urlInfixExisting;
			const string urlFieldName = "URL_SUFFIX";

			const string testName = "CanCheckTableParallel";

			ITable table = CreateTable(testName, urlFieldName);

			int urlFieldIndex = table.FindField(urlFieldName);

			AddRows(table, urlFieldIndex,
			        string.Concat(existingPrefix, "duplicate1.html"),
			        string.Concat(existingPrefix, "duplicate1.html"),
			        string.Concat(existingPrefix, "duplicate1.html"),
			        string.Concat(existingPrefix, "exists1.html"),
			        string.Concat(existingPrefix, "exists2.html"),
			        string.Concat(existingPrefix, "exists3.html"),
			        string.Concat(existingPrefix, "exists4.html"),
			        string.Concat(existingPrefix, "exists5.html"),
			        string.Concat(existingPrefix, "exists6.html"),
			        string.Concat(existingPrefix, "exists7.html"),
			        string.Concat(existingPrefix, "exists8.html"),
			        string.Concat(existingPrefix, "exists9.html"),
			        string.Concat(existingPrefix, "exists10.html"),
			        string.Concat(existingPrefix, "exists11.html"),
			        string.Concat(existingPrefix, "exists12.html"),
			        string.Concat(existingPrefix, "exists13.html"),
			        string.Concat(existingPrefix, "exists14.html"),
			        string.Concat(existingPrefix, "exists15.html"),
			        string.Concat(existingPrefix, "exists16.html"),
			        string.Concat(existingPrefix, "exists17.html"),
			        string.Concat(existingPrefix, "exists18.html"),
			        string.Concat(existingPrefix, "exists19.html"),
			        string.Concat(existingPrefix, "exists20.html"),
			        string.Concat(existingPrefix, "exists21.html"),
			        string.Concat(existingPrefix, "exists22.html"),
			        string.Concat(existingPrefix, "exists23.html"),
			        string.Concat(existingPrefix, "exists24.html"),
			        string.Concat(existingPrefix, "exists25.html"),
			        string.Concat(existingPrefix, "exists26.html"),
			        string.Concat(existingPrefix, "exists27.html"),
			        string.Concat(existingPrefix, "exists28.html"),
			        string.Concat(existingPrefix, "exists29.html"),
			        string.Concat(existingPrefix, "exists30.html"),
			        string.Concat(existingPrefix, "duplicate2.html"),
			        string.Concat(existingPrefix, "duplicate2.html"),
			        string.Concat(existingPrefix, "duplicate2.html"),
			        string.Concat(urlRoot, "doesnotexist1.html"),
			        string.Concat(urlRoot, "doesnotexist2.html"),
			        string.Concat(urlRoot, "doesnotexist3.html"),
			        string.Concat(urlRoot, "doesnotexist4.html"));

			var test = new QaValidUrls(table, urlFieldName);
			test.MaximumParallelTasks = 8;

			var runner = new QaContainerTestRunner(500, test);

			_sleepMilliseconds = 100;
			using (var webServer = new WebServer(SendHeadResponse, existingPrefix))
			{
				webServer.Run();

				Stopwatch stopWatch = Stopwatch.StartNew();

				runner.Execute();

				stopWatch.Stop();

				Console.WriteLine(@"{0:N0} ms", stopWatch.ElapsedMilliseconds);
			}

			Assert.AreEqual(4, runner.Errors.Count);
			Assert.AreEqual(32, _webRequestCounts); // 404 responses are not counted
		}

		[Test]
		public void CanCheckOnlyDistinctUrls()
		{
			const string urlRoot = "http://localhost:8080/";
			const string urlInfixExisting = "test/";
			const string existingPrefix = urlRoot + urlInfixExisting;
			const string helloWorldHtml = "hello_world.html";
			const string urlFieldName = "URL_SUFFIX";

			var objectClass = new ObjectClassMock(1, "testtable");
			objectClass.AddField(FieldUtils.CreateTextField(urlFieldName, 500));

			int urlFieldIndex = objectClass.FindField(urlFieldName);
			Assert.IsTrue(urlFieldIndex >= 0);

			IObject rowWithExistingUrl = CreateRow(objectClass, 1, urlFieldIndex,
			                                       helloWorldHtml);

			string urlExpression = $"'{existingPrefix}' + [{urlFieldName}]";

			var errorCount = 0;
			using (var webServer = new WebServer(SendHeadResponse, existingPrefix))
			{
				webServer.Run();

				var test = new QaValidUrls(objectClass, urlExpression);
				var runner = new QaTestRunner(test);

				errorCount += runner.Execute(rowWithExistingUrl);
				errorCount += runner.Execute(rowWithExistingUrl);
				errorCount += runner.Execute(rowWithExistingUrl);
			}

			Assert.AreEqual(0, errorCount);
			Assert.AreEqual(1, _webRequestCounts);
		}

		[Test]
		public void CrashesIfUnexistingField()
		{
			const string url = "http://localhost:8080/test/";

			var objectClass = new ObjectClassMock(1, "testtable");

			QaValidUrls test = null;
			using (var webServer = new WebServer(SendHeadResponse, url))
			{
				webServer.Run();

				try
				{
					test = new QaValidUrls(objectClass, "doesnotexist");

					Assert.Fail("Exception expected for non-existing field");
				}
				catch (ArgumentException e)
				{
					Console.WriteLine(e);
				}
			}

			Assert.IsNull(test);
		}

		[Test]
		public void CrashesIfInvalidExpression()
		{
			const string url = "http://localhost:8080/test/";

			var objectClass = new ObjectClassMock(1, "testtable");

			QaValidUrls test = null;
			using (var webServer = new WebServer(SendHeadResponse, url))
			{
				webServer.Run();

				try
				{
					test = new QaValidUrls(objectClass, "blah(blah)");

					Assert.Fail("Exception expected for invalid expression");
				}
				catch (ArgumentException e)
				{
					Console.WriteLine(e);
				}
			}

			Assert.IsNull(test);
		}

		[Test]
		public void CanCheckFileSystem()
		{
			const string urlFieldName = "URL";
			const string nameNonExistingFile = "doesnotexist.txt";
			const string nameExistingFile = "_testfile.txt";

			// C:\Users\<USER>\AppData\Local\Temp\
			string pathNonExistingFile = Path.Combine(Path.GetTempPath(), nameNonExistingFile);
			string pathExistingFile = Path.Combine(Path.GetTempPath(), nameExistingFile);

			string uncPathExistingFile =
				Path.Combine(
					string.Format(@"\\{0}\C$\Users\{1}\AppData\Local\Temp",
					              Environment.MachineName, Environment.UserName),
					nameExistingFile);
			string uncPathExistingDirectory =
				string.Format(@"\\{0}\C$\Users", Environment.MachineName);

			string filePathExistingFile = string.Format("file:///{0}",
			                                            Path.Combine(Path.GetTempPath(),
			                                                         nameExistingFile));

			CreateTextFile(pathExistingFile);

			try
			{
				var objectClass = new ObjectClassMock(1, "testtable");
				objectClass.AddField(FieldUtils.CreateTextField(urlFieldName, 500));

				int urlFieldIndex = objectClass.FindField(urlFieldName);
				Assert.IsTrue(urlFieldIndex >= 0);

				IObject rowNonExistingFile = CreateRow(objectClass, 1, urlFieldIndex,
				                                       pathNonExistingFile);
				IObject rowExistingFile = CreateRow(objectClass, 2, urlFieldIndex,
				                                    pathExistingFile);
				IObject rowExistingFileUncPath = CreateRow(objectClass, 3, urlFieldIndex,
				                                           uncPathExistingFile);
				IObject rowExistingFilePath = CreateRow(objectClass, 4, urlFieldIndex,
				                                        filePathExistingFile);
				IObject rowExistingDirectoryPath = CreateRow(objectClass, 5, urlFieldIndex,
				                                             uncPathExistingDirectory);

				var errorCount = 0;
				var test = new QaValidUrls(objectClass, urlFieldName);
				var runner = new QaTestRunner(test);

				errorCount += runner.Execute(rowNonExistingFile);
				errorCount += runner.Execute(rowExistingFile);
				errorCount += runner.Execute(rowExistingFileUncPath);
				errorCount += runner.Execute(rowExistingFilePath);
				errorCount += runner.Execute(rowExistingDirectoryPath);

				// NOTE: 3 errors when offline (unc path to C$ on local machine cannot be resolved)
				Assert.AreEqual(1, errorCount);
				Assert.AreEqual(0, _webRequestCounts); // no http requests expected
			}
			finally
			{
				File.Delete(pathExistingFile);
			}
		}

		[Test]
		public void CanHandleHttpAndFieldExpression()
		{
			const string urlFieldName = "URL_SUFFIX";
			const string urlRoot = "http://localhost:8080/";

			const string urlInfixExisting = "test/";
			const string urlInfixNotExisting = "doesnotexist/";
			const string existingPrefix = urlRoot + urlInfixExisting;

			var objectClass = new ObjectClassMock(1, "testtable");
			objectClass.AddField(FieldUtils.CreateTextField(urlFieldName, 500));

			int urlFieldIndex = objectClass.FindField(urlFieldName);
			Assert.IsTrue(urlFieldIndex >= 0);

			// ok as long as prefix matches:
			const string urlExistingPage = urlInfixExisting + "pagexy.html";

			// not ok since prefix does not match:
			const string urlNonExistingPage = urlInfixNotExisting + "doesnotexist.html";

			IObject rowWithExistingUrl = CreateRow(objectClass, 1, urlFieldIndex,
			                                       urlExistingPage);
			IObject rowWithNonExistingUrl = CreateRow(objectClass, 2, urlFieldIndex,
			                                          urlNonExistingPage);

			string urlExpression = $"'{urlRoot}' + [{urlFieldName}]";

			var errorCount = 0;
			using (var webServer = new WebServer(SendHeadResponse, existingPrefix))
			{
				webServer.Run();

				var test = new QaValidUrls(objectClass, urlExpression);
				var runner = new QaTestRunner(test);

				errorCount += runner.Execute(rowWithExistingUrl);
				errorCount += runner.Execute(rowWithNonExistingUrl);
			}

			Assert.AreEqual(1, errorCount);
		}

		[Test]
		public void CanHandleHttpAndSimpleFieldExpression()
		{
			const string urlFieldName = "URL";
			const string urlRoot = "http://localhost:8080/";

			const string urlInfixExisting = "test/";
			const string urlInfixNotExisting = "doesnotexist/";
			const string existingPrefix = urlRoot + urlInfixExisting;

			var objectClass = new ObjectClassMock(1, "testtable");
			objectClass.AddField(FieldUtils.CreateTextField(urlFieldName, 500));

			int urlFieldIndex = objectClass.FindField(urlFieldName);
			Assert.IsTrue(urlFieldIndex >= 0);

			// ok as long as prefix matches:
			const string urlExistingPage = urlRoot + urlInfixExisting + "pagexy.html";

			// not ok since prefix does not match:
			const string urlNonExistingPage =
				urlRoot + urlInfixNotExisting + "doesnotexist.html";

			IObject rowWithExistingUrl = CreateRow(objectClass, 1, urlFieldIndex,
			                                       urlExistingPage);
			IObject rowWithNonExistingUrl = CreateRow(objectClass, 2, urlFieldIndex,
			                                          urlNonExistingPage);
			IObject rowWithNullUrl = CreateRow(objectClass, 2, urlFieldIndex, null);

			// the expression contains no blanks - this checks for correct tokenization
			string urlExpression = $"TRIM({urlFieldName})";

			var errorCount = 0;
			using (var webServer = new WebServer(SendHeadResponse, existingPrefix))
			{
				webServer.Run();

				var test = new QaValidUrls(objectClass, urlExpression);
				var runner = new QaTestRunner(test);

				errorCount += runner.Execute(rowWithExistingUrl);
				errorCount += runner.Execute(rowWithNonExistingUrl);
				errorCount += runner.Execute(rowWithNullUrl);
			}

			Assert.AreEqual(1, errorCount);
		}

		[Test]
		public void CanHandleSpecialFieldValuesForUrls()
		{
			const string urlFieldName = "URL";

			var objectClass = new ObjectClassMock(1, "testtable");

			objectClass.AddField(urlFieldName, esriFieldType.esriFieldTypeString);
			int urlFieldIndex = objectClass.FindField(urlFieldName);
			Assert.IsTrue(urlFieldIndex >= 0);

			IObject rowWithEmptyValue = CreateRow(objectClass, 1, urlFieldIndex, string.Empty);
			IObject rowWithNullValue = CreateRow(objectClass, 2, urlFieldIndex, null);
			IObject rowWithBlanksOnlyValue = CreateRow(objectClass, 3, urlFieldIndex, "  ");

			var errorCount = 0;

			using (
				var webServer = new WebServer(SendHeadResponse,
				                              "http://localhost:8080/doesnotmatter/"))
			{
				webServer.Run();

				var test = new QaValidUrls(objectClass, urlFieldName);
				var runner = new QaTestRunner(test);

				errorCount += runner.Execute(rowWithEmptyValue);
				errorCount += runner.Execute(rowWithNullValue);
				errorCount += runner.Execute(rowWithBlanksOnlyValue);
			}

			Assert.AreEqual(0, errorCount);
			Assert.AreEqual(0, _webRequestCounts);
		}

		[Test]
		[Ignore("ftp handling not yet implemented (maybe not relevant)")]
		public void CanCheckFtpServer()
		{
			// Explanation how to set up a ftp server with IIS comes here.

			const string urlFieldName = "URL";

			var objectClass = new ObjectClassMock(1, "testtable");
			objectClass.AddField(FieldUtils.CreateTextField(urlFieldName, 500));

			int urlFieldIndex = objectClass.FindField(urlFieldName);
			Assert.IsTrue(urlFieldIndex >= 0);

			const string url = "ftp://thisisnotimplementedyet.ch/test/";
			IObject ftpUrl = CreateRow(objectClass, 1, urlFieldIndex, url);

			var errorCount = 0;

			var test = new QaValidUrls(objectClass, urlFieldName);

			var runner = new QaTestRunner(test);
			errorCount += runner.Execute(ftpUrl);

			Assert.AreEqual(1, errorCount);
		}

		private static void AddFeatures([NotNull] IFeatureClass featureClass,
		                                int urlFieldIndex,
		                                params string[] urlFieldValues)
		{
			Assert.IsTrue(urlFieldIndex >= 0);

			var i = 0;
			foreach (string value in urlFieldValues)
			{
				IFeature feature = featureClass.CreateFeature();

				double x = i * 100;
				double y = x;
				const double dx = 300;
				feature.Shape = GeometryFactory.CreatePolyline(x, y, x + dx, y);
				feature.Value[urlFieldIndex] = value;
				feature.Store();

				i++;
			}
		}

		private static void AddRows([NotNull] ITable table,
		                            int urlFieldIndex,
		                            params string[] urlFieldValues)
		{
			Assert.IsTrue(urlFieldIndex >= 0);

			foreach (string value in urlFieldValues)
			{
				IRow row = table.CreateRow();

				row.Value[urlFieldIndex] = value;
				row.Store();
			}
		}

		private static void CreateTextFile([NotNull] string path)
		{
			const string text = "Hello World";

			using (StreamWriter writer = File.CreateText(path))
			{
				writer.Write(text);
			}
		}

		[NotNull]
		private static IObject CreateRow([NotNull] ObjectClassMock mockObjectClass,
		                                 int oid,
		                                 int urlFieldIndex,
		                                 string url)
		{
			IObject result = mockObjectClass.CreateObject(oid);

			result.Value[urlFieldIndex] = (object) url ?? DBNull.Value;

			return result;
		}

		[NotNull]
		private IFeatureClass CreateFeatureClass([NotNull] string name,
		                                         esriGeometryType type,
		                                         [NotNull] string urlFieldName)
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);

			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000, 0.0001,
			                                  0.001);

			const bool mAware = false;
			const bool hasZ = false;
			IFields fields = FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField("Shape",
				                            type,
				                            sref, 1000,
				                            hasZ, mAware),
				FieldUtils.CreateTextField(urlFieldName, 500));

			return DatasetUtils.CreateSimpleFeatureClass(_testWs, name, fields);
		}

		[NotNull]
		private ITable CreateTable([NotNull] string name,
		                           [NotNull] string urlFieldName)
		{
			return DatasetUtils.CreateTable(_testWs, name,
			                                FieldUtils.CreateOIDField(),
			                                FieldUtils.CreateTextField(urlFieldName, 500));
		}

		#region learning tests

		[Test]
		[Ignore("TODO reason")]
		public void LearningTestFileExists()
		{
			const string path = @"C:\temp\_test.txt";
			Assert.IsTrue(File.Exists(path));

			var uri = new Uri(path);
			Assert.IsTrue(File.Exists(uri.LocalPath));

			const string uncFilePath = @"\\sital\C$\temp\_test.txt";

			Assert.IsTrue(File.Exists(uncFilePath));

			uri = new Uri(uncFilePath);
			Assert.IsTrue(File.Exists(uri.LocalPath));

			// IsFalse()!
			const string filePath = @"file:///" + path;

			Assert.IsFalse(File.Exists(filePath));

			WebRequest request = WebRequest.Create(filePath);
			using (var fileWebResponse = (FileWebResponse) request.GetResponse())
			{
				Assert.IsNotNull(fileWebResponse);
				Assert.IsTrue(File.Exists(fileWebResponse.ResponseUri.LocalPath));
			}

			uri = new Uri(filePath);
			Assert.IsTrue(File.Exists(uri.LocalPath));
		}

		[Test]
		[Ignore("TODO reason")]
		public void LearningTestRequestForFileSystemPaths()
		{
			AssertRequest(@"\\nas-zh-01\SITE_ZH_ONLY$");
			AssertRequest(@"L:\local-zh");
		}

		[Test]
		[Ignore("TODO reason")]
		public void LearningWebServer()
		{
			// https://www.codehosting.net/blog/BlogEngine/post/Simple-C-Web-Server.aspx
			// http://blogs.msdn.com/b/youssefm/archive/2013/01/28/writing-tests-for-an-asp-net-webapi-service.aspx
			// http://www.codeproject.com/Articles/137979/Simple-HTTP-Server-in-C

			const string url = "http://localhost:8080/test/";
			using (var webServer = new WebServer(SendHeadResponse, url))
			{
				webServer.Run();

				var webRequest = (HttpWebRequest) WebRequest.Create(url);
				webRequest.Method = "HEAD";
				HttpWebResponse response = null;
				try
				{
					response = (HttpWebResponse) webRequest.GetResponse();
					Console.WriteLine(response.StatusCode);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
				}
				finally
				{
					if (response != null)
					{
						response.Close();
					}
				}
			}
		}

		private static void AssertRequest([NotNull] string uri)
		{
			WebRequest request = WebRequest.Create(uri);

			Console.WriteLine(@"Request for '{0}': [{1}]", uri, request);

			Assert.IsNotNull(request);
		}

		#endregion

		#region web server

		private string SendHeadResponse([NotNull] HttpListenerRequest httpListenerRequest)
		{
			// Counts web requests. Important for testing only requests of distinct urls:
			_webRequestCounts++;

			if (_sleepMilliseconds > 0)
			{
				Thread.Sleep(_sleepMilliseconds);
			}

			Console.WriteLine(@"Method: {0} Url: {1}",
			                  httpListenerRequest.HttpMethod,
			                  httpListenerRequest.Url);

			return "<head><title>Esri Schweiz | Home</title><head>";
		}

		#endregion
	}
}
