using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing
{
	public class TargetFeatureSelectionViewModel : INotifyPropertyChanged
	{
		private Visibility _selectedFeaturesVisibility;

		private Visibility _editableSelectableFeaturesVisibility;

		private bool _isTargetFeatureSelectionEnabled;

		public TargetFeatureSelectionViewModel(
			CentralizableSetting<TargetFeatureSelection> centralizableSetting)
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

		public Visibility SelectedFeaturesVisibility
		{
			get => _selectedFeaturesVisibility;
			set
			{
				_selectedFeaturesVisibility = value;
				OnPropertyChanged();
			}
		}

		public Visibility EditableSelectableFeaturesVisibility
		{
			get => _editableSelectableFeaturesVisibility;
			set
			{
				_editableSelectableFeaturesVisibility = value;
				OnPropertyChanged();
			}
		}

		public bool IsTargetFeatureSelectionEnabled
		{
			get => _isTargetFeatureSelectionEnabled;
			set
			{
				_isTargetFeatureSelectionEnabled = value;
				OnPropertyChanged();
			}
		}

		public string ToolTip
		{
			get => ManagedOptionsUtils.GetMessage(CentralizableSetting);
		}
	}
}
