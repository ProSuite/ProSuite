using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.Commons.UI.Finder
{
	public class Finder<T> : IFinder<T> where T : class
	{
		#region IFinder<T> Members

		public T ShowDialog(IWin32Window owner, IList<T> list,
		                    params ColumnDescriptor[] columnDescriptors)
		{
			return ShowDialog(owner, list,
			                  new List<ColumnDescriptor>(columnDescriptors));
		}

		public T ShowDialog(IWin32Window owner, IList<T> list,
		                    IList<ColumnDescriptor> columnDescriptors)
		{
			return GetUniqueSelection(ShowDialog(owner, list, false, columnDescriptors));
		}

		public IList<T> ShowDialog(IWin32Window owner, IList<T> list,
		                           bool allowMultiSelection,
		                           params ColumnDescriptor[] columnDescriptors)
		{
			return ShowDialog(owner, list, allowMultiSelection,
			                  columnDescriptors.ToList());
		}

		public IList<T> ShowDialog(IWin32Window owner, IList<T> list,
		                           bool allowMultiSelection,
		                           string filterSettingsContext,
		                           params ColumnDescriptor[] columnDescriptors)
		{
			return ShowDialog(owner, list, allowMultiSelection,
			                  columnDescriptors.ToList(),
			                  filterSettingsContext);
		}

		public IList<T> ShowDialog(IWin32Window owner, IList<T> list,
		                           bool allowMultiSelection,
		                           IList<ColumnDescriptor> columnDescriptors,
		                           string filterSettingsContext = null)
		{
			Assert.ArgumentNotNull(owner, nameof(owner));
			Assert.ArgumentNotNull(list, nameof(list));

			FinderForm<T> form = CreateForm(list, columnDescriptors, allowMultiSelection,
			                                filterSettingsContext);

			DialogResult result = form.ShowDialog(owner);

			return result == DialogResult.OK
				       ? form.Selection
				       : null;
		}

		public IList<T> ShowDialog(IWin32Window owner,
		                           IEnumerable<FinderQuery<T>> finderQueries,
		                           bool allowMultiSelection,
		                           string filterSettingsContext = null,
		                           params ColumnDescriptor[] columnDescriptors)
		{
			return ShowDialog(owner, finderQueries, allowMultiSelection,
			                  columnDescriptors.ToList(), filterSettingsContext);
		}

		public IList<T> ShowDialog(IWin32Window owner,
		                           IEnumerable<FinderQuery<T>> finderQueries,
		                           bool allowMultiSelection,
		                           IList<ColumnDescriptor> columnDescriptors,
		                           string filterSettingsContext = null)
		{
			Assert.ArgumentNotNull(owner, nameof(owner));
			Assert.ArgumentNotNull(finderQueries, nameof(finderQueries));

			FinderForm<T> form = CreateForm(finderQueries, columnDescriptors,
			                                allowMultiSelection,
			                                filterSettingsContext);

			DialogResult result = form.ShowDialog(owner);

			return result == DialogResult.OK
				       ? form.Selection
				       : null;
		}

		public FinderForm<T> CreateForm(IList<T> list,
		                                IList<ColumnDescriptor> columnDescriptors,
		                                bool allowMultiSelection,
		                                string filterSettingsContext = null)
		{
			Assert.ArgumentNotNull(list, nameof(list));

			var form = new FinderForm<T>(GetColumnDescriptors(columnDescriptors),
			                             list, allowMultiSelection,
			                             filterSettingsContext: filterSettingsContext);
			new FinderPresenter<T>(form);

			return form;
		}

		public FinderForm<T> CreateForm(IEnumerable<FinderQuery<T>> finderQueries,
		                                IList<ColumnDescriptor> columnDescriptors,
		                                bool allowMultiSelection,
		                                string filterSettingsContext = null)
		{
			Assert.ArgumentNotNull(finderQueries, nameof(finderQueries));

			var form = new FinderForm<T>(GetColumnDescriptors(columnDescriptors),
			                             finderQueries, allowMultiSelection,
			                             filterSettingsContext: filterSettingsContext);
			new FinderPresenter<T>(form);

			return form;
		}

		#endregion

		[CanBeNull]
		private static T GetUniqueSelection([CanBeNull] IList<T> selection)
		{
			if (selection == null)
			{
				// cancelled, return null
				return null;
			}

			if (selection.Count > 1)
			{
				throw new InvalidOperationException("More than one row selected");
			}

			return selection.Count == 0
				       ? null
				       : selection[0];
		}

		[NotNull]
		private static IEnumerable<ColumnDescriptor> GetColumnDescriptors(
			[CanBeNull] ICollection<ColumnDescriptor> columnDescriptors)
		{
			return columnDescriptors == null || columnDescriptors.Count == 0
				       ? ColumnDescriptor.GetColumns<T>()
				       : columnDescriptors;
		}
	}
}
