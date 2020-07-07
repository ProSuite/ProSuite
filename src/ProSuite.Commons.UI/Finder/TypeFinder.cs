using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Env;

namespace ProSuite.Commons.UI.Finder
{
	public static class TypeFinder
	{
		[CanBeNull]
		public static IList<Type> ShowDialog<T>([CanBeNull] IWin32Window owner,
		                                        bool allowMultiSelection)
		{
			const bool includeObsoleteTypes = false;
			return ShowDialog<T>(owner, allowMultiSelection, null, null, includeObsoleteTypes);
		}

		[CanBeNull]
		public static IList<Type> ShowDialog<T>([CanBeNull] IWin32Window owner,
		                                        bool allowMultiSelection,
		                                        [CanBeNull] Type initialSelection)
		{
			const bool includeObsoleteTypes = false;
			return ShowDialog<T>(owner, allowMultiSelection, initialSelection, null,
			                     includeObsoleteTypes);
		}

		[CanBeNull]
		public static IList<Type> ShowDialog<T>([CanBeNull] IWin32Window owner,
		                                        bool allowMultiSelection,
		                                        [CanBeNull] Predicate<Type> match)
		{
			const bool includeObsoleteTypes = false;
			return ShowDialog<T>(owner, allowMultiSelection, null, match, includeObsoleteTypes);
		}

		[CanBeNull]
		public static IList<Type> ShowDialog<T>([CanBeNull] IWin32Window owner,
		                                        bool allowMultiSelection,
		                                        [CanBeNull] Type initialSelection,
		                                        [CanBeNull] Predicate<Type> match)
		{
			const bool includeObsoleteTypes = false;
			return ShowDialog<T>(owner, allowMultiSelection, initialSelection, match,
			                     includeObsoleteTypes);
		}

		[CanBeNull]
		public static IList<Type> ShowDialog<T>([CanBeNull] IWin32Window owner,
		                                        bool allowMultiSelection,
		                                        [CanBeNull] Type initialSelection,
		                                        [CanBeNull] Predicate<Type> match,
		                                        bool includeObsoleteTypes)
		{
			using (var form = new TypeFinderForm(allowMultiSelection, typeof(T)))
			{
				new TypeFinderPresenter<T>(form, initialSelection, match, includeObsoleteTypes);

				DialogResult result = UIEnvironment.ShowDialog(form, owner);

				return result == DialogResult.OK
					       ? form.SelectedTypes
					       : null;
			}
		}
	}
}
