using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.LoggerUI;

public partial class LogDockPane
{
	public LogDockPane()
	{
		InitializeComponent();
	}

	private void logMessagesGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
	{
		DataGridRow row = sender as DataGridRow;
		logMessagesGrid.SelectedItem = row?.Item;
	}

	private void logMessagesGrid_Loaded(object sender, RoutedEventArgs e)
	{
		var items = logMessagesGrid.ItemsSource as ObservableCollection<LoggingEventItem>;

		if (items == null)
			return;

		items.CollectionChanged += CollectionChanged;
	}

	private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		var msSecDelay = 500; // delay to scroll to end to last message - cancelable?
		Task.Delay(msSecDelay).ContinueWith(_ => ScrollMessagesToEnd());
	}

	private void ScrollMessagesToEnd()
	{
		logMessagesGrid.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action) (() =>
				                                       {
					                                       if (logMessagesGrid.Items != null &&
					                                           logMessagesGrid.Items
						                                           .Count > 0)
					                                       {
						                                       logMessagesGrid.ScrollIntoView(
							                                       logMessagesGrid.Items[
								                                       logMessagesGrid.Items
									                                       .Count - 1]);
					                                       }
				                                       }));
	}

	private void UserControl_IsVisibleChanged(object sender,
	                                          DependencyPropertyChangedEventArgs e)
	{
		if (IsVisible)
			ScrollMessagesToEnd();
	}
}
