using System.Collections.Generic;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class IssueWorkListVm : WorkListViewModelBase
	{
		private WorkItemVmBase _currentWorkItem;
		private string _qualityCondition;
		private List<InvolvedObjectRow> _involvedObjectRows;
		private string _errorDescription;

		public IssueWorkListVm(IWorkList workList)
		{
			CurrentWorkList = workList;
			CurrentWorkList.GoNext();
			CurrentWorkItem = new IssueWorkItemVm(CurrentWorkList.Current as IssueItem);
		}

		public override WorkItemVmBase CurrentWorkItem
		{
			get => new IssueWorkItemVm(CurrentWorkList.Current as IssueItem);
			set
			{
				SetProperty(ref _currentWorkItem, value, () => CurrentWorkItem);
				Status = CurrentWorkItem.Status;
				Visited = CurrentWorkItem.Visited;
				InvolvedObjectRows = CompileInvolvedRows();
				if (CurrentWorkItem is IssueWorkItemVm issueWorkItemVm)
				{
					QualityCondition = issueWorkItemVm.QualityCondition;
					ErrorDescription = issueWorkItemVm.ErrorDescription;
				}

				CurrentIndex = CurrentWorkList.CurrentIndex;
				Count = GetCount();
			}
		}

		public string QualityCondition
		{
			get
			{
				if (CurrentWorkItem is IssueWorkItemVm issueWorkItemVm)
				{
					return issueWorkItemVm.QualityCondition;
				}
				else return string.Empty;
			}
			set { SetProperty(ref _qualityCondition, value, () => QualityCondition); }
		}

		public string ErrorDescription
		{
			get
			{
				if (CurrentWorkItem is IssueWorkItemVm issueWorkItemVm)
				{
					return issueWorkItemVm.ErrorDescription;
				}
				else return string.Empty;
			}
			set { SetProperty(ref _errorDescription, value, () => QualityCondition); }
		}

		public List<InvolvedObjectRow> InvolvedObjectRows
		{
			get => CompileInvolvedRows();
			set { SetProperty(ref _involvedObjectRows, value, () => InvolvedObjectRows); }
		}

		private List<InvolvedObjectRow> CompileInvolvedRows()
		{
			var issueWorkItemVm = CurrentWorkItem as IssueWorkItemVm;
			List<InvolvedObjectRow> involvedRows = new List<InvolvedObjectRow>();

			if (issueWorkItemVm == null)
			{
				return involvedRows;
			}

			foreach (var table in issueWorkItemVm.IssueItem.InIssueInvolvedTables)
			{
				involvedRows.AddRange(InvolvedObjectRow.CreateObjectRows(table));
			}

			return involvedRows;
		}
	}
}
