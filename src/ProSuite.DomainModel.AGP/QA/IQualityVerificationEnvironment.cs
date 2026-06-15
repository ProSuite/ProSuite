using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;

namespace ProSuite.DomainModel.AGP.QA;

public interface IQualityVerificationEnvironment
{
	/// <summary>
	/// The name of the DDX environment associated with this verification environment. This is
	/// relevant for multi-ddx setups on the server side.
	/// </summary>
	[CanBeNull]
	string DdxEnvironmentName { get; }

	/// <summary>
	/// Gets or sets the current quality specification.
	/// </summary>
	/// <value>The current quality specification.</value>
	[CanBeNull]
	IQualitySpecificationReference CurrentQualitySpecificationReference { get; set; }

	/// <summary>
	/// The list of applicable quality specification references for the current environment.
	/// </summary>
	[NotNull]
	IList<IQualitySpecificationReference> QualitySpecificationReferences { get; }

	/// <summary>
	/// Refresh the list of quality verifications.
	/// </summary>
	void RefreshQualitySpecificationReferences();

	/// <summary>
	/// Loads the full specification of the currently selected specification reference.
	/// </summary>
	/// <returns></returns>
	Task<QualitySpecification> GetCurrentQualitySpecification();

	/// <summary>
	/// Loads the full specification for the specified data dictionary id.
	/// </summary>
	/// <param name="ddxId"></param>
	/// <returns></returns>
	Task<QualitySpecification> GetQualitySpecification(int ddxId);

	/// <summary>
	/// Sets the customized quality specification version of the current specification.
	/// </summary>
	/// <param name="customSpecification"></param>
	void SetCustomSpecification(QualitySpecification customSpecification);

	/// <summary>
	/// Occurs after the list of quality specifications was refreshed.
	/// </summary>
	event EventHandler QualitySpecificationsRefreshed;

	/// <summary>
	/// Gets or sets the last verification perimeter.
	/// </summary>
	/// <value>The last verification perimeter.</value>
	[CanBeNull]
	Geometry LastVerificationPerimeter { get; set; }

	/// <summary>
	/// Gets or sets the date for filtering issues by the 'Latest verification'.
	/// </summary>
	DateTime? LastVerificationFilterStartDate { get; set; }

	/// <summary>
	/// Display name of the backend, such as 'localhost'
	/// </summary>
	[CanBeNull]
	string BackendDisplayName { get; }

	IQualityConditionProvider ConditionProvider { get; }

	/// <summary>
	/// Verifies the provided perimeter or the full extent if no perimeter is provided.
	/// </summary>
	/// <param name="perimeter">The perimeter. Null means 'full extent'.</param>
	/// <param name="progress"></param>
	/// <param name="perimeterDisplayName">The display name for the provided perimeter.</param>
	/// <param name="resultsPath"></param>
	/// <returns></returns>
	Task<ServiceCallStatus> VerifyPerimeter(
		[CanBeNull] Geometry perimeter,
		[NotNull] QualityVerificationProgressTracker progress,
		[NotNull] string perimeterDisplayName,
		string resultsPath);

	Task<ServiceCallStatus> VerifySelection(
		IList<Row> objectsToVerify,
		[CanBeNull] Geometry perimeter,
		QualityVerificationProgressTracker progress,
		[CanBeNull] string resultsPath);

	/// <summary>
	/// Whether the current environment supports storing issues in the central issue feature
	/// classes of the production model.
	/// </summary>
	/// <returns></returns>
	bool CanSaveIssuesInProductionModel(out IIssueStoreContext issueStoreContext);

	/// <summary>
	/// Stores the issues in the central issue feature classes of the production model.
	/// </summary>
	/// <param name="verificationResult"></param>
	/// <param name="errorDeletion"></param>
	/// <param name="updateLatestTestDate"></param>
	/// <returns></returns>
	Task<int> SaveBackgroundVerificationIssues(
		IQualityVerificationResult verificationResult,
		ErrorDeletionInPerimeter errorDeletion,
		bool updateLatestTestDate);

	ITransformerQueryService GetTransformerQueryService();

	/// <summary>
	/// Disable the current environment. All specifications are cleared.
	/// </summary>
	void Disable();
}
