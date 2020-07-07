using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.Commons.UI.Finder
{
	public interface IFinder<T> where T : class
	{
		[CanBeNull]
		T ShowDialog([NotNull] IWin32Window owner,
		             [NotNull] IList<T> list,
		             params ColumnDescriptor[] columnDescriptors);

		[CanBeNull]
		T ShowDialog([NotNull] IWin32Window owner,
		             [NotNull] IList<T> list,
		             [CanBeNull] IList<ColumnDescriptor> columnDescriptors);

		[CanBeNull]
		IList<T> ShowDialog([NotNull] IWin32Window owner,
		                    [NotNull] IList<T> list,
		                    bool allowMultiSelection,
		                    [CanBeNull] IList<ColumnDescriptor> columnDescriptors,
		                    [CanBeNull] string filterSettingsContext = null);

		[CanBeNull]
		IList<T> ShowDialog([NotNull] IWin32Window owner,
		                    [NotNull] IList<T> list,
		                    bool allowMultiSelection,
		                    params ColumnDescriptor[] columnDescriptors);

		[CanBeNull]
		IList<T> ShowDialog([NotNull] IWin32Window owner,
		                    [NotNull] IList<T> list,
		                    bool allowMultiSelection,
		                    [CanBeNull] string filterSettingsContext,
		                    params ColumnDescriptor[] columnDescriptors);

		[CanBeNull]
		IList<T> ShowDialog([NotNull] IWin32Window owner,
		                    [NotNull] IEnumerable<FinderQuery<T>> finderQueries,
		                    bool allowMultiSelection,
		                    [CanBeNull] IList<ColumnDescriptor> columnDescriptors,
		                    string filterSettingsContext = null);

		[NotNull]
		FinderForm<T> CreateForm([NotNull] IList<T> list,
		                         [CanBeNull] IList<ColumnDescriptor> columnDescriptors,
		                         bool allowMultiSelection,
		                         [CanBeNull] string filterSettingsContext = null);

		[NotNull]
		FinderForm<T> CreateForm([NotNull] IEnumerable<FinderQuery<T>> finderQueries,
		                         [CanBeNull] IList<ColumnDescriptor> columnDescriptors,
		                         bool allowMultiSelection,
		                         [CanBeNull] string filterSettingsContext = null);
	}
}
