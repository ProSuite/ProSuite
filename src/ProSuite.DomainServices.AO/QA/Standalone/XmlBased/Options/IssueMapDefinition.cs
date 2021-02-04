using System;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public class IssueMapDefinition
	{
		[CLSCompliant(false)]
		public IssueMapDefinition(
			[CanBeNull] string templatePath,
			[NotNull] string fileName,
			bool listLayersByAffectedComponent,
			[NotNull] LabelOptions issueLabelOptions,
			[NotNull] LabelOptions exceptionLabelOptions,
			[NotNull] DisplayExpression issueDisplayExpression,
			[NotNull] DisplayExpression exceptionDisplayExpression,
			double verifiedFeaturesMinimumScale,
			IssueLayersGroupBy issueLayersGroupBy = IssueLayersGroupBy.IssueType,
			esriArcGISVersion documentVersion = esriArcGISVersion.esriArcGISVersionCurrent,
			[CanBeNull] FieldConfigurator issueFieldConfigurator = null,
			[CanBeNull] FieldConfigurator exceptionFieldConfigurator = null)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));
			Assert.ArgumentNotNull(issueLabelOptions, nameof(issueLabelOptions));
			Assert.ArgumentNotNull(exceptionLabelOptions, nameof(exceptionLabelOptions));
			Assert.ArgumentNotNull(issueDisplayExpression, nameof(issueDisplayExpression));
			Assert.ArgumentNotNull(exceptionDisplayExpression,
			                       nameof(exceptionDisplayExpression));

			TemplatePath = templatePath;
			FileName = fileName;
			SkipEmptyIssueDatasets = true;
			SkipEmptyExceptionDatasets = true;
			ListLayersByAffectedComponent = listLayersByAffectedComponent;
			IssueLabelOptions = issueLabelOptions;
			ExceptionLabelOptions = exceptionLabelOptions;
			IssueDisplayExpression = issueDisplayExpression;
			ExceptionDisplayExpression = exceptionDisplayExpression;
			VerifiedFeaturesMinimumScale = verifiedFeaturesMinimumScale;
			IssueLayersGroupBy = issueLayersGroupBy;
			DocumentVersion = documentVersion;
			IssueFieldConfigurator = issueFieldConfigurator;
			ExceptionFieldConfigurator = exceptionFieldConfigurator;
		}

		[CanBeNull]
		public string TemplatePath { get; }

		[NotNull]
		public string FileName { get; }

		public bool SkipEmptyIssueDatasets { get; }

		public bool SkipEmptyExceptionDatasets { get; }

		public bool ListLayersByAffectedComponent { get; }

		public IssueLayersGroupBy IssueLayersGroupBy { get; }

		public double VerifiedFeaturesMinimumScale { get; }

		[CLSCompliant(false)]
		public esriArcGISVersion DocumentVersion { get; }

		[NotNull]
		public LabelOptions IssueLabelOptions { get; }

		[NotNull]
		public DisplayExpression IssueDisplayExpression { get; }

		[NotNull]
		public LabelOptions ExceptionLabelOptions { get; }

		[NotNull]
		public DisplayExpression ExceptionDisplayExpression { get; }

		[CanBeNull]
		public FieldConfigurator IssueFieldConfigurator { get; }

		[CanBeNull]
		public FieldConfigurator ExceptionFieldConfigurator { get; }
	}
}
