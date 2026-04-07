using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.WPF;
using RelayCommand = ProSuite.Commons.UI.WPF.RelayCommand;

namespace ProSuite.Commons.AGP.Help;

public class AboutViewModel : INotifyPropertyChanged
{
	private const string DefaultTitle = "About";

	private string _title;
	private IList<AboutItem> _itemList;
	private ICollectionView _itemView;
	private ICommand _copyCommand;
	private ICommand _closeCommand;

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public AboutViewModel(string title, IEnumerable<AboutItem> items)
	{
		_title = title ?? DefaultTitle;
		SetItems(items); // items may be empty
	}

	public string Title
	{
		get => _title;
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				value = DefaultTitle;
			}

			if (! string.Equals(_title, value))
			{
				_title = value;
				OnPropertyChanged();
			}
		}
	}

	public ICollectionView AboutItems
	{
		get => _itemView;
		private set
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

	public ICommand CopyCommand =>
		_copyCommand ??= new RelayCommand(CopyToClipboard, () => true);

	public ICommand CloseCommand =>
		_closeCommand ??= new RelayCommand<ICloseableWindow>(CloseDialog);

	#region Command actions

	private void CopyToClipboard()
	{
		try
		{
			if (_itemList is null) return; // no-op

			var text = AboutUtils.GetPlainText(_itemList);

			Clipboard.SetText(text);

			//DialogResult = true;
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex.Message, _msg, "Copy About Box Text");
		}
	}

	private static void CloseDialog(ICloseableWindow window)
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

	#endregion

	#region INotifyPropertyChanged

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	#endregion
}
