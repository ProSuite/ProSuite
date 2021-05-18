using System;
using ArcGIS.Desktop.Framework.Controls;
using ProSuite.AGP.Solution.ProjectItem;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.WorkListUI
{
	internal class WorkListObserver : IWorkListObserver, IDisposable
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly IWorkList _worklist;
		[NotNull] private readonly WorklistItem _item;
		[CanBeNull] private ProWindow _view;

		public WorkListObserver(IWorkList worklist, WorklistItem item)
		{
			Assert.ArgumentNotNull(worklist, nameof(worklist));

			_worklist = worklist;
			_item = item;
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
			_item.DisableDelete(true);
			_item.DisableRename(true);

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

				_view.Owner = System.Windows.Application.Current.MainWindow;

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
			// an exception here can crash Pro
			ViewUtils.Try(() =>
			{
				if (_view == null)
				{
					return;
				}

				// in WPF a closed window cannot be re-openend again
				_view.Closed -= _view_Closed;
				_view = null;
				_item.DisableDelete(false);
				_item.DisableRename(false);
				// todo daro
				// set item to null?
			}, _msg);
		}
	}
}
