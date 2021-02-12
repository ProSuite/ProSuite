using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	public interface IVerificationContext : IModelContext
	{
		[NotNull]
		ICollection<Dataset> GetVerifiedDatasets();

		bool CanWriteIssues { get; }

		bool CanNavigateIssues { get; }

		void UpdateCanWriteIssues();

		[NotNull]
		IEnumerable<INotification> CannotWriteIssuesReasons { get; }

		[NotNull]
		IEnumerable<INotification> CannotNavigateIssuesReasons { get; }

		[NotNull]
		SpatialReferenceDescriptor SpatialReferenceDescriptor { get; }

		[CanBeNull]
		ErrorLineDataset LineIssueDataset { get; }

		[CanBeNull]
		ErrorPolygonDataset PolygonIssueDataset { get; }

		[CanBeNull]
		ErrorMultipointDataset MultipointIssueDataset { get; }

		[CanBeNull]
		ErrorMultiPatchDataset MultiPatchIssueDataset { get; }

		[CanBeNull]
		ErrorTableDataset NoGeometryIssueDataset { get; }
	}
}
