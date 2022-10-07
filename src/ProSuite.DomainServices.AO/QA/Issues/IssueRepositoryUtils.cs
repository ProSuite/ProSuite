using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public static class IssueRepositoryUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static IEnumerable<IObjectClass> GetIssueObjectClasses(
			[NotNull] IFeatureWorkspace issueRepositoryWorkspace)
		{
			Assert.ArgumentNotNull(issueRepositoryWorkspace, nameof(issueRepositoryWorkspace));

			foreach (string objectClassName in IssueDatasetUtils.ObjectClassNames)
			{
				IObjectClass objectClass;
				try
				{
					objectClass = DatasetUtils.OpenObjectClass(issueRepositoryWorkspace,
					                                           objectClassName);
				}
				catch (COMException e)
				{
					_msg.DebugFormat("Unable to open object class {0} from {1}: {2}",
					                 objectClassName,
					                 WorkspaceUtils.GetWorkspaceDisplayText(
						                 (IWorkspace) issueRepositoryWorkspace),
					                 e.Message);
					continue;
				}

				yield return objectClass;
			}
		}
	}
}
