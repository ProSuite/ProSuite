using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.IO;
using ProSuite.Commons.Text;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.Core.QA.Html;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.Schemas;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public static class VerificationOptionUtils
	{
		[NotNull]
		public static string GetMxdDocumentName([CanBeNull] XmlVerificationOptions options)
		{
			if (options == null || StringUtils.IsNullOrEmptyOrBlank(options.MxdDocumentName))
			{
				return "issues.mxd";
			}

			string name = Assert.NotNull(options.MxdDocumentName).Trim();

			if (FileSystemUtils.HasInvalidFileNameChars(name))
			{
				throw new InvalidConfigurationException(
					$"Mxd document name is not a valid file name: {name}");
			}

			return name;
		}

		[NotNull]
		public static string GetXmlReportFileName(
			[CanBeNull] XmlVerificationOptions options)
		{
			if (options == null || StringUtils.IsNullOrEmptyOrBlank(options.XmlReportName))
			{
				return "verification.xml";
			}

			string name = Assert.NotNull(options.XmlReportName).Trim();

			if (FileSystemUtils.HasInvalidFileNameChars(name))
			{
				throw new InvalidConfigurationException(
					$"Xml report name is not a valid file name: {name}");
			}

			return name;
		}

		[NotNull]
		public static string GetIssueWorkspaceName(
			[CanBeNull] XmlVerificationOptions options)
		{
			if (options == null || StringUtils.IsNullOrEmptyOrBlank(options.IssueGdbName))
			{
				return "issues";
			}

			string name = Assert.NotNull(options.IssueGdbName).Trim();

			if (FileSystemUtils.HasInvalidFileNameChars(name))
			{
				throw new InvalidConfigurationException(
					$"Issue workspace name is not a valid file or directory name: {name}");
			}

			return name;
		}

		[NotNull]
		public static string GetProgressWorkspaceName(
			[CanBeNull] XmlVerificationOptions options)
		{
			if (string.IsNullOrWhiteSpace(options?.ProgressGdbName))
			{
				return "progress";
			}

			string name = Assert.NotNull(options.ProgressGdbName).Trim();

			if (FileSystemUtils.HasInvalidFileNameChars(name))
			{
				throw new InvalidConfigurationException(
					$"Progress workspace name is not a valid file or directory name: {name}");
			}

			return name;
		}

		public static bool ExportExceptions([CanBeNull] XmlVerificationOptions options)
		{
			return options?.Exceptions != null && options.Exceptions.ExportExceptions;
		}

		public static string GetExportedExceptionsWorkspaceName(
			[CanBeNull] XmlVerificationOptions options)
		{
			if (options == null || StringUtils.IsNullOrEmptyOrBlank(options.ExceptionGdbName))
			{
				return "exceptions";
			}

			string name = Assert.NotNull(options.ExceptionGdbName).Trim();

			if (FileSystemUtils.HasInvalidFileNameChars(name))
			{
				throw new InvalidConfigurationException(
					$"Exception workspace name is not a valid file or directory name: {name}");
			}

			return name;
		}

		[NotNull]
		public static HtmlReportDefinition GetReportDefinition(
			[NotNull] XmlHtmlReportOptions reportOptions,
			[CanBeNull] string defaultTemplatePath,
			[CanBeNull] string defaultTemplateDirectory)
		{
			Assert.ArgumentNotNull(reportOptions, nameof(reportOptions));

			string templatePath = reportOptions.TemplatePath;
			string fileName = reportOptions.ReportFileName;

			if (StringUtils.IsNullOrEmptyOrBlank(templatePath))
			{
				if (StringUtils.IsNullOrEmptyOrBlank(defaultTemplatePath))
				{
					throw new InvalidConfigurationException(
						"Html report template path is not defined");
				}

				templatePath = defaultTemplatePath;
			}

			if (StringUtils.IsNullOrEmptyOrBlank(fileName))
			{
				throw new InvalidConfigurationException("Html file name is not defined");
			}

			if (FileSystemUtils.HasInvalidPathChars(templatePath))
			{
				throw new InvalidConfigurationException(
					$"Invalid html report template path: {templatePath}");
			}

			if (FileSystemUtils.HasInvalidFileNameChars(fileName))
			{
				throw new InvalidConfigurationException(
					$"Invalid html report file name: {fileName}");
			}

			if (defaultTemplateDirectory != null && ! Path.IsPathRooted(templatePath))
			{
				templatePath = Path.GetFullPath(
					Path.Combine(defaultTemplateDirectory, templatePath));
			}

			return new HtmlReportDefinition(
				templatePath, fileName,
				GetHtmlReportDataQualityCategoryOptions(reportOptions.CategoryOptions));
		}

		[NotNull]
		public static SpecificationReportDefinition GetSpecificationReportDefinition(
			[NotNull] XmlSpecificationReportOptions reportOptions,
			[CanBeNull] string defaultTemplatePath,
			[CanBeNull] string defaultTemplateDirectory)
		{
			Assert.ArgumentNotNull(reportOptions, nameof(reportOptions));

			string templatePath = reportOptions.TemplatePath;
			string fileName = reportOptions.ReportFileName;

			if (StringUtils.IsNullOrEmptyOrBlank(templatePath))
			{
				if (StringUtils.IsNullOrEmptyOrBlank(defaultTemplatePath))
				{
					throw new InvalidConfigurationException(
						"Quality specification report template path is not defined");
				}

				templatePath = defaultTemplatePath;
			}

			if (StringUtils.IsNullOrEmptyOrBlank(fileName))
			{
				throw new InvalidConfigurationException(
					"Quality specification report file name is not defined");
			}

			if (FileSystemUtils.HasInvalidPathChars(templatePath))
			{
				throw new InvalidConfigurationException(
					$"Invalid quality specification report template path: {templatePath}");
			}

			if (FileSystemUtils.HasInvalidFileNameChars(fileName))
			{
				throw new InvalidConfigurationException(
					$"Invalid quality specification report file name: {fileName}");
			}

			if (defaultTemplateDirectory != null && ! Path.IsPathRooted(templatePath))
			{
				templatePath = Path.GetFullPath(
					Path.Combine(defaultTemplateDirectory, templatePath));
			}

			return new SpecificationReportDefinition(
				templatePath, fileName,
				GetHtmlReportDataQualityCategoryOptions(reportOptions.CategoryOptions));
		}

		[NotNull]
		public static IssueMapDefinition GetIssueMapDefinition(
			[NotNull] XmlIssueMapOptions options,
			[CanBeNull] string defaultTemplatePath,
			[CanBeNull] string defaultTemplateDirectory)
		{
			Assert.ArgumentNotNull(options, nameof(options));

			string templatePath = options.TemplatePath;
			string fileName = options.MxdFileName;

			if (StringUtils.IsNullOrEmptyOrBlank(templatePath))
			{
				if (StringUtils.IsNullOrEmptyOrBlank(defaultTemplatePath))
				{
					throw new InvalidConfigurationException(
						"Issue map template path is not defined");
				}

				templatePath = defaultTemplatePath;
			}

			if (StringUtils.IsNullOrEmptyOrBlank(fileName))
			{
				throw new InvalidConfigurationException("Issue map file name is not defined");
			}

			if (FileSystemUtils.HasInvalidPathChars(templatePath))
			{
				throw new InvalidConfigurationException(
					$"Invalid issue map template path: {templatePath}");
			}

			if (FileSystemUtils.HasInvalidFileNameChars(fileName))
			{
				throw new InvalidConfigurationException(
					$"Invalid issue map file name: {fileName}");
			}

			if (defaultTemplateDirectory != null && ! Path.IsPathRooted(templatePath))
			{
				templatePath = Path.GetFullPath(
					Path.Combine(defaultTemplateDirectory, templatePath));
			}

			return new IssueMapDefinition(
				templatePath, fileName,
				options.ListLayersByAffectedComponent,
				GetLabellingDefinition(options.IssueLabelOptions, options.DisplayLabels),
				GetLabellingDefinition(options.ExceptionLabelOptions, options.DisplayLabels),
				GetDisplayExpression(options.IssueDisplayExpression, options.ShowMapTips),
				GetDisplayExpression(options.ExceptionDisplayExpression, options.ShowMapTips),
				options.VerifiedFeaturesMinimumScale,
				options.IssueLayersGroupBy,
				ParseDocumentVersion(options.Version),
				GetFieldConfigurator(options.IssueFieldOptions),
				GetFieldConfigurator(options.ExceptionFieldOptions));
		}

		public static esriArcGISVersion ParseDocumentVersion(
			[CanBeNull] string versionString)
		{
			if (versionString == null)
			{
				return esriArcGISVersion.esriArcGISVersionCurrent;
			}

			string trimmedVersion = versionString.Trim();

			if (trimmedVersion.Length == 0)
			{
				return esriArcGISVersion.esriArcGISVersionCurrent;
			}

			if (string.Equals(trimmedVersion, "current", StringComparison.OrdinalIgnoreCase))
			{
				return esriArcGISVersion.esriArcGISVersionCurrent;
			}

			double version;
			if (! double.TryParse(trimmedVersion,
			                      NumberStyles.AllowDecimalPoint,
			                      CultureInfo.InvariantCulture,
			                      out version))
			{
				throw new InvalidConfigurationException(
					$"Unsupported ArcGIS version for saving documents (<major>.<minor> expected): {trimmedVersion}");
			}

			int major = Convert.ToInt32(Math.Floor(version));
			int minor = Convert.ToInt32((version - major) * 10);

			return GetDocumentVersion(major, minor);
		}

		[CanBeNull]
		public static string GetDefaultTemplateDirectory(
			[CanBeNull] XmlVerificationOptions options)
		{
			if (options == null)
			{
				return null;
			}

			string path = options.DefaultTemplateDirectoryPath;

			if (StringUtils.IsNullOrEmptyOrBlank(path))
			{
				return null;
			}

			string trimmedPath = path.Trim();

			if (FileSystemUtils.HasInvalidPathChars(trimmedPath))
			{
				throw new InvalidConfigurationException(
					$"Invalid default template directory path: {trimmedPath}");
			}

			if (! Path.IsPathRooted(trimmedPath))
			{
				throw new InvalidConfigurationException(
					$"Default template directory should be an absolute path: {trimmedPath}");
			}

			return trimmedPath;
		}

		[NotNull]
		public static XmlVerificationOptions ReadOptionsFile([NotNull] string xmlFilePath)
		{
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));
			Assert.ArgumentCondition(File.Exists(xmlFilePath),
			                         "File does not exist: {0}", xmlFilePath);

			string schema = Schema.ProSuite_QA_XmlBasedVerificationOptions_1_0;

			try
			{
				return XmlUtils.DeserializeFile<XmlVerificationOptions>(xmlFilePath, schema);
			}
			catch (Exception e)
			{
				throw new XmlDeserializationException($"Error deserializing file: {e.Message}", e);
			}
		}

		[CanBeNull]
		public static XmlVerificationOptions ReadOptionsXml([CanBeNull] string xml)
		{
			if (string.IsNullOrEmpty(xml))
			{
				return null;
			}

			xml = xml.TrimStart('\ufeff', ' ', '\t', '\r', '\n');

			string schema = Schema.ProSuite_QA_XmlBasedVerificationOptions_1_0;

			try
			{
				using (TextReader sr = new StringReader(xml))
				{
					return XmlUtils.Deserialize<XmlVerificationOptions>(sr, schema);
				}
			}
			catch (Exception e)
			{
				throw new XmlDeserializationException(
					$"Error deserializing xml string: {e.Message}", e);
			}
		}


		[NotNull]
		public static IEnumerable<XmlSpecificationReportOptions>
			GetSpecificationReportOptions(
				[CanBeNull] XmlVerificationOptions options,
				[CanBeNull] string defaultTemplatePath)
		{
			const string defaultReportFileName = "qualityspecification.html";

			List<XmlSpecificationReportOptions> result =
				options?.SpecificationReports ?? new List<XmlSpecificationReportOptions>();

			if (result.Count == 0 && ! string.IsNullOrEmpty(defaultTemplatePath))
			{
				result.Add(new XmlSpecificationReportOptions
				           {
					           ReportFileName = defaultReportFileName,
					           TemplatePath = defaultTemplatePath
				           });
			}

			return result;
		}

		[NotNull]
		public static IEnumerable<XmlHtmlReportOptions> GetHtmlReportOptions(
			[CanBeNull] XmlVerificationOptions options,
			[CanBeNull] string defaultReportTemplatePath)
		{
			const string defaultReportFileName = "verification.html";

			List<XmlHtmlReportOptions> result = options?.HtmlReports ??
			                                    new List<XmlHtmlReportOptions>();

			if (result.Count == 0 &&
			    ! string.IsNullOrEmpty(defaultReportTemplatePath))
			{
				result.Add(new XmlHtmlReportOptions
				           {
					           ReportFileName = defaultReportFileName,
					           TemplatePath = defaultReportTemplatePath
				           });
			}

			return result;
		}

		[NotNull]
		public static IEnumerable<XmlIssueMapOptions> GetIssueMapOptions(
			[CanBeNull] XmlVerificationOptions options,
			[CanBeNull] string defaultIssueMapTemplatePath,
			[NotNull] string defaultIssueMapFileName)
		{
			List<XmlIssueMapOptions> result =
				options?.IssueMaps ?? new List<XmlIssueMapOptions>();

			if (result.Count == 0 && StringUtils.IsNotEmpty(defaultIssueMapTemplatePath))
			{
				result.Add(new XmlIssueMapOptions
				           {
					           MxdFileName = defaultIssueMapFileName,
					           TemplatePath = defaultIssueMapTemplatePath
				           });
			}

			return result;
		}

		public static ExceptionObjectStatus GetDefaultExceptionObjectStatus(
			[CanBeNull] XmlVerificationOptions verificationOptions)
		{
			const ExceptionObjectStatus defaultValue = ExceptionObjectStatus.Active;
			if (verificationOptions == null)
			{
				return defaultValue;
			}

			XmlExceptionConfiguration exceptions = verificationOptions.Exceptions;
			return exceptions?.DefaultExceptionObjectStatus ?? defaultValue;
		}

		public static ShapeMatchCriterion GetDefaultShapeMatchCriterion(
			[CanBeNull] XmlVerificationOptions verificationOptions)
		{
			const ShapeMatchCriterion defaultValue = ShapeMatchCriterion.EqualEnvelope;
			if (verificationOptions == null)
			{
				return defaultValue;
			}

			XmlExceptionConfiguration exceptions = verificationOptions.Exceptions;
			return exceptions?.DefaultShapeMatchCriterion ?? defaultValue;
		}

		[CanBeNull]
		public static IWorkspace GetExceptionWorkspace(
			[NotNull] XmlVerificationOptions verificationOptions)
		{
			XmlExceptionConfiguration exceptions = verificationOptions.Exceptions;
			if (exceptions == null)
			{
				return null;
			}

			string dataSource = exceptions.DataSource;
			return StringUtils.IsNullOrEmptyOrBlank(dataSource)
				       ? null
				       : WorkspaceUtils.OpenWorkspace(dataSource.Trim());
		}

		[CanBeNull]
		public static InvolvedObjectsMatchCriteria GetInvolvedObjectMatchCriteria(
			[NotNull] XmlVerificationOptions verificationOptions)
		{
			List<XmlInvolvedObjectsMatchCriterionIgnoredDatasets> ignoredDatasets =
				verificationOptions.Exceptions?.InvolvedObjectsMatchCriteria?.IgnoredDatasets;

			if (ignoredDatasets == null || ignoredDatasets.Count == 0)
			{
				return null;
			}

			return new InvolvedObjectsMatchCriteria(ignoredDatasets);
		}

		[CanBeNull]
		private static FieldConfigurator GetFieldConfigurator(
			[CanBeNull] IEnumerable<XmlFieldOptions> fieldOptionsCollection)
		{
			return fieldOptionsCollection == null
				       ? null
				       : new FieldConfigurator(fieldOptionsCollection);
		}

		[NotNull]
		private static LabelOptions GetLabellingDefinition(
			[CanBeNull] XmlLabelOptions options, bool displayLabels)
		{
			if (options == null)
			{
				return new LabelOptions(displayLabels);
			}

			return new LabelOptions(GetBoolean(options.Visible, displayLabels),
			                        options.Expression,
			                        options.IsExpressionSimple,
			                        options.MinimumScale);
		}

		[NotNull]
		private static DisplayExpression GetDisplayExpression(
			[CanBeNull] XmlDisplayExpressionOptions options, bool showMapTips)
		{
			if (options == null)
			{
				return new DisplayExpression(showMapTips);
			}

			return new DisplayExpression(GetBoolean(options.ShowMapTips, showMapTips),
			                             options.Expression, options.IsExpressionSimple);
		}

		private static bool GetBoolean(TrueFalseDefault trueFalseDefault, bool defaultValue)
		{
			return trueFalseDefault == TrueFalseDefault.@default
				       ? defaultValue
				       : trueFalseDefault == TrueFalseDefault.@true;
		}

		private static esriArcGISVersion GetDocumentVersion(int major, int minor)
		{
			if (major == 10 && minor == 2)
			{
				// 10.2 not supported explicitly --> convert to 10.1
				minor = 1;
			}

			if (major == 9 && minor == 1)
			{
				// 9.1 not supported explicitly --> convert to 9.0
				minor = 0;
			}

			string versionSuffix = minor == 0
				                       ? major == 9
					                         ? "90"
					                         : $"{major}"
				                       : $"{major}{minor}";

			string enumName = $"esriArcGISVersion{versionSuffix}";

			try
			{
				return (esriArcGISVersion) Enum.Parse(typeof(esriArcGISVersion), enumName);
			}
			catch (Exception)
			{
				throw new ArgumentException(
					$"Unsupported ArcGIS version for saving documents: {major}.{minor}");
			}
		}

		[NotNull]
		private static IEnumerable<HtmlDataQualityCategoryOptions>
			GetHtmlReportDataQualityCategoryOptions(
				[CanBeNull] IEnumerable<XmlHtmlReportDataQualityCategoryOptions> categoryOptions)
		{
			return categoryOptions?.Select(o => new HtmlDataQualityCategoryOptions(o.CategoryUuid,
				                               o.IgnoreCategoryLevel,
				                               o.AliasName))
			       ?? new List<HtmlDataQualityCategoryOptions>();
		}
	}
}
