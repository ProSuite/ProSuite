using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;

namespace ProSuite.DomainModel.AGP.QA
{
	public interface IQualityVerificationEnvironment
	{
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
		/// Display name of the backend, such as 'localhost'
		/// </summary>
		[CanBeNull]
		string BackendDisplayName { get; }

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
	}
}
