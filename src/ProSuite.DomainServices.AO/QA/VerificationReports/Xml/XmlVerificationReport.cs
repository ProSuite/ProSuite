using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.Xml;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	[XmlRoot("QualityVerification")]
	public class XmlVerificationReport
	{
		[NotNull] private readonly List<XmlVerifiedDataset> _verifiedDatasets =
			new List<XmlVerifiedDataset>();

		[NotNull] private readonly VerifiedCategoriesBuilder _verifiedCategoriesBuilder =
			new VerifiedCategoriesBuilder();

		[NotNull] private readonly VerifiedCategoriesBuilder _categoriesWithIssuesBuilder =
			new VerifiedCategoriesBuilder();

		[NotNull] private readonly List<XmlDataSourceDescription> _dataSourceDescriptions =
			new List<XmlDataSourceDescription>();

		[XmlAttribute("qualitySpecification")]
		public string QualitySpecification { get; set; }

		[XmlAttribute("startTime")]
		public DateTime StartTime { get; set; }

		[XmlAttribute("endTime")]
		public DateTime EndTime { get; set; }

		[XmlAttribute("processingSeconds")]
		public double ProcessingTimeSeconds { get; set; }

		[XmlAttribute("warningCount")]
		public int WarningCount { get; set; }

		[XmlAttribute("errorCount")]
		public int ErrorCount { get; set; }

		[XmlAttribute("exceptionCount")]
		[DefaultValue(0)]
		public int ExceptionCount { get; set; }

		[XmlAttribute("stopErrorCount")]
		public int StopErrorCount { get; set; }

		[XmlAttribute("cancelled")]
		[DefaultValue(false)]
		public bool Cancelled { get; set; }

		[XmlAttribute("version")]
		public string Version { get; set; }

		[XmlArray("Properties")]
		[XmlArrayItem("Property")]
		[CanBeNull]
		public List<XmlNameValuePair> Properties { get; set; }

		[XmlElement("TestExtent")]
		[CanBeNull]
		public Xml2DEnvelope TestExtent { get; set; }

		[CanBeNull]
		[XmlElement("AreaOfInterest")]
		public XmlAreaOfInterest AreaOfInterest { get; set; }

		[XmlArray("VerifiedConditions")]
		[XmlArrayItem("Category")]
		[CanBeNull]
		public List<XmlVerifiedCategory> VerifiedCategories
			=> _verifiedCategoriesBuilder.RootCategories.Count == 0
				   ? null
				   : _verifiedCategoriesBuilder.RootCategories;

		[XmlArray("VerifiedDatasets")]
		[XmlArrayItem("VerifiedDataset")]
		[NotNull]
		public List<XmlVerifiedDataset> VerifiedDatasets => _verifiedDatasets;

		[XmlArray("DataSourceDescriptions")]
		[XmlArrayItem("DataSourceDescription")]
		[NotNull]
		public List<XmlDataSourceDescription> DataSourceDescriptions => _dataSourceDescriptions;

		[XmlArray("ReportedIssues")]
		[XmlArrayItem("Category")]
		[CanBeNull]
		public List<XmlVerifiedCategory> CategoriesWithIssues
			=> _categoriesWithIssuesBuilder.RootCategories.Count == 0
				   ? null
				   : _categoriesWithIssuesBuilder.RootCategories;

		[CanBeNull]
		[XmlElement("Exceptions")]
		public XmlExceptionStatistics ExceptionStatistics { get; set; }

		/// <summary>
		/// Adds the verified condition.
		/// </summary>
		/// <param name="xmlCondition">The quality condition.</param>
		public void AddVerifiedCondition([NotNull] XmlVerifiedQualityCondition xmlCondition)
		{
			Assert.ArgumentNotNull(xmlCondition, nameof(xmlCondition));

			_verifiedCategoriesBuilder.AddVerifiedCondition(xmlCondition);
		}

		public void AddVerifiedDataset([NotNull] XmlVerifiedDataset xmlVerifiedDataset)
		{
			_verifiedDatasets.Add(xmlVerifiedDataset);
		}

		public void AddConditionWithIssues(
			[NotNull] XmlVerifiedQualityCondition xmlCondition)
		{
			Assert.ArgumentNotNull(xmlCondition, nameof(xmlCondition));

			_categoriesWithIssuesBuilder.AddVerifiedCondition(xmlCondition);
		}

		public void AddDataSourceDescription(string modelName, string dataSourceDescription)
		{
			_dataSourceDescriptions.Add(
				new XmlDataSourceDescription(modelName, dataSourceDescription));
		}
	}
}
