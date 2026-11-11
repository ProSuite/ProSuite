using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.UI.WPF;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using ToolTip = System.Windows.Controls.ToolTip;
using TriggerBase = Microsoft.Xaml.Behaviors.TriggerBase;

namespace ProSuite.UI.Core.QA.VerificationProgress
{
	/// <summary>
	/// Interaction logic for VerificationProgressWpfControl.xaml
	/// </summary>
	public partial class VerificationProgressWpfControl : IWinFormHostable
	{
		private Form _hostWinForm;

		private ToolTip _clickedToolTip;
		private FrameworkElement _clickedDisabledElement;

		public VerificationProgressWpfControl()
		{
			// BUG: Could not load file or assembly 'Microsoft.Xaml.Behaviors, ...'
			//      when not loaded directly.
			// https://github.com/microsoft/XamlBehaviorsWpf/issues/86
			// https://github.com/zspitz/ANTLR4ParseTreeVisualizer/issues/35

			var _ = new DefaultTriggerAttribute(
				typeof(Trigger), typeof(TriggerBase), null);

			InitializeComponent();
		}

		public void SetDataSource(VerificationProgressViewModel viewModel)
		{
			if (viewModel.CloseAction == null)
			{
				viewModel.CloseAction = () =>
				{
					if (_hostWinForm != null)
					{
						_hostWinForm.Close();
					}
					else
					{
						var wpfWindow = Window.GetWindow(this);

						Assert.NotNull(wpfWindow).Close();
					}
				};
			}

			DataContext = viewModel;
		}

		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			VerificationProgressViewModel vm = (VerificationProgressViewModel) DataContext;

			// This is too early for the host win form (must be wired separately in the setter):
			WireClosingEvent(vm);

			// In the designer:
			if (vm == null) return;

			await Assert.NotNull(vm).RunBackgroundVerificationAsync();
		}

		private void WireClosingEvent(VerificationProgressViewModel vm)
		{
			if (_hostWinForm != null)
			{
				_hostWinForm.Closing += vm.Closing;
			}
			else
			{
				var wpfWindow = Window.GetWindow(this);

				if (wpfWindow != null)
				{
					wpfWindow.Closing += vm.Closing;
				}
			}
		}

		/// <summary>
		/// A disabled element receives no mouse events, the click arrives at the first
		/// enabled parent. Its tooltip, which gives the reason why it is disabled, is shown
		/// also on a click because users rarely wait for it to appear on hovering.
		/// </summary>
		private void ShowToolTipOfDisabledElement(object sender, MouseButtonEventArgs e)
		{
			CloseClickedToolTip(sender, e);

			FrameworkElement disabledElement = FindDisabledElementWithToolTip(e);

			if (disabledElement == null)
			{
				return;
			}

			_clickedDisabledElement = disabledElement;
			_clickedToolTip = new ToolTip
			                  {
				                  Content = disabledElement.ToolTip,
				                  PlacementTarget = disabledElement,
				                  Placement = PlacementMode.Bottom,
				                  StaysOpen = true,
				                  IsOpen = true
			                  };
		}

		private void CloseClickedToolTipWhenMovedAway(object sender, MouseEventArgs e)
		{
			if (_clickedToolTip != null &&
			    FindDisabledElementWithToolTip(e) != _clickedDisabledElement)
			{
				CloseClickedToolTip(sender, e);
			}
		}

		private FrameworkElement FindDisabledElementWithToolTip(MouseEventArgs e)
		{
			// The hit test also finds disabled elements, unlike the routing of mouse events.
			DependencyObject current = VisualTreeHelper.HitTest(this, e.GetPosition(this))
			                                           ?.VisualHit;

			while (current != null && current != this)
			{
				if (current is FrameworkElement element && ! element.IsEnabled &&
				    element.ToolTip != null)
				{
					return element;
				}

				current = VisualTreeHelper.GetParent(current);
			}

			return null;
		}

		private void CloseClickedToolTip(object sender, EventArgs e)
		{
			_clickedDisabledElement = null;

			if (_clickedToolTip != null)
			{
				_clickedToolTip.IsOpen = false;
				_clickedToolTip = null;
			}
		}

		public Form HostFormsWindow
		{
			set
			{
				_hostWinForm = value;

				VerificationProgressViewModel vm = (VerificationProgressViewModel) DataContext;

				if (_hostWinForm != null && vm != null)
				{
					_hostWinForm.Closing += vm.Closing;
				}
			}
		}
	}
}
