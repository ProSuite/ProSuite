using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[AttributeTest]
	[UsedImplicitly]
	public class QaValidUrls : ContainerTest
	{
		[NotNull] private readonly IReadOnlyTable _table;
		[NotNull] private readonly string _urlExpression;

		/// <summary>
		/// index of the single field specified in the url expression 
		/// (set to a valid field index only if expression corresponds to one field name)
		/// </summary>
		private readonly int _fieldIndex = -1;

		/// <summary>
		/// Indicates if the url expression corresponds to a single field name
		/// </summary>
		private readonly bool _isSingleField;

		/// <summary>
		/// Field expression resulting in a string value. Defined if url expression does
		/// not correspond to a single field name.
		/// </summary>
		[CanBeNull] private StringFieldExpression _fieldExpression;

		/// <summary>
		/// may be null if more than one field is used in expression
		/// </summary>
		[CanBeNull] private readonly string _affectedComponent;

		/// <summary>
		/// The status for already checked urls. If no error, the value is null.
		/// </summary>
		/// <remarks>don't ignore case differences in urls, as http servers may be case-sensitive</remarks>
		[NotNull] private readonly IDictionary<string, ErrorInfo> _knownUrlStatus =
			new Dictionary<string, ErrorInfo>(StringComparer.Ordinal);

		private int _maximumParallelTasks;
		private int? _maximumThreadCount;

		private const int _defaultMaximumParallelTasks = 1;
		[CanBeNull] private List<RowUrl> _rowUrls;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string InvalidUrlFormat = "UnsupportedUrlFormat";

			public const string UnsupportedUrlType = "UnsupportedUrlType";

			public const string CannotAccessResource_FileSystem_PathDoesNotExist =
				"CannotAccessResource.FileSystem.PathDoesNotExist";

			public const string CannotAccessResource_FileSystem_NoReadAccess =
				"CannotAccessResource.FileSystem.NoReadAccess";

			public const string CannotAccessResource_Web_InvalidResponse =
				"CannotAccessResource.Web.InvalidResponse";

			public const string CannotAccessResource_Web_ErrorGettingResponse =
				"CannotAccessResource.Web.ErrorGettingResponse";

			public Code() : base("ValidUrls") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaValidUrls_0))]
		public QaValidUrls(
			[Doc(nameof(DocStrings.QaValidUrls_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaValidUrls_urlExpression))] [NotNull]
			string urlExpression)
			: base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(urlExpression, nameof(urlExpression));

			_table = table;
			_urlExpression = urlExpression;

			AddCustomQueryFilterExpression(urlExpression);

			_maximumParallelTasks = _defaultMaximumParallelTasks;

			// if the url expression is a single field, then direclty read from the field
			// (by the field index determined here)
			// Use the infrastructure for expression evaluation only if the url expression 
			// is really a sql expression involving more than one field name 
			// and/or functions, literals etc.,
			List<string> tokens = ExpressionUtils.GetExpressionTokens(urlExpression).ToList();

			_isSingleField = false;
			if (tokens.Count == 1)
			{
				int fieldIndex = table.FindField(urlExpression);

				if (fieldIndex < 0)
				{
					throw new ArgumentException(
						string.Format("Field not found in table {0}: {1}",
						              table.Name,
						              urlExpression), nameof(urlExpression));
				}

				// the url expression is a single field name
				_fieldIndex = fieldIndex;
				_isSingleField = true;
				_affectedComponent = urlExpression.ToUpper().Trim();
			}

			if (! _isSingleField)
			{
				List<string> fieldNames = ExpressionUtils.GetExpressionFieldNames(
					_table, _urlExpression).ToList();

				if (fieldNames.Count == 0)
				{
					throw new ArgumentException(
						$@"Invalid expression: {urlExpression}",
						nameof(urlExpression));
				}

				if (fieldNames.Count == 1)
				{
					_affectedComponent = fieldNames[0].ToUpper().Trim();
				}
			}
		}

		[InternallyUsedTest]
		public QaValidUrls([NotNull] QaValidUrlsDefinition definition)
			: this((IReadOnlyTable) definition.Table, definition.UrlExpression)
		{
			MaximumParallelTasks = definition.MaximumParallelTasks;
		}

		[TestParameter(_defaultMaximumParallelTasks)]
		[Doc(nameof(DocStrings.QaValidUrls_MaximumParallelTasks))]
		public int MaximumParallelTasks
		{
			get { return _maximumParallelTasks; }
			set
			{
				Assert.ArgumentCondition(value > 0, "Invalid value (must be > 0): {0}", value);

				_maximumParallelTasks = value;
			}
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			string url = GetUrl(row);

			if (url == null || StringUtils.IsNullOrEmptyOrBlank(url))
			{
				return NoError;
			}

			string trimmedUrl = url.Trim();

			if (_maximumParallelTasks > 1)
			{
				if (_maximumThreadCount == null)
				{
					// ProcessorCount indicates the *virtual* cores.
					// No need to subtract 1 for the main thread, since that thread will anyway
					// be waiting while the checker threads are at work.
					_maximumThreadCount = Math.Min(_maximumParallelTasks,
					                               Environment.ProcessorCount);
				}
			}

			ErrorInfo errorInfo;
			if (_knownUrlStatus.TryGetValue(trimmedUrl, out errorInfo))
			{
				return errorInfo == null
					       ? NoError
					       : ReportError(errorInfo, row);
			}

			if (_maximumThreadCount == null || _maximumThreadCount.Value < 2)
			{
				errorInfo = CheckUrl(trimmedUrl);

				_knownUrlStatus.Add(trimmedUrl, errorInfo);

				return errorInfo == null
					       ? NoError
					       : ReportError(errorInfo, row);
			}

			if (_rowUrls == null)
			{
				_rowUrls = new List<RowUrl>();
			}

			_rowUrls.Add(new RowUrl(trimmedUrl, row.OID));

			return NoError;
		}

		protected override int CompleteTileCore(TileInfo tileInfo)
		{
			if (_rowUrls == null)
			{
				return NoError;
			}

			Assert.True(_maximumThreadCount > 1,
			            "Unexpected maximum thread count: [{0}]", _maximumThreadCount);

			try
			{
				ICollection<RowErrorInfo> errorRows = CheckParallel(_rowUrls,
					_maximumThreadCount.Value,
					_knownUrlStatus);

				return ReportErrors(errorRows);
			}
			finally
			{
				_rowUrls = null;
			}
		}

		private int ReportErrors([NotNull] ICollection<RowErrorInfo> errorRows)
		{
			Assert.ArgumentNotNull(errorRows);

			var oids = new List<long>(errorRows.Count);
			var errorsByOid = new Dictionary<long, ErrorInfo>(errorRows.Count);

			foreach (RowErrorInfo errorRow in errorRows)
			{
				oids.Add(errorRow.RowUrl.Oid);
				errorsByOid.Add(errorRow.RowUrl.Oid, errorRow.ErrorInfo);
			}

			if (oids.Count == 0)
			{
				return NoError;
			}

			var errorCount = 0;

			const bool recycling = false;
			foreach (IReadOnlyRow row in TableFilterUtils.GetRows(_table, oids, recycling))
			{
				errorCount += ReportError(errorsByOid[row.OID], row);
			}

			return errorCount;
		}

		[NotNull]
		private static ICollection<RowErrorInfo> CheckParallel(
			[NotNull] IEnumerable<RowUrl> rowUrls,
			int threadCount,
			[NotNull] IDictionary<string, ErrorInfo> knownUrlStatus)
		{
			var urls = new HashSet<string>();
			var urlRows = new Dictionary<string, List<long>>();

			foreach (RowUrl rowUrl in rowUrls)
			{
				urls.Add(rowUrl.Url);

				List<long> oids;
				if (! urlRows.TryGetValue(rowUrl.Url, out oids))
				{
					oids = new List<long>();
					urlRows.Add(rowUrl.Url, oids);
				}

				oids.Add(rowUrl.Oid);
			}

			List<UrlError> urlErrors = CheckParallel(urls, threadCount);

			foreach (string url in urls)
			{
				knownUrlStatus.Add(url, null);
			}

			var result = new List<RowErrorInfo>(urlErrors.Count);

			foreach (UrlError urlError in urlErrors)
			{
				knownUrlStatus[urlError.Url] = urlError.ErrorInfo;

				List<long> oids = urlRows[urlError.Url];

				foreach (long oid in oids)
				{
					result.Add(new RowErrorInfo(new RowUrl(urlError.Url, oid), urlError.ErrorInfo));
				}
			}

			return result;
		}

		[NotNull]
		private static List<UrlError> CheckParallel([NotNull] IEnumerable<string> urls,
		                                            int threadCount)
		{
			Assert.ArgumentNotNull(urls, nameof(urls));
			Assert.ArgumentCondition(threadCount > 0, "Unexpected thread count: {0}",
			                         threadCount);

			var checkers = new List<UrlChecker>(threadCount);
			checkers.AddRange(CollectionUtils.Partition(urls, threadCount)
			                                 .Select(chunk => new UrlChecker(chunk)));

			SecurityProtocolType securityProtocols = ServicePointManager.SecurityProtocol;

			try
			{
				// try setting value corresponding to: Ssl3 | Tls | Tls11 | Tls12
				// Ssl: 48
				// Tls: 192
				// Tls11: 768
				// Tls12: 3072

				// Note: Tls11 and Tls12 require either .Net 4.5, or the installation of TLS support package for .Net 3.5.1 
				// (https://support.microsoft.com/en-us/help/3154518/support-for-tls-system-default-versions-included-in-the--net-framework)
				// Note: enum values Tls11 and Tls12 are not defined in .Net 3.5, so try setting the integer value
				ServicePointManager.SecurityProtocol = (SecurityProtocolType) 4080;
			}
			catch (Exception)
			{
				// shouldn't have been changed, but to be sure: reset value
				ServicePointManager.SecurityProtocol = securityProtocols;
			}

			try
			{
				CheckParallel(checkers);
			}
			finally
			{
				ServicePointManager.SecurityProtocol = securityProtocols;
			}

			var result = new List<UrlError>();

			foreach (UrlChecker checker in checkers)
			{
				result.AddRange(checker.Result);
			}

			return result;
		}

		private static void CheckParallel([NotNull] ICollection<UrlChecker> checkers)
		{
			Assert.ArgumentNotNull(checkers, nameof(checkers));

			var threads = new List<Thread>(checkers.Count);

			// using the thread pool seems to be significantly slower (~30% in unit test)
			// (additional controller thread needed since WaitHandle.WaitAll() is not supported on STA thread)
			foreach (UrlChecker checker in checkers)
			{
				var thread = new Thread(checker.Execute);
				threads.Add(thread);

				thread.Start();
			}

			foreach (Thread thread in threads)
			{
				thread.Join();
			}
		}

		private int ReportError([NotNull] ErrorInfo errorInfo, [NotNull] IReadOnlyRow row)
		{
			return ReportError(
				errorInfo.Description, InvolvedRowUtils.GetInvolvedRows(row),
				TestUtils.GetShapeCopy(row), errorInfo.IssueCode, _affectedComponent);
		}

		[CanBeNull]
		private static ErrorInfo CheckUrl([NotNull] string url)
		{
			WebRequest request;
			try
			{
				request = CreateRequest(url);
			}
			catch (UriFormatException)
			{
				return new ErrorInfo($"Invalid url format: {url}", Codes[Code.InvalidUrlFormat]);
			}

			RequestHandler requestHandler = GetRequestHandler(request);

			return requestHandler == null
				       ? new ErrorInfo("Unsupported url type", Codes[Code.UnsupportedUrlType])
				       : requestHandler.CheckRequest();
		}

		[NotNull]
		private static WebRequest CreateRequest([NotNull] string url)
		{
			const string wwwPrefix = "www.";
			const string httpScheme = "http";
			const string schemeSeparator = "://";

			string completedUrl =
				url.StartsWith(wwwPrefix, StringComparison.OrdinalIgnoreCase) &&
				url.IndexOf(schemeSeparator, StringComparison.OrdinalIgnoreCase) < 0
					? $"{httpScheme}{schemeSeparator}{url}"
					: url;

			return WebRequest.Create(completedUrl);
		}

		[CanBeNull]
		private static RequestHandler GetRequestHandler([NotNull] WebRequest request)
		{
			var httpWebRequest = request as HttpWebRequest;
			if (httpWebRequest != null)
			{
				return new HttpRequestHandler(httpWebRequest);
			}

			var fileWebRequest = request as FileWebRequest;
			if (fileWebRequest != null)
			{
				return new FileRequestHandler(fileWebRequest);
			}

			// NOTE: add handler for FtpWebRequest when requested by users

			return null;
		}

		[CanBeNull]
		private string GetUrl([NotNull] IReadOnlyRow row)
		{
			if (_isSingleField)
			{
				object url = row.get_Value(_fieldIndex);

				return url is DBNull
					       ? null
					       : url as string;
			}

			if (_fieldExpression == null)
			{
				_fieldExpression = new StringFieldExpression(
					_table, _urlExpression,
					caseSensitive: GetSqlCaseSensitivity());
			}

			return _fieldExpression.GetString(row);
		}

		/// <summary>
		/// Helper to hold error info for a checked uri
		/// </summary>
		private class ErrorInfo
		{
			public ErrorInfo([NotNull] string description, [CanBeNull] IssueCode issueCode)
			{
				Description = description;
				IssueCode = issueCode;
			}

			[NotNull]
			public string Description { get; }

			[CanBeNull]
			public IssueCode IssueCode { get; }
		}

		private abstract class RequestHandler
		{
			[CanBeNull]
			public abstract ErrorInfo CheckRequest();
		}

		private class HttpRequestHandler : RequestHandler
		{
			[NotNull] private readonly HttpWebRequest _httpRequest;

			public HttpRequestHandler([NotNull] HttpWebRequest httpRequest)
			{
				Assert.ArgumentNotNull(httpRequest, nameof(httpRequest));

				_httpRequest = httpRequest;
			}

			public override ErrorInfo CheckRequest()
			{
				_httpRequest.Method = "HEAD";

				HttpWebResponse response;
				try
				{
					response = (HttpWebResponse) _httpRequest.GetResponse();
				}
				catch (Exception e)
				{
					string description = string.Format("Error getting response for url '{0}': {1}",
					                                   _httpRequest.RequestUri,
					                                   e.Message);

					return new ErrorInfo(description,
					                     Codes[Code.CannotAccessResource_Web_ErrorGettingResponse]);
				}

				try
				{
					HttpStatusCode statusCode = response.StatusCode;
					if ((int) statusCode < 400)
					{
						return null;
					}

					string description = $"Invalid response: {response.StatusDescription}";
					return new ErrorInfo(description,
					                     Codes[Code.CannotAccessResource_Web_InvalidResponse]);
				}
				finally
				{
					response.Close();
				}
			}
		}

		private class FileRequestHandler : RequestHandler
		{
			[NotNull] private readonly FileWebRequest _request;

			public FileRequestHandler([NotNull] FileWebRequest request)
			{
				Assert.ArgumentNotNull(request, nameof(request));

				_request = request;
			}

			public override ErrorInfo CheckRequest()
			{
				Uri uri = _request.RequestUri;

				string localPath = uri.LocalPath;
				Assert.NotNullOrEmpty(localPath, "local path not defined for uri {0}", uri);

				if (File.Exists(localPath))
				{
					//check if the file can be read
					string message;
					if (CanReadFile(localPath, out message))
					{
						return null;
					}

					return new ErrorInfo($"Cannot read file {localPath}: {message}",
					                     Codes[Code.CannotAccessResource_FileSystem_NoReadAccess]);
				}

				if (Directory.Exists(localPath))
				{
					// Possible enhancement: check if the directory content can be listed
					return null;
				}

				// the local path exists neither as a file nor as a directory
				return new ErrorInfo(
					$"Path does not exist: {localPath}",
					Codes[Code.CannotAccessResource_FileSystem_PathDoesNotExist]);
			}

			private static bool CanReadFile([NotNull] string localPath,
			                                [NotNull] out string message)
			{
				FileStream stream = null;
				try
				{
					stream = File.Open(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);

					message = string.Empty;
					return true;
				}
				catch (Exception e)
				{
					message = e.Message;
					return false;
				}
				finally
				{
					stream?.Dispose();
				}
			}

			//public override ErrorInfo CheckRequest()
			//{
			//    FileWebResponse response;
			//    try
			//    {
			//        response = (FileWebResponse) _fileWebRequest.GetResponse();
			//    }
			//    catch (Exception e)
			//    {
			//        string description = string.Format("Error getting response for url '{0}': {1}",
			//                                           _fileWebRequest.RequestUri, e.Message);

			//        return new ErrorInfo(description, Codes[Code.ErrorGettingResponse]);
			//    }
			//    try
			//    {
			//        return null;
			//    }
			//    finally
			//    {
			//        if (response != null)
			//        {
			//            response.Close();
			//        }
			//    }
			//}
		}

		private class RowUrl
		{
			public RowUrl([NotNull] string url, long oid)
			{
				Url = url;
				Oid = oid;
			}

			[NotNull]
			public string Url { get; }

			public long Oid { get; }
		}

		private class RowErrorInfo
		{
			public RowErrorInfo([NotNull] RowUrl rowUrl, [NotNull] ErrorInfo errorInfo)
			{
				RowUrl = rowUrl;
				ErrorInfo = errorInfo;
			}

			[NotNull]
			public RowUrl RowUrl { get; }

			[NotNull]
			public ErrorInfo ErrorInfo { get; }
		}

		private class UrlError
		{
			public UrlError([NotNull] string url, [NotNull] ErrorInfo errorInfo)
			{
				Url = url;
				ErrorInfo = errorInfo;
			}

			[NotNull]
			public string Url { get; }

			[NotNull]
			public ErrorInfo ErrorInfo { get; }
		}

		private class UrlChecker
		{
			private readonly ICollection<string> _urls;
			private readonly List<UrlError> _result = new List<UrlError>();

			public UrlChecker([NotNull] ICollection<string> urls)
			{
				Assert.ArgumentNotNull(urls, nameof(urls));

				_urls = urls;
			}

			public void Execute()
			{
				foreach (string url in _urls)
				{
					ErrorInfo errorInfo = CheckUrl(url);
					if (errorInfo != null)
					{
						_result.Add(new UrlError(url, errorInfo));
					}
				}
			}

			[NotNull]
			public IEnumerable<UrlError> Result => _result;
		}
	}
}
