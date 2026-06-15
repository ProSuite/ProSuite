using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProSuite.AGP.Editing.MergeFeatures;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing;

public class MergeOperationSurvivorViewModel : INotifyPropertyChanged
{
	private bool _isMergeOperationSurvivorEnabled;

	public MergeOperationSurvivorViewModel(
		CentralizableSetting<MergeOperationSurvivor> centralizableSetting)
	{
		CentralizableSetting = centralizableSetting;

		CentralizableSetting.PropertyChanged += (sender, args) =>
		{
			if (args.PropertyName == nameof(CentralizableSetting.CurrentValue))
			{
				OnPropertyChanged(nameof(CurrentValue));
				OnPropertyChanged(nameof(ToolTip));
			}

			if (args.PropertyName == nameof(CentralizableSetting.HasLocalOverride))
			{
				OnPropertyChanged(nameof(ToolTip));
			}
		};
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public CentralizableSetting<MergeOperationSurvivor> CentralizableSetting { get; }

	public MergeOperationSurvivor CurrentValue
	{
		get { return CentralizableSetting.CurrentValue; }
		set
		{
			CentralizableSetting.CurrentValue = value;
			OnPropertyChanged();
		}
	}

	public bool IsMergeOperationSurvivorEnabled
	{
		get => _isMergeOperationSurvivorEnabled;
		set
		{
			_isMergeOperationSurvivorEnabled = value;
			OnPropertyChanged();
		}
	}

	public string ToolTip
	{
		get => ManagedOptionsUtils.GetMessage(CentralizableSetting);
	}
}