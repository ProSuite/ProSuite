using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Finder
{
	public class FinderPresenter<T> : IFinderObserver where T : class
	{
		[NotNull] private readonly IFinderView<T> _view;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="FinderPresenter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="view">The view.</param>
		public FinderPresenter([NotNull] IFinderView<T> view)
		{
			Assert.ArgumentNotNull(view, nameof(view));

			_view = view;
			_view.Observer = this;
		}

		#endregion

		#region IFinderObserver Members

		public void CancelClicked()
		{
			_view.Selection = null;

			_view.DialogResult = DialogResult.Cancel;
		}

		public void OKClicked()
		{
			_view.Selection = new List<T>(_view.GetSelection());

			_view.DialogResult = DialogResult.OK;
		}

		public void SelectionChanged()
		{
			UpdateAppearance();
		}

		public void ListDoubleClicked()
		{
			if (_view.HasSelection)
			{
				OKClicked();
			}
		}

		public void ViewLoaded()
		{
			UpdateAppearance();
		}

		#endregion

		private void UpdateAppearance()
		{
			_view.OKEnabled = _view.HasSelection;

			string format = _view.TotalCount == 1
				                ? "{0} of {1} row selected"
				                : "{0} of {1} rows selected";

			_view.StatusMessage = string.Format(format,
			                                    _view.SelectionCount,
			                                    _view.TotalCount);
		}
	}
}
