using System;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Controls;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI
{
	internal class WorkListObserver : IWorkListObserver, IDisposable
	{
		[NotNull] private readonly IWorkList _worklist;
		[CanBeNull] private ProWindow _view;

		public WorkListObserver([NotNull] IWorkList worklist)
		{
			Assert.ArgumentNotNull(worklist, nameof(worklist));

			_worklist = worklist;
		}

		public void Dispose()
		{
			_worklist.Dispose();

			(_view as IDisposable)?.Dispose();
		}

		public ProWindow View => _view;

		public void Close()
		{
			if (_view == null)
			{
				return;
			}

			ViewUtils.RunOnUIThread(() => { _view.Close(); });
		}

		public void Show(string title)
		{
			if (_view != null)
			{
				if (! string.IsNullOrEmpty(title))
				{
					_view.Title = title;
				}

				// show work list button clicked > we're already on UI thread
				_view.Activate();
				return;
			}

			ViewUtils.RunOnUIThread(() =>
			{
				_view = WorkListViewFactory.CreateView(_worklist);

				_view.Owner = FrameworkApplication.Current.MainWindow;

				if (! string.IsNullOrEmpty(title))
				{
					_view.Title = title;
				}

				_view.Closed += _view_Closed;
				
				_view.Show();
			});
		}

		private void _view_Closed(object sender, EventArgs e)
		{
			if (_view == null)
			{
				return;
			}

			// in WPF a closed window cannot be re-openend again
			_view.Closed -= _view_Closed;
			_view = null;
		}
	}
}
