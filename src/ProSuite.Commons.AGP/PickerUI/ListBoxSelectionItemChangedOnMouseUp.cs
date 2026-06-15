using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProSuite.Commons.AGP.PickerUI;

public class ListBoxSelectionItemChangedOnMouseUp : ListBox
{
	protected override void OnMouseUp(MouseButtonEventArgs e)
	{
		DependencyObject obj = ContainerFromElement((Visual) e.OriginalSource);

		ListBoxItem item = obj as ListBoxItem;

		if (item != null && Items.Contains(item))
		{
			SelectedItem = item;
		}
	}
}
