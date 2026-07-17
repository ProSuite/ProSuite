using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProSuite.Commons.Geom;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.ChangeAlong;

public class ZValueSourceSelectionViewModel : INotifyPropertyChanged
{
	public ZValueSourceSelectionViewModel(
		CentralizableSetting<ChangeAlongZSource> centralizableSetting)
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

	public CentralizableSetting<ChangeAlongZSource> CentralizableSetting { get; }

	public ChangeAlongZSource CurrentValue
	{
		get { return CentralizableSetting.CurrentValue; }
		set
		{
			CentralizableSetting.CurrentValue = value;
			OnPropertyChanged();
		}
	}

	public string ToolTip
	{
		get => ManagedOptionsUtils.GetMessage(CentralizableSetting);
	}
}
