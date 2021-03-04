using System;
using ArcGIS.Desktop.Framework.Controls;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI
{
	internal class WorkListObserver : IWorkListObserver, IDisposable
	{
		[CanBeNull] private ProWindow _view;

		public WorkListObserver([NotNull] IWorkList worklist)
		{
			Assert.ArgumentNotNull(worklist, nameof(worklist));

			Worklist = worklist;
		}

		[NotNull]
		public IWorkList Worklist { get; private set; }

		public void Set(IWorkList worklist)
		{
			Worklist = worklist;
		}

		public bool CloseView()
		{
			if (_view == null)
			{
				return true;
			}

			RunOnUIThread(() => { _view.Close(); });

			return true;
		}

		public void Show()
		{
			RunOnUIThread(() =>
			{
				_view = WorkListViewFactory.CreateView(Worklist);

				_view.Show();
			});
		}

		//Utility method to consolidate UI update logic
		private static void RunOnUIThread([NotNull] Action action)
		{
			if (System.Windows.Application.Current.Dispatcher.CheckAccess())
			{
				//No invoke needed
				action();
			}
			else
			{
				//We are not on the UI
				System.Windows.Application.Current.Dispatcher.BeginInvoke(action);
			}
		}

		public void Dispose()
		{
			Worklist.Dispose();

			(_view as IDisposable)?.Dispose();
		}
	}
}
