using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing
{
	public class TargetFeatureSelectionViewModel : INotifyPropertyChanged
	{
		public TargetFeatureSelectionViewModel(
			CentralizableSetting<TargetFeatureSelection> centralizableSetting)
		{
			CentralizableSetting = centralizableSetting;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public CentralizableSetting<TargetFeatureSelection> CentralizableSetting { get; }

		public TargetFeatureSelection CurrentValue
		{
			get { return CentralizableSetting.CurrentValue; }
			set
			{
				CentralizableSetting.CurrentValue = value;
				OnPropertyChanged();
			}
		}
	}
}
