using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA
{
	public interface IVerificationParameters
	{
		double TileSize { get; }

		[CanBeNull]
		AreaOfInterest AreaOfInterest { get; }

		bool WriteDetailedVerificationReport { get; }

		[CanBeNull]
		string VerificationReportPath { get; }

		[CanBeNull]
		string HtmlReportPath { get; }

		[CanBeNull]
		string HtmlReportTemplatePath { get; }

		[CanBeNull]
		string HtmlSpecificationTemplatePath { get; }

		[CanBeNull]
		string MxdDocumentPath { get; }

		[CanBeNull]
		string MxdTemplatePath { get; }

		[CanBeNull]
		string IssueFgdbPath { get; }

		[CanBeNull]
		ISpatialReference IssueFgdbSpatialReference { get; }

		bool CompressIssueFgdb { get; }

		bool UpdateIssuesInVerifiedModelContext { get; }

		bool StoreIssuesOutsidePerimeter { get; }

		bool StoreRelatedGeometryForTableRowIssues { get; }

		bool FilterTableRowsUsingRelatedGeometry { get; }

		bool OverrideAllowedErrors { get; }

		bool DeleteUnusedAllowedErrors { get; }

		bool InvalidateAllowedErrorsIfQualityConditionWasUpdated { get; }

		bool InvalidateAllowedErrorsIfAnyInvolvedObjectChanged { get; }

		bool DeleteObsoleteAllowedErrors { get; }

		ErrorDeletionInPerimeter IssueDeletionInPerimeter { get; }

		bool ForceFullScanForNonContainerTests { get; }

		[NotNull]
		IEnumerable<KeyValuePair<string, string>> ReportProperties { get; }

		bool SaveVerificationStatistics { get; }
	}
}
