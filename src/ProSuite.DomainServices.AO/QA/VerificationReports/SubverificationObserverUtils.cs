using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.IssuePersistence;

namespace ProSuite.DomainServices.AO.QA.VerificationReports
{
	public static class SubverificationObserverUtils
	{
		public const string SubverificationFeatureClassName = "SubverificationProgress";

		[CanBeNull]
		public static SubverificationObserver GetPorgressRepository(
			[NotNull] string directoryFullPath,
			[CanBeNull] string gdbName,
			[CanBeNull] ISpatialReference spatialReference,
			IssueRepositoryType issueRepositoryType)
		{
			if (string.IsNullOrEmpty(directoryFullPath))
			{
				return null;
			}

			if (string.IsNullOrWhiteSpace(gdbName))
			{
				return null;
			}

			IFeatureWorkspace workspace =
				ExternalIssueRepositoryUtils.GetOrCreateDatabase(
					directoryFullPath, gdbName, issueRepositoryType);

			if (workspace == null)
			{
				return null;
			}

			return new SubverificationObserver(workspace, SubverificationFeatureClassName,
			                                   spatialReference);
		}
	}
}
