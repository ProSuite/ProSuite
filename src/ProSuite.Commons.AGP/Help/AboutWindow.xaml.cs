using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Persistence.WPF;

namespace ProSuite.Commons.AGP.Help
{
	/// <summary>
	/// Interaction logic for AboutWindow.xaml
	/// </summary>
	public partial class AboutWindow : Window, INotifyPropertyChanged
	{
		private string _heading;
		private IList<AboutItem> _itemList;
		private ICollectionView _itemView;
		private readonly BasicFormStateManager _formStateManager;
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public AboutWindow(string heading, IEnumerable<AboutItem> items)
		{
			InitializeComponent();

			Heading = heading ?? string.Empty;
			SetItems(items);

			DataContext = this; // quick'n'dirty

			_formStateManager = new BasicFormStateManager(this);
			_formStateManager.RestoreState();
		}

		protected override void OnClosed(EventArgs e)
		{
			// Occurs post factum (closed), but size and location are still
			// available: good! Could also wire the event (instead of overriding
			// this method): and make form state persistence a one-liner in the ctor!
			_formStateManager.SaveState();
			base.OnClosed(e); // still call base because it fires the event
		}

		public string Heading
		{
			get => _heading;
			set
			{
				if (! string.Equals(_heading, value))
				{
					_heading = value;
					OnPropertyChanged();
				}
			}
		}

		public ICollectionView AboutItems
		{
			get => _itemView;
			set
			{
				_itemView = value;
				OnPropertyChanged();
			}
		}

		public void SetItems(IEnumerable<AboutItem> items)
		{
			var list = (items ?? Enumerable.Empty<AboutItem>()).ToList();
			var view = new ListCollectionView(list);

			view.GroupDescriptions?.Add(new PropertyGroupDescription(nameof(AboutItem.Section)));

			//view.SortDescriptions.Add(new SortDescription(nameof(AboutItem.Section), ListSortDirection.Ascending));
			//view.SortDescriptions.Add(new SortDescription(nameof(AboutItem.Key), ListSortDirection.Ascending));

			_itemList = list;
			AboutItems = view;
		}

		private void CopyButtonClicked(object sender, RoutedEventArgs e)
		{
			try
			{
				if (_itemList is null) return; // no-op

				var text = AboutItem.GetPlainText(_itemList);

				Clipboard.SetText(text);

				DialogResult = true;
			}
			catch (Exception ex)
			{
				Gateway.ShowError(ex.Message, _msg, "Copy About Box Text");
			}
		}

		private void OkButtonClicked(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
