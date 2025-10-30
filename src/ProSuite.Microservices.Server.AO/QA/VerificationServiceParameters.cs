using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class VerificationServiceParameters : IVerificationParameters
	{
		public VerificationServiceParameters(
			[NotNull] string verificationContextType,
			[NotNull] string verificationContextName,
			double tileSize)
		{
			Assert.ArgumentNotNullOrEmpty(
				verificationContextType,
				nameof(verificationContextType));
			Assert.ArgumentNotNullOrEmpty(
				verificationContextName,
				nameof(verificationContextName));

			VerificationContextType = verificationContextType;
			VerificationContextName = verificationContextName;

			TileSize = tileSize;

			UpdateIssuesInVerifiedModelContext = false;

			IssueDeletionInPerimeter = ErrorDeletionInPerimeter.VerifiedQualityConditions;
		}

		#region Implementation of IVerificationParameters

		public string VerificationContextType { get; private set; }

		public string VerificationContextName { get; private set; }

		[GreaterThanZero]
		public double TileSize { get; set; }

		public AreaOfInterest AreaOfInterest { get; set; }

		/// <summary>
		/// Write a detailed error report (XML)
		/// </summary>
		public bool WriteDetailedVerificationReport { get; set; }

		/// <summary>
		/// The path for the error reports.
		/// </summary>
		public string VerificationReportPath { get; set; }

		/// <summary>
		/// Path for the (external) issue file geodatabase.
		/// </summary>
		public string IssueFgdbPath { get; set; }

		public ISpatialReference IssueFgdbSpatialReference { get; set; }

		// Do not compress file geodatabases (better zip them) because
		// - it can result in file locks
		// - it has Geoprocessor dependency
		public bool CompressIssueFgdb => false;

		public string HtmlReportPath { get; set; }

		public string HtmlReportTemplatePath { get; set; }

		public string HtmlSpecificationTemplatePath { get; set; }

		public string MxdDocumentPath => null;

		public string MxdTemplatePath => null;

		/// <summary>
		/// Store errors with geometry of related features
		/// </summary>
		public bool StoreRelatedGeometryForTableRowIssues { get; set; }

		/// <summary>
		/// Filter to perimeter by geometry of related features.
		/// </summary>
		public bool FilterTableRowsUsingRelatedGeometry { get; set; }

		/// <summary>
		/// Report errors as relevant despite their geometry being outside of the perimeter.
		/// </summary>
		public bool StoreIssuesOutsidePerimeter { get; set; }

		#region Do not update issues verified model context

		public bool UpdateIssuesInVerifiedModelContext { get; set; }

		public ErrorDeletionInPerimeter IssueDeletionInPerimeter { get; set; }

		// The following properties in the region are irrelevant for GP verification
		public bool DeleteObsoleteAllowedErrors => false;

		public bool InvalidateAllowedErrorsIfQualityConditionWasUpdated { get; set; }

		public bool InvalidateAllowedErrorsIfAnyInvolvedObjectChanged { get; set; }

		public bool DeleteUnusedAllowedErrors => false;

		#endregion

		#region Advanced

		/// <summary>
		/// Process all rows for non-container tests.
		/// </summary>
		public bool ForceFullScanForNonContainerTests { get; set; }

		/// <summary>
		/// Override allowed errors (report all errors).
		/// </summary>
		public bool OverrideAllowedErrors { get; set; }

		#endregion

		public IEnumerable<KeyValuePair<string, string>> ReportProperties { get; } =
			new List<KeyValuePair<string, string>>();

		public bool SaveVerificationStatistics { get; set; }

		#endregion

		//
		//public bool StoreErrorsInFileGeodatabase { get; set; }

		//[CanBeNull]
		//
		//public string OutputDirectoryFormat { get; set; }

		//
		//public bool ErrorDeletionInPerimeterEnabled => UpdateIssuesInVerifiedModelContext;

		//
		//public bool DeleteObsoleteAllowedErrorsEnabled =>
		//	UpdateIssuesInVerifiedModelContext;

		//
		//public bool InvalidateAllowedErrorsIfQualityConditionWasUpdatedEnabled
		//	=> UpdateIssuesInVerifiedModelContext && DeleteObsoleteAllowedErrors;

		//
		//public bool InvalidateAllowedErrorsIfAnyInvolvedObjectChangedEnabled
		//	=> UpdateIssuesInVerifiedModelContext && DeleteObsoleteAllowedErrors;

		//
		//public bool DeleteUnusedAllowedErrorsEnabled =>
		//	UpdateIssuesInVerifiedModelContext;

		//
		//public bool FilterTableRowsUsingRelatedGeometryEnabled { get; set; }
	}
}
