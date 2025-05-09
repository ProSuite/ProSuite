using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DotLiquid;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.AO.QA.SpecificationReport;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options;
using ProSuite.DomainServices.AO.QA.VerificationReports.Xml;
using HtmlTexts = ProSuite.DomainServices.AO.QA.HtmlReports.HtmlTexts;

namespace ProSuite.DomainServices.AO.QA.Standalone
{
	public static class StandaloneVerificationUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static bool TryDeleteOutputDirectory([NotNull] string directoryPath)
		{
			try
			{
				_msg.DebugFormat("Trying to delete the generated output directory: {0}",
				                 directoryPath);

				const bool recursive = true;
				Directory.Delete(directoryPath, recursive);

				_msg.Debug("Directory successfully deleted");
				return true;
			}
			catch (Exception e)
			{
				_msg.WarnFormat("Error cleaning up generated output directory: {0}", e.Message);
				return false;
			}
		}

		[CanBeNull]
		public static ExceptionObjectRepository PrepareExceptionRepository(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] IDatasetContext datasetContext,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
			[CanBeNull] AreaOfInterest areaOfInterest,
			[CanBeNull] XmlVerificationOptions verificationOptions)
		{
			if (verificationOptions == null)
			{
				return null;
			}

			IWorkspace workspace = VerificationOptionUtils.GetExceptionWorkspace(
				verificationOptions);

			if (workspace == null)
			{
				return null;
			}

			bool useDbfFieldNames = WorkspaceUtils.IsShapefileWorkspace(workspace);

			IIssueTableFieldManagement issueTableFields =
				IssueTableFieldsFactory.GetIssueTableFields(true, useDbfFieldNames);

			var result = new ExceptionObjectRepository(workspace, issueTableFields,
			                                           datasetContext, datasetResolver,
			                                           areaOfInterest?.Geometry);

			List<QualityCondition> qualityConditions =
				qualitySpecification.Elements.Select(element => element.QualityCondition)
				                    .ToList();

			InvolvedObjectsMatchCriteria involvedObjectsMatchCriteria =
				VerificationOptionUtils.GetInvolvedObjectMatchCriteria(verificationOptions);

			result.ReadExceptions(qualityConditions,
			                      VerificationOptionUtils.GetDefaultShapeMatchCriterion(
				                      verificationOptions),
			                      VerificationOptionUtils.GetDefaultExceptionObjectStatus(
				                      verificationOptions),
			                      involvedObjectsMatchCriteria);
			return result;
		}

		[CanBeNull]
		public static DdxModel GetPrimaryModel(
			[NotNull] QualitySpecification qualitySpecification)
		{
			var referenceCountByModel = new Dictionary<DdxModel, int>();

			foreach (QualitySpecificationElement element in qualitySpecification.Elements)
			{
				QualityCondition condition = element.QualityCondition;
				foreach (Dataset dataset in condition.GetDatasetParameterValues(
					         includeReferencedProcessors: true))
				{
					if (! referenceCountByModel.ContainsKey(dataset.Model))
					{
						referenceCountByModel.Add(dataset.Model, 1);
					}
					else
					{
						referenceCountByModel[dataset.Model]++;
					}
				}
			}

			DdxModel maxReferenceModel = null;
			var maxReferenceCount = 0;

			foreach (KeyValuePair<DdxModel, int> pair in referenceCountByModel)
			{
				if (pair.Value <= maxReferenceCount)
				{
					continue;
				}

				maxReferenceCount = pair.Value;
				maxReferenceModel = pair.Key;
			}

			return maxReferenceModel;
		}

		[NotNull]
		public static List<string> WriteQualitySpecificationReport(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] string directory,
			[CanBeNull] string defaultTemplateFilePath,
			[CanBeNull] XmlVerificationOptions options)
		{
			string defaultTemplateDirectory =
				VerificationOptionUtils.GetDefaultTemplateDirectory(options);

			var reportFilePaths = new List<string>();

			foreach (XmlSpecificationReportOptions reportOptions in
			         VerificationOptionUtils.GetSpecificationReportOptions(options,
				         defaultTemplateFilePath))
			{
				SpecificationReportDefinition reportDefinition =
					VerificationOptionUtils.GetSpecificationReportDefinition(reportOptions,
						defaultTemplateFilePath,
						defaultTemplateDirectory);

				if (! File.Exists(reportDefinition.TemplatePath))
				{
					WarnFileNotExists(reportDefinition.TemplatePath);

					continue;
				}

				string filePath = Path.Combine(directory, reportDefinition.FileName);

				Assert.True(FileSystemUtils.EnsureDirectoryExists(directory),
				            $"Invalid directory: {directory}");

				HtmlQualitySpecification model =
					SpecificationReportUtils.CreateHtmlQualitySpecification(qualitySpecification,
						reportDefinition);

				string reportFilePath = SpecificationReportUtils.RenderHtmlQualitySpecification(
					model, reportDefinition.TemplatePath, filePath);

				reportFilePaths.Add(reportFilePath);
			}

			return reportFilePaths;
		}

		[NotNull]
		public static IList<DataSource> GetDataSources(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] XmlQualitySpecification xmlQualitySpecification)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNull(xmlQualitySpecification, nameof(xmlQualitySpecification));

			XmlDataQualityDocumentCache documentCache =
				XmlDataQualityUtils.GetDocumentCache(
					document, new[] { xmlQualitySpecification },
					new TestParameterDatasetValidator());

			bool hasUndefinedWorkspaceReference;
			IList<XmlWorkspace> xmlWorkspaces = XmlDataQualityUtils.GetReferencedWorkspaces(
				documentCache, out hasUndefinedWorkspaceReference);

			var result = new List<DataSource>();
			if (hasUndefinedWorkspaceReference)
			{
				result.Add(new DataSource(GetUniqueDefaultName(xmlWorkspaces),
				                          DataSource.AnonymousId));
			}

			result.AddRange(
				xmlWorkspaces.Select(xmlWorkspace => new DataSource(xmlWorkspace)));

			// TODO sort based on reference count?

			return result;
		}

		[CanBeNull]
		public static Func<IObjectDataset, string> GetKeyFieldLookupFunction(
			[CanBeNull] XmlVerificationOptions verificationOptions)
		{
			if (verificationOptions?.KeyFields == null)
			{
				return null;
			}

			var keyFieldLookup = new KeyFieldLookup(verificationOptions.KeyFields);

			return keyFieldLookup.GetKeyField;
		}

		[NotNull]
		public static List<string> WriteHtmlReports(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] string directory,
			[NotNull] IssueStatistics issueStatistics,
			[NotNull] XmlVerificationReport verificationReport,
			[NotNull] string verificationReportFileName,
			[CanBeNull] string defaultReportTemplatePath = null,
			[CanBeNull] XmlVerificationOptions options = null,
			[CanBeNull] string issueGdbPath = null,
			[CanBeNull] IList<string> issueMapFilePaths = null,
			[CanBeNull] IList<string> qualitySpecificationReportFilePaths = null)
		{
			string defaultTemplateDirectory =
				VerificationOptionUtils.GetDefaultTemplateDirectory(options);

			var reportDefinitions = new List<HtmlReportDefinition>();

			foreach (XmlHtmlReportOptions reportOptions in
			         VerificationOptionUtils.GetHtmlReportOptions(
				         options, defaultReportTemplatePath))
			{
				HtmlReportDefinition reportDefinition =
					VerificationOptionUtils.GetReportDefinition(reportOptions,
					                                            defaultReportTemplatePath,
					                                            defaultTemplateDirectory);
				if (! File.Exists(reportDefinition.TemplatePath))
				{
					WarnFileNotExists(reportOptions.TemplatePath);
					continue;
				}

				reportDefinitions.Add(reportDefinition);
			}

			List<string> filePaths =
				reportDefinitions.Select(d => Path.Combine(directory, d.FileName))
				                 .ToList();

			foreach (HtmlReportDefinition reportDefinition in reportDefinitions)
			{
				string reportFilePath =
					WriteHtmlReport(qualitySpecification, directory,
					                reportDefinition,
					                issueStatistics, verificationReport,
					                verificationReportFileName,
					                issueGdbPath,
					                issueMapFilePaths,
					                filePaths,
					                qualitySpecificationReportFilePaths);

				_msg.InfoFormat("Html report written to {0}", reportFilePath);
			}

			return filePaths;
		}

		private static string WriteHtmlReport(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] string directory,
			[NotNull] HtmlReportDefinition reportDefinition,
			[NotNull] IssueStatistics issueStatistics,
			[NotNull] XmlVerificationReport verificationReport,
			[NotNull] string verificationReportFileName,
			[CanBeNull] string issueGdbPath,
			[CanBeNull] IEnumerable<string> issueMapFilePaths,
			[NotNull] IEnumerable<string> htmlReportFileNames,
			[CanBeNull] IEnumerable<string> qualitySpecificationReportFilePaths)
		{
			Assert.ArgumentNotNull(reportDefinition, nameof(reportDefinition));
			Assert.ArgumentCondition(File.Exists(reportDefinition.TemplatePath),
			                         "Template file does not exist: {0}",
			                         reportDefinition.TemplatePath);

			string reportFilePath = Path.Combine(directory, reportDefinition.FileName);

			_msg.DebugFormat("Preparing html report model");
			var reportModel = new HtmlReportModel(qualitySpecification,
			                                      issueStatistics,
			                                      verificationReport,
			                                      directory,
			                                      verificationReportFileName,
			                                      issueGdbPath,
			                                      issueMapFilePaths,
			                                      htmlReportFileNames,
			                                      qualitySpecificationReportFilePaths,
			                                      reportDefinition);

			_msg.DebugFormat("Rendering html report based on template {0}",
			                 reportDefinition.TemplatePath);

			LiquidUtils.RegisterSafeType<HtmlReportModel>();
			LiquidUtils.RegisterSafeType<HtmlTexts>();

			string output = LiquidUtils.Render(
				reportDefinition.TemplatePath,
				new KeyValuePair<string, object>("report", reportModel),
				new KeyValuePair<string, object>("text", new HtmlTexts()));

			_msg.DebugFormat("Writing html report to {0}", reportFilePath);
			FileSystemUtils.WriteTextFile(output, reportFilePath);

			return reportFilePath;
		}

		[NotNull]
		private static string GetUniqueDefaultName(
			[NotNull] IEnumerable<XmlWorkspace> xmlWorkspaces)
		{
			Assert.ArgumentNotNull(xmlWorkspaces, nameof(xmlWorkspaces));

			var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (XmlWorkspace xmlWorkspace in xmlWorkspaces)
			{
				names.Add(xmlWorkspace.ModelName);
			}

			var candidateName = "DEFAULT";
			while (true)
			{
				if (! names.Contains(candidateName))
				{
					return candidateName;
				}

				candidateName = $"[{candidateName}]";
			}
		}

		private static void WarnFileNotExists(string filePath)
		{
			string warning = $"Template file does not exist: {filePath}";

			_msg.WarnFormat(warning);
		}
	}
}
