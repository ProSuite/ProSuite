using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public static class IssueDatasetUtils
	{
		private const string _polygonClassName = "IssuePolygons";
		private const string _polylineClassName = "IssueLines";
		private const string _multipointClassName = "IssuePoints";
		private const string _multiPatchClassName = "IssueMultiPatches";
		private const string _rowClassName = "IssueRows";

		[NotNull] private static readonly List<string> _featureClassNames = new List<string>
			{
				_polygonClassName,
				_polylineClassName,
				_multipointClassName,
				_multiPatchClassName
			};

		[NotNull]
		public static string PolygonClassName => _polygonClassName;

		[NotNull]
		public static string PolylineClassName => _polylineClassName;

		[NotNull]
		public static string MultipointClassName => _multipointClassName;

		[NotNull]
		public static string MultiPatchClassName => _multiPatchClassName;

		[NotNull]
		public static string RowClassName => _rowClassName;

		[NotNull]
		public static IEnumerable<string> FeatureClassNames => _featureClassNames;

		[NotNull]
		public static IEnumerable<string> ObjectClassNames
		{
			get
			{
				foreach (string featureClassName in _featureClassNames)
				{
					yield return featureClassName;
				}

				yield return _rowClassName;
			}
		}
	}
}
