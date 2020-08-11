using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Clients.AGP.ProSuiteSolution.LoggerUI
{
    /// <summary>
    /// Interaction logic for ProSuiteLogPaneView.xaml
    /// </summary>
    public partial class ProSuiteLogPaneView : UserControl
    {
		private bool _scrollProcessing = false;

        public ProSuiteLogPaneView()
        {
            InitializeComponent();
        }

        private void logMessagesGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {

            if (e.ExtentHeight < e.ViewportHeight)
                return;

            if (logMessagesGrid.Items.Count <= 0)
                return;

            if (e.ExtentHeightChange == 0.0 && e.ViewportHeightChange == 0.0)
                return;

			if (_scrollProcessing)
				return;

			_scrollProcessing = true;

            var oldExtentHeight = e.ExtentHeight - e.ExtentHeightChange;
            var oldVerticalOffset = e.VerticalOffset - e.VerticalChange;
            var oldViewportHeight = e.ViewportHeight - e.ViewportHeightChange;
			if (oldVerticalOffset + oldViewportHeight + 5 >= oldExtentHeight)
			{
				logMessagesGrid.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
				{
					logMessagesGrid.UpdateLayout();
					logMessagesGrid.ScrollIntoView(logMessagesGrid.Items[logMessagesGrid.Items.Count - 1], null);
				}));
			}
			_scrollProcessing = false;
		}

    }

}
