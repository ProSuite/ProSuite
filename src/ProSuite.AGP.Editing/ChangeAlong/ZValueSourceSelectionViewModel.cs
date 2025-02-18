using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public class ZValueSourceSelectionViewModel : INotifyPropertyChanged
	{
		public ZValueSourceSelectionViewModel(
			CentralizableSetting<ZValueSource> centralizableSetting)
		{
			CentralizableSetting = centralizableSetting;

			CentralizableSetting.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == nameof(CentralizableSetting.CurrentValue))
				{
					OnPropertyChanged(nameof(CurrentValue));
				}
				if (args.PropertyName == nameof(CentralizableSetting.ToolTip))
				{
					OnPropertyChanged(nameof(Tooltip));
				}
			};
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public CentralizableSetting<ZValueSource> CentralizableSetting { get; }

		public ZValueSource CurrentValue
		{
			get { return CentralizableSetting.CurrentValue; }
			set
			{
				CentralizableSetting.CurrentValue = value;
				OnPropertyChanged();
			}
		}

		public string Tooltip
		{
			get => CentralizableSetting.ToolTip;
		}
	}
}
