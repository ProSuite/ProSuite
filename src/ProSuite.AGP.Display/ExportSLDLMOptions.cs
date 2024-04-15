using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Display;

public class ExportSLDLMOptions : INotifyPropertyChanged
{
	private GroupLayerComboItem _groupLayer;
	private string _configFilePath;
	private string _remark;
	private bool _omitMasks;
	private readonly List<GroupLayerComboItem> _groupLayers;

	private string _rememberedLayerURI;
	private string _rememberedConfigPath;
	private string _rememberedRemark;
	private bool? _rememberedOmitMasks;

	public ExportSLDLMOptions()
	{
		_groupLayers = new List<GroupLayerComboItem>();
		GroupLayerItems = new ReadOnlyCollection<GroupLayerComboItem>(_groupLayers);
	}

	public ExportSLDLMOptions(Map map) : this()
	{
		SetMap(map);
	}

	public void SetMap(Map map)
	{
		if (map is null)
		{
			MapName = string.Empty;
			_groupLayers.Clear();
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
		_rememberedRemark = Remark;
		_rememberedOmitMasks = OmitMasks;
	}

	public void RestoreOptions()
	{
		// find group layer by URI (may be null)
		var restore = _groupLayers.FirstOrDefault(
			item => string.Equals(item?.GroupLayer?.URI, _rememberedLayerURI));

		GroupLayerItem = restore;
		ConfigFilePath = _rememberedConfigPath;
		Remark = _rememberedRemark ?? string.Empty;
		OmitMasks = _rememberedOmitMasks ?? false;
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
				OnPropertyChanged(nameof(ExportButtonEnabled));
			}
		}
	}

	public string Remark
	{
		get => _remark;
		set
		{
			if (! string.Equals(_remark, value))
			{
				_remark = value;
				OnPropertyChanged();
			}
		}
	}

	public bool OmitMasks
	{
		get => _omitMasks;
		set
		{
			if (_omitMasks != value)
			{
				_omitMasks = value;
				OnPropertyChanged();
			}
		}
	}

	public bool ExportButtonEnabled => ! string.IsNullOrWhiteSpace(ConfigFilePath);

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
