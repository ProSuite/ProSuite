using System.Collections;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.ScreenBinding.Lists
{
	public interface IPicklist : IEnumerable
	{
		[NotNull]
		IList Values { get; }

		string DisplayMember { get; set; }

		void Fill([NotNull] ComboBox comboBox);

		void SelectForDisplay([NotNull] ComboBox comboBox, string display);

		[NotNull]
		string GetDisplay([NotNull] ComboBox comboBox);

		object GetValue(object selectedItem);

		void SetValue([NotNull] ComboBox comboBox, [CanBeNull] object originalValue);

		[NotNull]
		string GetDisplay([CanBeNull] object item);

		//void Fill(System.Windows.Controls.ComboBox control);
		//void SelectForDisplay(System.Windows.Controls.ComboBox comboBox, string display);
		//void SetValue(System.Windows.Controls.ComboBox comboBox, object originalValue);
	}
}
