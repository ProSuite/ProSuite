using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Html;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.HtmlReports;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Tests;

namespace ProSuite.DomainServices.AO.Test.QA.HtmlReports
{
	[TestFixture]
	public class HtmlReportUtilsTest
	{
		[Test]
		public void CanGroupQualityConditionsWithoutExclusion()
		{
			var catA = new DataQualityCategory("A");
			var catA1 = new DataQualityCategory("A1");
			var catA11 = new DataQualityCategory("A11");
			var catB = new DataQualityCategory("B");
			var catB1 = new DataQualityCategory("B1");
			var catB11 = new DataQualityCategory("B11");
			var catB111 = new DataQualityCategory("B111");

			catA.AddSubCategory(catA1);
			catA1.AddSubCategory(catA11);
			catB.AddSubCategory(catB1);
			catB1.AddSubCategory(catB11);
			catB11.AddSubCategory(catB111);

			var test = new TestDescriptor("test", new ClassDescriptor(typeof(QaMinSegAngle)));

			var qc1 = new QualityCondition("qc1", test) {Category = catA11};
			var qc2 = new QualityCondition("qc2", test) {Category = catB11};
			var qc3 = new QualityCondition("qc3", test) {Category = catB11};

			var issueGroups = new List<IssueGroup>
			                  {
				                  CreateIssueGroup(qc1, "ic1", 3),
				                  CreateIssueGroup(qc2, "ic2", 4),
				                  CreateIssueGroup(qc3, "ic3", 5),
				                  CreateIssueGroup(qc3, "ic4", 6)
			                  };

			List<HtmlReportIssueGroup> reportIssueGroups;
			List<HtmlReportDataQualityCategory> reportCategories =
				GroupByCategories(issueGroups, null, out reportIssueGroups);

			Assert.AreEqual(18, reportIssueGroups.Sum(ig => ig.IssueCount));

			Assert.AreEqual(6, reportCategories.Count);
			Assert.AreEqual(18, reportCategories.Sum(qc => qc.IssueCount));

			List<HtmlReportDataQualityCategory> rootCategories =
				reportCategories.Where(qc => qc.IsRoot)
				                .ToList();
			Assert.AreEqual(2, rootCategories.Count);
			Assert.AreEqual(18, rootCategories.Sum(qc => qc.IssueCountWithChildren));
		}

		[NotNull]
		private static List<HtmlReportDataQualityCategory> GroupByCategories(
			[NotNull] IEnumerable<IssueGroup> issueGroups,
			[CanBeNull] IHtmlDataQualityCategoryOptionsProvider optionsProvider,
			[NotNull] out List<HtmlReportIssueGroup> reportIssueGroups)
		{
			var statistics = new IssueStatistics(issueGroups);

			return HtmlReportUtils.GroupByCategories(
				statistics,
				new HtmlReportDataQualityCategoryComparer(),
				new HtmlReportQualityConditionComparer(),
				new HtmlReportIssueGroupComparer(),
				ig => "testidentifier",
				optionsProvider,
				out reportIssueGroups);
		}

		[Test]
		public void CanGroupQualityConditionsWithExclusion()
		{
			var catA = new DataQualityCategory("A");
			var catA1 = new DataQualityCategory("A1");
			var catA11 = new DataQualityCategory("A11");
			var catB = new DataQualityCategory("B");
			var catB1 = new DataQualityCategory("B1");
			var catB11 = new DataQualityCategory("B11");
			var catB111 = new DataQualityCategory("B111");

			catA.AddSubCategory(catA1);
			catA1.AddSubCategory(catA11);
			catB.AddSubCategory(catB1);
			catB1.AddSubCategory(catB11);
			catB11.AddSubCategory(catB111);

			var test = new TestDescriptor("test", new ClassDescriptor(typeof(QaMinSegAngle)));

			var qc1 = new QualityCondition("qc1", test) {Category = catA11};
			var qc2 = new QualityCondition("qc2", test) {Category = catB11};
			var qc3 = new QualityCondition("qc3", test) {Category = catB11};

			var issueGroups = new List<IssueGroup>
			                  {
				                  CreateIssueGroup(qc1, "ic1", 3),
				                  CreateIssueGroup(qc2, "ic2", 4),
				                  CreateIssueGroup(qc3, "ic3", 5),
				                  CreateIssueGroup(qc3, "ic4", 6)
			                  };

			var reportDefinition = new HtmlReportDefinition(
				"templatepath", "fileName",
				new List<HtmlDataQualityCategoryOptions>
				{
					new HtmlDataQualityCategoryOptions(catA11.Uuid, ignoreCategoryLevel: true),
					new HtmlDataQualityCategoryOptions(catB1.Uuid, ignoreCategoryLevel: true)
				});

			List<HtmlReportIssueGroup> reportIssueGroups;
			List<HtmlReportDataQualityCategory> reportCategories =
				GroupByCategories(issueGroups, reportDefinition,
				                  out reportIssueGroups);

			Assert.AreEqual(4, reportIssueGroups.Count);
			Assert.AreEqual(18, reportIssueGroups.Sum(ig => ig.IssueCount));

			Assert.AreEqual(4, reportCategories.Count);
			Assert.AreEqual(18, reportCategories.Sum(qc => qc.IssueCount));

			List<HtmlReportDataQualityCategory> rootCategories =
				reportCategories.Where(qc => qc.IsRoot)
				                .ToList();
			Assert.AreEqual(2, rootCategories.Count);
			Assert.AreEqual(18, rootCategories.Sum(qc => qc.IssueCountWithChildren));
		}

		[Test]
		public void CanGroupQualityConditionsWithRootExclusion()
		{
			var catA = new DataQualityCategory("A");
			var catA1 = new DataQualityCategory("A1");
			var catA11 = new DataQualityCategory("A11");

			catA.AddSubCategory(catA1);
			catA1.AddSubCategory(catA11);

			var test = new TestDescriptor("test", new ClassDescriptor(typeof(QaMinSegAngle)));

			var qc1 = new QualityCondition("qc1", test) {Category = catA11};
			var qc2 = new QualityCondition("qc2", test) {Category = catA11};
			var qc3 = new QualityCondition("qc3", test) {Category = catA11};

			var issueGroups = new List<IssueGroup>
			                  {
				                  CreateIssueGroup(qc1, "ic1", 3),
				                  CreateIssueGroup(qc2, "ic2", 4),
				                  CreateIssueGroup(qc3, "ic3", 5),
				                  CreateIssueGroup(qc3, "ic4", 6)
			                  };

			var reportDefinition = new HtmlReportDefinition(
				"templatepath", "fileName",
				new List<HtmlDataQualityCategoryOptions>
				{
					new HtmlDataQualityCategoryOptions(catA.Uuid, ignoreCategoryLevel: true),
				});

			List<HtmlReportIssueGroup> reportIssueGroups;
			List<HtmlReportDataQualityCategory> reportCategories =
				GroupByCategories(issueGroups, reportDefinition,
				                  out reportIssueGroups);

			Assert.AreEqual(4, reportIssueGroups.Count);
			Assert.AreEqual(18, reportIssueGroups.Sum(ig => ig.IssueCount));

			Assert.AreEqual(2, reportCategories.Count);
			Assert.AreEqual(18, reportCategories.Sum(qc => qc.IssueCount));

			List<HtmlReportDataQualityCategory> rootCategories =
				reportCategories.Where(qc => qc.IsRoot)
				                .ToList();
			Assert.AreEqual(1, rootCategories.Count);
			Assert.AreEqual(18, rootCategories.Sum(qc => qc.IssueCountWithChildren));
		}

		[Test]
		public void CanGroupQualityConditionsWithNonRootExclusion()
		{
			var catA = new DataQualityCategory("A");
			var catA1 = new DataQualityCategory("A1");
			var catA11 = new DataQualityCategory("A11");

			catA.AddSubCategory(catA1);
			catA1.AddSubCategory(catA11);

			var test = new TestDescriptor("test", new ClassDescriptor(typeof(QaMinSegAngle)));

			var qc1 = new QualityCondition("qc1", test) {Category = catA11};
			var qc2 = new QualityCondition("qc2", test) {Category = catA11};
			var qc3 = new QualityCondition("qc3", test) {Category = catA11};

			var issueGroups = new List<IssueGroup>
			                  {
				                  CreateIssueGroup(qc1, "ic1", 3),
				                  CreateIssueGroup(qc2, "ic2", 4),
				                  CreateIssueGroup(qc3, "ic3", 5),
				                  CreateIssueGroup(qc3, "ic4", 6)
			                  };

			var reportDefinition = new HtmlReportDefinition(
				"templatepath", "fileName",
				new List<HtmlDataQualityCategoryOptions>
				{
					new HtmlDataQualityCategoryOptions(catA1.Uuid, ignoreCategoryLevel: true),
					new HtmlDataQualityCategoryOptions(catA11.Uuid, ignoreCategoryLevel: true),
				});

			List<HtmlReportIssueGroup> reportIssueGroups;
			List<HtmlReportDataQualityCategory> reportCategories =
				GroupByCategories(issueGroups, reportDefinition,
				                  out reportIssueGroups);

			Assert.AreEqual(4, reportIssueGroups.Count);
			Assert.AreEqual(18, reportIssueGroups.Sum(ig => ig.IssueCount));

			Assert.AreEqual(1, reportCategories.Count);
			Assert.AreEqual(18, reportCategories.Sum(qc => qc.IssueCount));

			List<HtmlReportDataQualityCategory> rootCategories =
				reportCategories.Where(qc => qc.IsRoot)
				                .ToList();
			Assert.AreEqual(1, rootCategories.Count);
			Assert.AreEqual(18, rootCategories.Sum(qc => qc.IssueCountWithChildren));
		}

		[Test]
		public void CanGroupQualityConditionsWitIntermediateExclusion()
		{
			var catA = new DataQualityCategory("A");
			var catA1 = new DataQualityCategory("A1");
			var catA11 = new DataQualityCategory("A11");

			catA.AddSubCategory(catA1);
			catA1.AddSubCategory(catA11);

			var test = new TestDescriptor("test", new ClassDescriptor(typeof(QaMinSegAngle)));

			var qc1 = new QualityCondition("qc1", test) {Category = catA};
			var qc2 = new QualityCondition("qc2", test) {Category = catA1};
			var qc3 = new QualityCondition("qc3", test) {Category = catA11};

			var issueGroups = new List<IssueGroup>
			                  {
				                  CreateIssueGroup(qc1, "ic1", 3),
				                  CreateIssueGroup(qc2, "ic2", 4),
				                  CreateIssueGroup(qc3, "ic3", 5),
				                  CreateIssueGroup(qc3, "ic4", 6)
			                  };

			var reportDefinition = new HtmlReportDefinition(
				"templatepath", "fileName",
				new List<HtmlDataQualityCategoryOptions>
				{
					new HtmlDataQualityCategoryOptions(catA1.Uuid, ignoreCategoryLevel: true),
				});

			List<HtmlReportIssueGroup> reportIssueGroups;
			List<HtmlReportDataQualityCategory> reportCategories =
				GroupByCategories(issueGroups, reportDefinition,
				                  out reportIssueGroups);

			Assert.AreEqual(4, reportIssueGroups.Count);
			Assert.AreEqual(18, reportIssueGroups.Sum(ig => ig.IssueCount));

			Assert.AreEqual(2, reportCategories.Count);
			Assert.AreEqual(18, reportCategories.Sum(qc => qc.IssueCount));

			List<HtmlReportDataQualityCategory> rootCategories =
				reportCategories.Where(qc => qc.IsRoot)
				                .ToList();
			Assert.AreEqual(1, rootCategories.Count);
			Assert.AreEqual(18, rootCategories.Sum(qc => qc.IssueCountWithChildren));
		}

		[Test]
		public void CanGroupQualityConditionsWithCompleteExclusion()
		{
			var catA = new DataQualityCategory("A");
			var catA1 = new DataQualityCategory("A1");
			var catA11 = new DataQualityCategory("A11");

			catA.AddSubCategory(catA1);
			catA1.AddSubCategory(catA11);

			var test = new TestDescriptor("test", new ClassDescriptor(typeof(QaMinSegAngle)));

			var qc1 = new QualityCondition("qc1", test) {Category = catA11};
			var qc2 = new QualityCondition("qc2", test) {Category = catA11};
			var qc3 = new QualityCondition("qc3", test) {Category = catA11};

			var issueGroups = new List<IssueGroup>
			                  {
				                  CreateIssueGroup(qc1, "ic1", 3),
				                  CreateIssueGroup(qc2, "ic2", 4),
				                  CreateIssueGroup(qc3, "ic3", 5),
				                  CreateIssueGroup(qc3, "ic4", 6)
			                  };

			var reportDefinition = new HtmlReportDefinition(
				"templatepath", "fileName",
				new List<HtmlDataQualityCategoryOptions>
				{
					new HtmlDataQualityCategoryOptions(catA.Uuid, ignoreCategoryLevel: true),
					new HtmlDataQualityCategoryOptions(catA1.Uuid, ignoreCategoryLevel: true),
					new HtmlDataQualityCategoryOptions(catA11.Uuid, ignoreCategoryLevel: true),
				});

			List<HtmlReportIssueGroup> reportIssueGroups;
			List<HtmlReportDataQualityCategory> reportCategories =
				GroupByCategories(issueGroups, reportDefinition,
				                  out reportIssueGroups);

			Assert.AreEqual(4, reportIssueGroups.Count);
			Assert.AreEqual(18, reportIssueGroups.Sum(ig => ig.IssueCount));

			Assert.AreEqual(1, reportCategories.Count);
			Assert.AreEqual(18, reportCategories.Sum(qc => qc.IssueCount));

			List<HtmlReportDataQualityCategory> rootCategories =
				reportCategories.Where(qc => qc.IsRoot)
				                .ToList();
			Assert.AreEqual(1, rootCategories.Count);
			Assert.AreEqual(18, rootCategories.Sum(qc => qc.IssueCountWithChildren));
		}

		[Test]
		public void CanSortExceptionCategories()
		{
			var exceptionCategories = new List<ExceptionCategory>
			                          {
				                          new ExceptionCategory("C"),
				                          new ExceptionCategory("B"),
				                          new ExceptionCategory("A"),
				                          new ExceptionCategory(null)
			                          };

			exceptionCategories.Sort();

			Assert.AreEqual("A", exceptionCategories[0].Name);
			Assert.AreEqual("B", exceptionCategories[1].Name);
			Assert.AreEqual("C", exceptionCategories[2].Name);
			Assert.AreEqual(null, exceptionCategories[3].Name);
		}

		[Test]
		public void CanAggregateExceptionCategoryCounts()
		{
			var exceptionCategories = new List<ExceptionCategory>
			                          {
				                          new ExceptionCategory("C"),
				                          new ExceptionCategory("B"),
				                          new ExceptionCategory("A"),
				                          new ExceptionCategory(null)
			                          };

			var categoryCounts = new List<HtmlExceptionCategoryCount>();
			categoryCounts.Add(new HtmlExceptionCategoryCount(new ExceptionCategory("b"), 10));
			categoryCounts.Add(
				new HtmlExceptionCategoryCount(new ExceptionCategory(null), 100));

			List<HtmlExceptionCategoryCount> result =
				HtmlReportUtils.AggregateExceptionCategoryCounts(
					categoryCounts, exceptionCategories);

			foreach (HtmlExceptionCategoryCount aggregated in result)
			{
				Console.WriteLine($@"{aggregated.Name}: {aggregated.ExceptionCount}");
			}

			Assert.AreEqual(4, result.Count);

			Assert.AreEqual("C", result[0].Name);
			Assert.AreEqual(0, result[0].ExceptionCount);

			Assert.AreEqual("B", result[1].Name);
			Assert.AreEqual(10, result[1].ExceptionCount);

			Assert.AreEqual("A", result[2].Name);
			Assert.AreEqual(0, result[2].ExceptionCount);

			Assert.AreEqual("-", result[3].Name);
			Assert.AreEqual(100, result[3].ExceptionCount);
		}

		[NotNull]
		private static IssueGroup CreateIssueGroup(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] string issueCode,
			int issueCount)
		{
			return new IssueGroup(qualityCondition,
			                      new IssueCode(issueCode),
			                      issueCount: issueCount,
			                      affectedComponent: null,
			                      issueDescription: null,
			                      allowable: true,
			                      stopCondition: false);
		}

		private class IssueStatistics : IIssueStatistics
		{
			[NotNull] private readonly List<IssueGroup> _issueGroups;

			public IssueStatistics([NotNull] IEnumerable<IssueGroup> issueGroups,
			                       IEnumerable<ExceptionCategory> exceptionCategories = null)
			{
				_issueGroups = issueGroups.ToList();
				ExceptionCategories =
					exceptionCategories?.ToList() ?? new List<ExceptionCategory>();

				WarningCount = _issueGroups.Where(ig => ig.Allowable).Sum(ig => ig.IssueCount);
				ErrorCount = _issueGroups.Where(ig => ! ig.Allowable).Sum(ig => ig.IssueCount);
				IssueCount = WarningCount + ErrorCount;

				ExceptionCount = _issueGroups.Sum(ig => ig.ExceptionCount);
			}

			public IList<ExceptionCategory> ExceptionCategories { get; }

			public int IssueCount { get; }

			public int WarningCount { get; }

			public int ErrorCount { get; }

			public int ExceptionCount { get; }

			public IEnumerable<IssueGroup> GetIssueGroups()
			{
				return _issueGroups;
			}
		}
	}
}
