using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Progress;
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
		QualitySpecificationReference CurrentQualitySpecification { get; set; }

		/// <summary>
		/// The list of applicable quality specifications for the current environment.
		/// </summary>
		[NotNull]
		IList<QualitySpecificationReference> QualitySpecifications { get; }

		/// <summary>
		/// Refresh the list of quality verifications.
		/// </summary>
		void RefreshQualitySpecifications();

		/// <summary>
		/// Occurs after the list of quality specifications was refreshed.
		/// </summary>
		event EventHandler QualitySpecificationsRefreshed;

		string BackendDisplayName { get; }

		Task<ServiceCallStatus> VerifyExtent([NotNull] Envelope extent,
		                                     [NotNull] QualityVerificationProgressTracker progress,
		                                     string resultsPath);
	}
}
