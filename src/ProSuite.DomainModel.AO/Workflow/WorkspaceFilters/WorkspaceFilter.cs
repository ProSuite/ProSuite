using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.Workflow.WorkspaceFilters
{
	public class WorkspaceFilter : IWorkspaceFilter
	{
		private readonly List<IWorkspaceMatchCriterion> _restrictions;

		public WorkspaceFilter(
			[NotNull] IEnumerable<IWorkspaceMatchCriterion> restrictions)
		{
			Assert.ArgumentNotNull(restrictions, nameof(restrictions));

			_restrictions = new List<IWorkspaceMatchCriterion>(restrictions);
		}

		public bool Ignore(IWorkspace workspace, out string reason)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			if (_restrictions.Count == 0)
			{
				reason = "No workspace inclusion criteria, included by default";
				return false;
			}

			foreach (IWorkspaceMatchCriterion criterion in _restrictions)
			{
				if (criterion.IsSatisfied(workspace, out reason))
				{
					return false;
				}
			}

			reason = "None of the defined workspace inclusion criteria is satisfied";
			return true;
		}
	}
}
