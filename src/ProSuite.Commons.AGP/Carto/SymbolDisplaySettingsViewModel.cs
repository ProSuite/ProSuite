using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProSuite.Commons.AGP.Carto;

public class SymbolDisplaySettingsViewModel : INotifyPropertyChanged
{
	private bool _avoidSLMWithoutSLD;
	private bool _useScaleRange;
	private double _minScaleDenominator;
	private double _maxScaleDenominator;
	private bool? _wantSLD;
	private bool? _wantLM;
	private string _scopeMessage;

	public bool AvoidSLMWithoutSLD
	{
		get => _avoidSLMWithoutSLD;
		set
		{
			if (_avoidSLMWithoutSLD != value)
			{
				_avoidSLMWithoutSLD = value;
				OnPropertyChanged();
			}
		}
	}

	public bool UseScaleRange
	{
		get => _useScaleRange;
		set
		{
			if (_useScaleRange != value)
			{
				_useScaleRange = value;
				OnPropertyChanged();
			}
		}
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
		set
		{
			if (_wantSLD != value)
			{
				_wantSLD = value;
				OnPropertyChanged();
			}
		}
	}

	public bool? WantLM
	{
		get => _wantLM;
		set
		{
			if (_wantLM != value)
			{
				_wantLM = value;
				OnPropertyChanged();
			}
		}
	}

	public string ScopeMessage
	{
		get => _scopeMessage;
		set
		{
			if (_scopeMessage != value)
			{
				_scopeMessage = value;
				OnPropertyChanged();
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
