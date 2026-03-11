using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Display;

public class ExportSLDLMOptions : INotifyPropertyChanged
{
	private Map _map;
	private GroupLayerComboItem _groupLayer;
	private string _configFilePath;
	private string _remark;
	private bool _includeMaskingInfo;
	private bool _extraMaskingInfo;
	private string _warningText;
	private readonly List<GroupLayerComboItem> _groupLayers;

	private string _rememberedLayerUri;
	private string _rememberedConfigPath;
	private string _rememberedRemark;
	private bool? _rememberedIncludeMaskingInfo;
	private bool? _rememberedExtraMaskingInfo;

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public ExportSLDLMOptions()
	{
		_groupLayers = new List<GroupLayerComboItem>();
		GroupLayerItems = new ReadOnlyCollection<GroupLayerComboItem>(_groupLayers);
	}

	public void SetMap(Map map)
	{
		_map = map;

		if (map is null)
		{
			_groupLayers.Clear();
		}
		else
		{
			var entireMapItem = new GroupLayerComboItem("Entire Map");
			var selectedUri = GroupLayerItem?.GroupLayer?.URI;

			_groupLayers.Clear();
			_groupLayers.Add(entireMapItem);
			_groupLayers.AddRange(map.GetLayersAsFlattenedList().OfType<GroupLayer>().Select(gl => new GroupLayerComboItem(gl)));

			var restore = _groupLayers.FirstOrDefault(
				item => string.Equals(item?.GroupLayer?.URI, selectedUri));
			GroupLayerItem = restore ?? entireMapItem;
		}
	}

	public void RememberOptions()
	{
		_rememberedLayerUri = GroupLayerItem?.GroupLayer?.URI;
		_rememberedConfigPath = ConfigFilePath;
		_rememberedRemark = Remark;
		_rememberedIncludeMaskingInfo = IncludeMaskingInfo;
		_rememberedExtraMaskingInfo = ExtraMaskingInfo;
	}

	public void RestoreOptions()
	{
		// find group layer by URI (can be null)
		var restore = _groupLayers.FirstOrDefault(
			item => string.Equals(item?.GroupLayer?.URI, _rememberedLayerUri));

		GroupLayerItem = restore;
		ConfigFilePath = _rememberedConfigPath;
		Remark = _rememberedRemark ?? string.Empty;
		IncludeMaskingInfo = _rememberedIncludeMaskingInfo ?? true;
		ExtraMaskingInfo = _rememberedExtraMaskingInfo ?? false;
	}

	public string MapName => _map?.Name ?? string.Empty;

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
				ValidateSelectedGroupLayer();
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

	public bool IncludeMaskingInfo
	{
		get => _includeMaskingInfo;
		set
		{
			if (_includeMaskingInfo != value)
			{
				_includeMaskingInfo = value;
				OnPropertyChanged();

				if (! value && _extraMaskingInfo)
				{
					_extraMaskingInfo = false;
					OnPropertyChanged(nameof(ExtraMaskingInfo));
				}

				OnPropertyChanged(nameof(ExtraMaskingInfoEnabled));
			}
		}
	}

	public bool ExtraMaskingInfo
	{
		get => _extraMaskingInfo;
		set
		{
			if (_extraMaskingInfo != value)
			{
				_extraMaskingInfo = value;
				OnPropertyChanged();
			}
		}
	}

	public bool ExtraMaskingInfoEnabled => IncludeMaskingInfo;

	public bool ExportButtonEnabled => ! string.IsNullOrWhiteSpace(ConfigFilePath);

	public Visibility WarningVisibility => string.IsNullOrEmpty(WarningText)
		                                       ? Visibility.Hidden
		                                       : Visibility.Visible;

	public string WarningText
	{
		get => _warningText;
		set
		{
			if (_warningText != value)
			{
				_warningText = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(WarningVisibility));
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	private async void ValidateSelectedGroupLayer()
	{
		try
		{
			var selectedGroupLayer = GroupLayerItem?.GroupLayer; // null means entire map

			var container = (ILayerContainer)selectedGroupLayer ?? _map;

			if (container is null)
			{
				WarningText = string.Empty;
			}
			else
			{
				var usesSLD = await QueuedTask.Run(() => DisplayUtils.UsesSLD(container));

				if (usesSLD)
				{
					WarningText = string.Empty;
				}
				else
				{
					WarningText = "No SLD configured (or disabled)";
				}
			}
		}
		catch (Exception ex)
		{
			WarningText = ex.Message;
			_msg.Error(ex.Message, ex);
		}
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
