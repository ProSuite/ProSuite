using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ProSuite.Commons.AGP.Carto;

public class SymbolDisplaySettingsViewModel : INotifyPropertyChanged
{
	private bool _showMaskingSettings = true;
	private bool _avoidSLMWithoutSLD;
	private bool _useScaleRange;
	private double _minScaleDenominator;
	private double _maxScaleDenominator;
	private bool? _wantSLD;
	private bool? _wantLM;
	private string _scopeMessage;

	public bool ShowMaskingSettings
	{
		get => _showMaskingSettings;
		set
		{
			if (SetProperty(ref _showMaskingSettings, value))
			{
				OnPropertyChanged(nameof(MaskingSettingsVisibility));
				OnPropertyChanged(nameof(WhatText));
			}
		}
	}
	public bool AvoidSLMWithoutSLD
	{
		get => _avoidSLMWithoutSLD;
		set => SetProperty(ref _avoidSLMWithoutSLD, value);
	}

	public bool UseScaleRange
	{
		get => _useScaleRange;
		set => SetProperty(ref _useScaleRange, value);
	}

	public double MinScaleDenominator
	{
		get => _minScaleDenominator;
		set
		{
			if (Math.Abs(_minScaleDenominator - value) > double.Epsilon)
			{
				_minScaleDenominator = value;
				OnPropertyChanged();
			}
		}
	}

	public double MaxScaleDenominator
	{
		get => _maxScaleDenominator;
		set
		{
			if (Math.Abs(_maxScaleDenominator - value) > double.Epsilon)
			{
				_maxScaleDenominator = value;
				OnPropertyChanged();
			}
		}
	}

	public bool? WantSLD
	{
		get => _wantSLD;
		set => SetProperty(ref _wantSLD, value);
	}

	public bool? WantLM
	{
		get => _wantLM;
		set => SetProperty(ref _wantLM, value);
	}

	public string ScopeMessage
	{
		get => _scopeMessage;
		set => SetProperty(ref _scopeMessage, value);
	}

	public Visibility MaskingSettingsVisibility =>
		ShowMaskingSettings ? Visibility.Visible : Visibility.Collapsed;

	public string WhatText =>
		ShowMaskingSettings ? "SLD/SLM" : "SLD";

	#region Tentatively piggyback perimeter display settings

	private bool _wantPerimeter;
	private Visibility _perimeterSettingsVisibility;

	public bool WantPerimeter
	{
		get => _wantPerimeter;
		set => SetProperty(ref _wantPerimeter, value);
	}

	public Visibility PerimeterSettingsVisibility
	{
		get => _perimeterSettingsVisibility;
		set => SetProperty(ref _perimeterSettingsVisibility, value);
	}

	#endregion

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	private bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(backingField, value))
		{
			return false;
		}

		backingField = value;
		OnPropertyChanged(propertyName);
		return true;
	}
}
