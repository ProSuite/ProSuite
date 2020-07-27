using System.Windows;
using System.Windows.Controls;


namespace Clients.AGP.ProSuiteSolution.LoggerUI
{
    /// <summary>
    /// Interaction logic for ProSuiteLogPaneView.xaml
    /// </summary>
    public partial class ProSuiteLogPaneView : UserControl
    {
        public ProSuiteLogPaneView()
        {
            InitializeComponent();
        }

        private void logMessagesGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // If the entire contents fit on the screen, ignore this event
            if (e.ExtentHeight < e.ViewportHeight)
                return;

            // If no items are available to display, ignore this event
            if (logMessagesGrid.Items.Count <= 0)
                return;

            // If the ExtentHeight and ViewportHeight haven't changed, ignore this event
            if (e.ExtentHeightChange == 0.0 && e.ViewportHeightChange == 0.0)
                return;

            // If we were close to the bottom when a new item appeared,
            // scroll the new item into view.  We pick a threshold of 5
            // items since issues were seen when resizing the window with
            // smaller threshold values.
            var oldExtentHeight = e.ExtentHeight - e.ExtentHeightChange;
            var oldVerticalOffset = e.VerticalOffset - e.VerticalChange;
            var oldViewportHeight = e.ViewportHeight - e.ViewportHeightChange;
            if (oldVerticalOffset + oldViewportHeight + 5 >= oldExtentHeight)
                logMessagesGrid.ScrollIntoView(logMessagesGrid.Items[logMessagesGrid.Items.Count - 1]);
        }

    }

}
