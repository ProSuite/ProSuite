using System.Collections.Generic;
using System.Linq;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class IssueWorkListVm : WorkListViewModelBase
	{
		private WorkListView _view;
		private readonly bool _hasDetailSection;
		private WorkItemVmBase _currentWorkItem;
		private IList<InvolvedTableVm> _involvedObjects;
		private string _qualityCondition;

		public IssueWorkListVm(IWorkList workList)
		{
			CurrentWorkList = workList;
			CurrentWorkList.GoNext();
			CurrentWorkItem = new IssueWorkItemVm(CurrentWorkList.Current as IssueItem);
			_hasDetailSection = true;
		}

		public override bool HasDetailSection
		{
			get => _hasDetailSection;
		}

		public override WorkItemVmBase CurrentWorkItem
		{
			get => new IssueWorkItemVm(CurrentWorkList.Current as IssueItem);
			set
			{
				SetProperty(ref _currentWorkItem, value, () => CurrentWorkItem);
				Status = CurrentWorkItem.Status;
				Visited = CurrentWorkItem.Visited;
				var issueWorkItemVm = CurrentWorkItem as IssueWorkItemVm;
				InvolvedObjects = issueWorkItemVm.IssueItem.InIssueInvolvedTables
				                                 .Select(table => new InvolvedTableVm(table))
				                                 .ToList();
				QualityCondition = issueWorkItemVm.QualityCondition;
				CurrentIndex = CurrentWorkList.CurrentIndex;
				Count = GetCount();
			}
		}

		public string QualityCondition
		{
			get
			{
				var issueItem = CurrentWorkItem as IssueWorkItemVm;
				return issueItem.QualityCondition;
			}
			set { SetProperty(ref _qualityCondition, value, () => QualityCondition); }
		}

		public IList<InvolvedTableVm> InvolvedObjects
		{
			get
			{
				var currentIssue = CurrentWorkItem as IssueWorkItemVm;
				//var tables = IssueUtils.ParseInvolvedTables(IssueItem.InvolvedObjects);
				//return tables.Select(involvedTable => new InvolvedTableVm(involvedTable)).ToList();
				return currentIssue.IssueItem.InIssueInvolvedTables
				                   .Select(table => new InvolvedTableVm(table)).ToList();
			}
			set { SetProperty(ref _involvedObjects, value, () => InvolvedObjects); }
		}
	}
}
