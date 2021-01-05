using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Html;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.VerificationReports.Xml;
using ProSuite.DomainServices.AO.Xml;

namespace ProSuite.DomainServices.AO.QA.HtmlReports
{
	[CLSCompliant(false)]
	public static class HtmlReportUtils
	{
		[NotNull]
		public static HtmlReportAreaOfInterest GetAreaOfInterest(
			[NotNull] XmlAreaOfInterest xmlAreaOfInterest)
		{
			Assert.ArgumentNotNull(xmlAreaOfInterest, nameof(xmlAreaOfInterest));

			return new HtmlReportAreaOfInterest(GetAreaOfInterestType(xmlAreaOfInterest.Type))
			       {
				       Description = xmlAreaOfInterest.Description,
				       FeatureSource = xmlAreaOfInterest.FeatureSource,
				       WhereClause = xmlAreaOfInterest.WhereClause,
				       BufferDistance = xmlAreaOfInterest.BufferDistance,
				       GeneralizationTolerance = xmlAreaOfInterest.GeneralizationTolerance,
				       ExtentString = xmlAreaOfInterest.Extent == null
					                      ? null
					                      : FormatExtent(xmlAreaOfInterest.Extent),
				       UsesClipExtent = xmlAreaOfInterest.ClipExtent != null,
				       ClipExtentString = xmlAreaOfInterest.ClipExtent == null
					                          ? null
					                          : FormatExtent(xmlAreaOfInterest.ClipExtent)
			       };
		}

		[NotNull]
		public static string FormatExtent([NotNull] Xml2DEnvelope extent)
		{
			Assert.ArgumentNotNull(extent, nameof(extent));

			bool round = extent.XMax > 10000 || extent.YMax > 10000;

			string format = round
				                ? "X-Min: {0:N2} Y-Min: {1:N2} X-Max: {2:N2} Y-Max: {3:N2}"
				                : "X-Min: {0} Y-Min: {1} X-Max: {2} Y-Max: {3}";

			string extentString = string.Format(format,
			                                    extent.XMin, extent.YMin,
			                                    extent.XMax, extent.YMax);

			return StringUtils.IsNotEmpty(extent.CoordinateSystem)
				       ? string.Format("{0} ({1})", extentString, extent.CoordinateSystem)
				       : extentString;
		}

		[NotNull]
		public static string GetRelativeUrl([NotNull] string fileName)
		{
			Assert.ArgumentNotNull(fileName, nameof(fileName)); // empty is allowed

			const string relativeUrlFormat = "./{0}";
			return string.Format(relativeUrlFormat, fileName);
		}

		[NotNull]
		public static string Format(int value)
		{
			return string.Format("{0:N0}", value);
		}

		[NotNull]
		public static string FormatTimeSpan(TimeSpan timeSpan)
		{
			return string.Format("{0:00}h:{1:00}m:{2:00}s",
			                     Math.Truncate(timeSpan.TotalHours),
			                     timeSpan.Minutes,
			                     timeSpan.Seconds);
		}

		[NotNull]
		public static string FormatGeometryType(esriGeometryType geometryType,
		                                        bool hasZ,
		                                        bool hasM)
		{
			string baseName = FormatGeometryType(geometryType);

			string suffix = string.Format("{0}{1}",
			                              hasZ
				                              ? "Z"
				                              : string.Empty,
			                              hasM
				                              ? "M"
				                              : string.Empty);

			return string.IsNullOrEmpty(suffix)
				       ? baseName
				       : string.Format("{0} {1}", baseName, suffix);
		}

		[NotNull]
		public static List<HtmlReportDataQualityCategory> GroupByCategories(
			[NotNull] IIssueStatistics issueStatistics,
			[NotNull] HtmlReportDataQualityCategoryComparer categoryComparer,
			[NotNull] HtmlReportQualityConditionComparer qualityConditionComparer,
			[NotNull] HtmlReportIssueGroupComparer issueGroupComparer,
			[CanBeNull] Func<IssueGroup, string> getTestIdentifier,
			[CanBeNull] IHtmlDataQualityCategoryOptionsProvider optionsProvider,
			[NotNull] out List<HtmlReportIssueGroup> htmlReportIssueGroups)
		{
			List<IssueGroup> issueGroupList = issueStatistics.GetIssueGroups().ToList();

			IDictionary<string, HtmlReportDataQualityCategory> reportCategories =
				MapReportCategories(issueGroupList,
				                    issueStatistics.ExceptionCategories,
				                    categoryComparer,
				                    qualityConditionComparer,
				                    issueGroupComparer,
				                    optionsProvider);

			int maximumIssueCount = GetMaximumIssueCount(issueGroupList);

			htmlReportIssueGroups = new List<HtmlReportIssueGroup>();

			foreach (IssueGroup issueGroup in issueGroupList)
			{
				string testIdentifier = getTestIdentifier != null
					                        ? getTestIdentifier(issueGroup)
					                        : issueGroup.QualityCondition.TestDescriptor.Name;

				var reportIssueGroup = new HtmlReportIssueGroup(issueGroup,
				                                                maximumIssueCount,
				                                                testIdentifier,
				                                                GetHtmlExceptionCategories(
					                                                issueStatistics, issueGroup));

				HtmlReportDataQualityCategory reportCategory =
					reportCategories[GetCategoryKey(issueGroup.QualityCondition.Category)];

				reportCategory.AddIssueGroup(reportIssueGroup, issueGroup.IssueCount);
				htmlReportIssueGroups.Add(reportIssueGroup);
			}

			htmlReportIssueGroups.Sort(issueGroupComparer);

			// exclude undefined root category if it does not contain any quality conditions

			return reportCategories.Values
			                       .Where(cat => ! cat.IsRoot ||
			                                     ! cat.IsUndefinedCategory ||
			                                     cat.QualityConditions.Count > 0)
			                       .Distinct()
			                       .OrderBy(c => c, categoryComparer)
			                       .ToList();
		}

		[NotNull]
		public static string GetCompleteUrl([NotNull] string url)
		{
			Assert.ArgumentNotNullOrEmpty(url, nameof(url));

			return url.IndexOf("://", StringComparison.Ordinal) > 0
				       ? url
				       : $"http://{url}";
		}

		[NotNull]
		public static string GetQualifiedText(
			[NotNull] HtmlReportDataQualityCategory category,
			[NotNull] Func<HtmlReportDataQualityCategory, string> getText,
			[CanBeNull] string separator = "/",
			bool skipNullOrEmpty = false)
		{
			Assert.ArgumentNotNull(category, nameof(category));

			var strings = new List<string>();

			CollectStrings(strings, category, getText, skipNullOrEmpty);

			return string.Join(separator, strings.ToArray());
		}

		[NotNull]
		public static List<HtmlExceptionCategoryCount> AggregateExceptionCategoryCounts(
			[NotNull] IEnumerable<HtmlExceptionCategoryCount> htmlExceptionCategoryCounts,
			[NotNull] IEnumerable<ExceptionCategory> allExceptionCategories)
		{
			Assert.ArgumentNotNull(htmlExceptionCategoryCounts,
			                       nameof(htmlExceptionCategoryCounts));
			Assert.ArgumentNotNull(allExceptionCategories, nameof(allExceptionCategories));

			var exceptionCategories = allExceptionCategories.ToList();

			var exceptionCategorySet = new HashSet<ExceptionCategory>(exceptionCategories);

			var totalByCategory = new Dictionary<ExceptionCategory, int>();

			foreach (HtmlExceptionCategoryCount categoryCount in htmlExceptionCategoryCounts)
			{
				var exceptionCategory = categoryCount.Category;

				Assert.True(exceptionCategorySet.Contains(exceptionCategory),
				            "unexpected exception category: {0}",
				            exceptionCategory);

				int count;
				count = totalByCategory.TryGetValue(exceptionCategory, out count)
					        ? count + categoryCount.ExceptionCount
					        : categoryCount.ExceptionCount;

				totalByCategory[exceptionCategory] = count;
			}

			var result = new List<HtmlExceptionCategoryCount>();

			foreach (ExceptionCategory category in exceptionCategories)
			{
				int count;
				if (! totalByCategory.TryGetValue(category, out count))
				{
					count = 0;
				}

				result.Add(new HtmlExceptionCategoryCount(category, count));
			}

			return result;
		}

		[NotNull]
		private static IEnumerable<HtmlExceptionCategoryCount> GetHtmlExceptionCategories(
			[NotNull] IIssueStatistics issueStatistics,
			[NotNull] IssueGroup issueGroup)
		{
			return issueStatistics.ExceptionCategories.Select(
				category => new HtmlExceptionCategoryCount(
					category,
					issueGroup.GetExceptionCount(category)));
		}

		[NotNull]
		private static string GetAreaOfInterestType(AreaOfInterestType type)
		{
			switch (type)
			{
				case AreaOfInterestType.None:
					return "No area of interest";

				case AreaOfInterestType.Box:
					return "Box";

				case AreaOfInterestType.Polygon:
					return "Polygon";

				case AreaOfInterestType.Empty:
					return "Empty area of interest";

				default:
					throw new ArgumentOutOfRangeException(nameof(type));
			}
		}

		private static int GetMaximumIssueCount(
			[NotNull] IEnumerable<IssueGroup> issueGroups)
		{
			return issueGroups.Select(g => g.IssueCount)
			                  .DefaultIfEmpty(0)
			                  .Max();
		}

		[NotNull]
		private static Dictionary<string, HtmlReportDataQualityCategory> MapReportCategories
		([NotNull] IEnumerable<IssueGroup> issueGroups,
		 [NotNull] IList<ExceptionCategory> exceptionCategories,
		 [NotNull] HtmlReportDataQualityCategoryComparer categoryComparer,
		 [NotNull] HtmlReportQualityConditionComparer qualityConditionComparer,
		 [NotNull] HtmlReportIssueGroupComparer issueGroupComparer,
		 [CanBeNull] IHtmlDataQualityCategoryOptionsProvider optionsProvider = null)
		{
			var result = new Dictionary<string, HtmlReportDataQualityCategory>();

			foreach (IssueGroup issueGroup in issueGroups)
			{
				// add the next non-ignored category
				AddReportCategory(issueGroup.QualityCondition.Category,
				                  exceptionCategories,
				                  categoryComparer,
				                  qualityConditionComparer,
				                  issueGroupComparer,
				                  result,
				                  optionsProvider);
			}

			return result;
		}

		[NotNull]
		private static HtmlReportDataQualityCategory AddReportCategory(
			[CanBeNull] DataQualityCategory category,
			[NotNull] IList<ExceptionCategory> exceptionCategories,
			[NotNull] HtmlReportDataQualityCategoryComparer categoryComparer,
			[NotNull] HtmlReportQualityConditionComparer qualityConditionComparer,
			[NotNull] HtmlReportIssueGroupComparer issueGroupComparer,
			[NotNull] IDictionary<string, HtmlReportDataQualityCategory> reportCategories,
			[CanBeNull] IHtmlDataQualityCategoryOptionsProvider optionsProvider = null)
		{
			string key = GetCategoryKey(category);

			HtmlReportDataQualityCategory result;
			if (reportCategories.TryGetValue(key, out result))
			{
				// already added (including parents)
				return result;
			}

			HtmlDataQualityCategoryOptions options =
				GetReportCategoryOptions(optionsProvider, category);

			if (category != null && options != null && options.IgnoreCategoryLevel)
			{
				// skip this category level
				result = AddReportCategory(category.ParentCategory,
				                           exceptionCategories,
				                           categoryComparer,
				                           qualityConditionComparer,
				                           issueGroupComparer,
				                           reportCategories,
				                           optionsProvider);

				reportCategories.Add(key, result);
				return result;
			}

			result = new HtmlReportDataQualityCategory(category,
			                                           exceptionCategories,
			                                           options,
			                                           categoryComparer,
			                                           qualityConditionComparer,
			                                           issueGroupComparer);
			reportCategories.Add(key, result);

			if (category?.ParentCategory != null)
			{
				HtmlReportDataQualityCategory parent = AddReportCategory(category.ParentCategory,
				                                                         exceptionCategories,
				                                                         categoryComparer,
				                                                         qualityConditionComparer,
				                                                         issueGroupComparer,
				                                                         reportCategories,
				                                                         optionsProvider);
				if (! parent.IsUndefinedCategory)
				{
					result.ParentCategory = parent;
					result.ParentCategory.IncludeSubCategory(result);
				}
			}

			return result;
		}

		[NotNull]
		private static string GetCategoryKey([CanBeNull] DataQualityCategory category)
		{
			return category?.Uuid ?? string.Empty;
		}

		[CanBeNull]
		private static HtmlDataQualityCategoryOptions GetReportCategoryOptions(
			[CanBeNull] IHtmlDataQualityCategoryOptionsProvider categoryOptionsProvider,
			[CanBeNull] DataQualityCategory category)
		{
			return category == null
				       ? null
				       : categoryOptionsProvider?.GetCategoryOptions(category.Uuid);
		}

		private static string FormatGeometryType(esriGeometryType geometryType)
		{
			switch (geometryType)
			{
				case esriGeometryType.esriGeometryPoint:
					return "Point";

				case esriGeometryType.esriGeometryMultipoint:
					return "Multipoint";

				case esriGeometryType.esriGeometryPolyline:
					return "Polyline";

				case esriGeometryType.esriGeometryPolygon:
					return "Polygon";

				case esriGeometryType.esriGeometryMultiPatch:
					return "MultiPatch";

				case esriGeometryType.esriGeometryNull:
				case esriGeometryType.esriGeometryLine:
				case esriGeometryType.esriGeometryCircularArc:
				case esriGeometryType.esriGeometryEllipticArc:
				case esriGeometryType.esriGeometryBezier3Curve:
				case esriGeometryType.esriGeometryPath:
				case esriGeometryType.esriGeometryRing:
				case esriGeometryType.esriGeometryEnvelope:
				case esriGeometryType.esriGeometryAny:
				case esriGeometryType.esriGeometryBag:
				case esriGeometryType.esriGeometryTriangleStrip:
				case esriGeometryType.esriGeometryTriangleFan:
				case esriGeometryType.esriGeometryRay:
				case esriGeometryType.esriGeometrySphere:
				case esriGeometryType.esriGeometryTriangles:
					throw new ArgumentOutOfRangeException(nameof(geometryType), geometryType,
					                                      @"Must be a high-level geometry type");

				default:
					throw new ArgumentOutOfRangeException(nameof(geometryType), geometryType,
					                                      @"Unknown geometry type");
			}
		}

		private static void CollectStrings(
			[NotNull] ICollection<string> strings,
			[NotNull] HtmlReportDataQualityCategory category,
			[NotNull] Func<HtmlReportDataQualityCategory, string> getString,
			bool skipNullOrEmpty)
		{
			if (category.ParentCategory != null)
			{
				CollectStrings(strings, category.ParentCategory, getString, skipNullOrEmpty);
			}

			string value = getString(category);
			if (! skipNullOrEmpty || ! string.IsNullOrEmpty(value))
			{
				strings.Add(getString(category));
			}
		}
	}
}
