using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AGP.QA
{
	public interface IQualityVerificationEnvironment
	{
		/// <summary>
		/// Gets or sets the current quality specification.
		/// </summary>
		/// <value>The current quality specification.</value>
		[CanBeNull]
		QualitySpecificationRef CurrentQualitySpecification { get; set; }

		/// <summary>
		/// The list of applicable quality specifications for the current environment.
		/// </summary>
		[NotNull]
		IList<QualitySpecificationRef> QualitySpecifications { get; }

		/// <summary>
		/// Refresh the list of quality verifications.
		/// </summary>
		void RefreshQualitySpecifications();

		/// <summary>
		/// Occurs after the list of quality specifications was refreshed.
		/// </summary>
		event EventHandler QualitySpecificationsRefreshed;
	}
}
