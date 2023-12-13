using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.IssuePersistence;

namespace ProSuite.DomainServices.AO.QA.VerificationReports
{
	public static class SubVerificationObserverUtils
	{
		public const string SubVerificationFeatureClassName = "SubverificationProgress";

		[CanBeNull]
		public static ISubVerificationObserver GetProgressRepository(
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

			if (spatialReference == null)
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

			var result = new SubVerificationObserver(workspace, SubVerificationFeatureClassName,
			                                         spatialReference);

			Marshal.ReleaseComObject(workspace);

			return result;
		}
	}
}
