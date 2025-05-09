using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.Commons.AGP.Help;

public class AboutViewModel : INotifyPropertyChanged
{
	private string _heading;
	private IList<AboutItem> _itemList;
	private ICollectionView _itemView;
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public AboutViewModel(string heading, IEnumerable<AboutItem> items)
	{
		_heading = heading ?? string.Empty;
		SetItems(items); // items may be empty
	}

	public string Heading
	{
		get => _heading;
		set
		{
			if (!string.Equals(_heading, value))
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

	private ICommand _copyCommand;
	public ICommand CopyCommand =>
		_copyCommand ??= new RelayCommand(CopyItems);

	private ICommand _closeCommand;
	public ICommand CloseCommand =>
		_closeCommand ??= new RelayCommand<ICloseableWindow>(CloseDialog);

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

	private void CopyItems()
	{
		try
		{
			if (_itemList is null) return; // no-op

			var text = AboutItem.GetPlainText(_itemList);

			Clipboard.SetText(text);

			//DialogResult = true;
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex.Message, _msg, "Copy About Box Text");
		}
	}

	private void CloseDialog(ICloseableWindow window)
	{
		try
		{
			window.CloseWindow();
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex.Message, _msg, "Close About Box");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
