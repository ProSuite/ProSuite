using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public interface IBoundView<T, O> : IWin32Window where O : IViewObserver
	{
		O Observer { get; set; }

		void BindTo([NotNull] T target);

		event EventHandler Load;
	}
}
