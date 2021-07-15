using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.Workflow.WorkspaceFilters
{
	public class WorkspaceMatchCriterionConjunction : IWorkspaceMatchCriterion
	{
		private readonly List<IWorkspaceMatchCriterion> _criteria =
			new List<IWorkspaceMatchCriterion>();

		public void Add([NotNull] IWorkspaceMatchCriterion criterion)
		{
			_criteria.Add(criterion);
		}

		public bool IsSatisfied(IWorkspace workspace, out string reason)
		{
			foreach (IWorkspaceMatchCriterion criterion in _criteria)
			{
				if (! criterion.IsSatisfied(workspace, out reason))
				{
					return false;
				}
			}

			reason = string.Empty;
			return true;
		}
	}
}
