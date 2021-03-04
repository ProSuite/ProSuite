using System;
using ArcGIS.Desktop.Framework.Controls;
using ProSuite.AGP.Solution.WorkListUI.Views;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public static class WorkListViewFactory
	{
		[NotNull]
		public static ProWindow CreateView([NotNull] IWorkList workList)
		{
			Assert.ArgumentNotNull(workList, nameof(workList));

			switch (workList)
			{
				case SelectionWorkList _:
					return new WorkListView(new SelectionWorkListVm(workList));
				case IssueWorkList _:
					return new IssueWorkListView(new IssueWorkListVm(workList));
				default:
					throw new ArgumentOutOfRangeException(
						$"Unkown work list type {workList.GetType()}");
			}
		}
	}
}
