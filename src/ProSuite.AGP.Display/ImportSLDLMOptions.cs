using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Display;

/// <summary>
/// Options for the Import SLD/LM dialog; also serves as the dialog's View Model
/// </summary>
public class ImportSLDLMOptions : INotifyPropertyChanged
{
	private string _configFilePath;
	private GroupLayerComboItem _groupLayer;
	private readonly List<GroupLayerComboItem> _groupLayers;
	private readonly Action<string, ImportSLDLMButtonBase.IFeedback> _validate;

	private string _rememberedLayerURI;
	private string _rememberedConfigPath;

	public ImportSLDLMOptions(Action<string, ImportSLDLMButtonBase.IFeedback> validate)
	{
		_validate = validate ?? throw new ArgumentNullException(nameof(validate));
		_groupLayers = new List<GroupLayerComboItem>();
		GroupLayerItems = new ReadOnlyCollection<GroupLayerComboItem>(_groupLayers);
	}

	public void SetMap(Map map)
	{
		if (map is null)
		{
			MapName = string.Empty;
			_groupLayers.Clear();
			GroupLayerItem = null;
		}
		else
		{
			MapName = map.Name ?? string.Empty;

			var entireMapItem = new GroupLayerComboItem("Entire Map");
			var selectedURI = GroupLayerItem?.GroupLayer?.URI;

			_groupLayers.Clear();
			_groupLayers.Add(entireMapItem);
			_groupLayers.AddRange(map.GetLayersAsFlattenedList().OfType<GroupLayer>().Select(gl => new GroupLayerComboItem(gl)));

			var restore = _groupLayers.FirstOrDefault(
				item => string.Equals(item?.GroupLayer?.URI, selectedURI));
			GroupLayerItem = restore ?? entireMapItem;
		}
	}

	public void RememberOptions()
	{
		_rememberedLayerURI = GroupLayerItem?.GroupLayer?.URI;
		_rememberedConfigPath = ConfigFilePath;
	}

	public void RestoreOptions()
	{
		// find group layer by URI (can be null)
		var restored = _groupLayers.FirstOrDefault(
			item => string.Equals(item?.GroupLayer?.URI, _rememberedLayerURI));

		GroupLayerItem = restored;
		ConfigFilePath = _rememberedConfigPath;
	}

	public string MapName { get; private set; }

	public IReadOnlyList<GroupLayerComboItem> GroupLayerItems { get; }

	public GroupLayerComboItem GroupLayerItem
	{
		get => _groupLayer;
		set
		{
			if (_groupLayer != value)
			{
				_groupLayer = value;
				OnPropertyChanged();
			}
		}
	}

	public string ConfigFilePath
	{
		get => _configFilePath;
		set
		{
			if (! string.Equals(_configFilePath, value))
			{
				_configFilePath = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(ValidateButtonEnabled));
				OnPropertyChanged(nameof(ImportButtonEnabled));
			}
		}
	}

	public ICommand ValidateConfigCommand =>
		_validateConfigCommand ??= new RelayCommand(ValidateConfig);
	private ICommand _validateConfigCommand;

	public bool ValidateButtonEnabled => ! string.IsNullOrWhiteSpace(ConfigFilePath);

	public bool ImportButtonEnabled => ! string.IsNullOrWhiteSpace(ConfigFilePath);

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	private void ValidateConfig()
	{
		var feedback = new ImportSLDLMButtonBase.Feedback();

		_validate(ConfigFilePath, feedback);

		Utils.ShowFeedback(feedback);
	}

	#region Nested type

	public class GroupLayerComboItem
	{
		[PublicAPI] public string Name { get; }
		public GroupLayer GroupLayer { get; }

		public GroupLayerComboItem(string name)
		{
			Name = name ?? string.Empty;
			GroupLayer = null;
		}

		public GroupLayerComboItem(GroupLayer groupLayer)
		{
			Name = groupLayer?.Name ?? string.Empty;
			GroupLayer = groupLayer ?? throw new ArgumentNullException(nameof(groupLayer));
		}

		public override string ToString()
		{
			return Name ?? string.Empty;
		}
	}

	#endregion
}
