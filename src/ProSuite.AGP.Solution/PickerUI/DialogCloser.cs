using System.Windows;

namespace Clients.AGP.ProSuiteSolution.PickerUI
{
	//Since the window's DialogResult Property is not a dependency property,
	//we cannot bind to it. This attached property is used instead.
	//The ViewModel's DialogResult property is then bound to it. 

	public static class DialogCloser
	{
		public static readonly DependencyProperty DialogResultProperty =
			DependencyProperty.RegisterAttached(
				"DialogResult",
				typeof(bool?),
				typeof(DialogCloser),
				new PropertyMetadata(DialogResultChanged));

		private static void DialogResultChanged(
			DependencyObject d,
			DependencyPropertyChangedEventArgs e)
		{
			var window = d as Window;
			if (window != null)
				window.DialogResult = e.NewValue as bool?;
		}

		public static void SetDialogResult(Window target, bool? value)
		{
			target.SetValue(DialogResultProperty, value);
		}
	}
}
