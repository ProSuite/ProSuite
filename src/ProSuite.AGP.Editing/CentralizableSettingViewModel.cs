using System;
using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing
{
	public class CentralizableSettingViewModel<T> : INotifyPropertyChanged where T : struct
	{
		private readonly CentralizableSetting<T> _centralizableSetting;
		private readonly CentralizableSetting<bool> _controllingParent;
		private bool _isChangeAllowedByParent;

		public CentralizableSettingViewModel(
			CentralizableSetting<T> centralizableSetting,
			[CanBeNull] CentralizableSetting<bool> controllingParent = null)
		{
			_centralizableSetting = centralizableSetting ??
			                        throw new ArgumentNullException(nameof(centralizableSetting));

			_centralizableSetting.PropertyChanged += CentralizableSetting_PropertyChanged;

			_controllingParent = controllingParent;
			IsChangeAllowedByParent = _controllingParent?.CurrentValue ?? true;

			if (_controllingParent != null)
			{
				_controllingParent.PropertyChanged += (sender, e) =>
				{
					IsChangeAllowedByParent = _controllingParent.CurrentValue;
				};
			}
		}

		private void CentralizableSetting_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(_centralizableSetting.HasLocalOverride))
			{
				OnPropertyChanged(nameof(HasLocalOverride));
				OnPropertyChanged(nameof(ToolTip));
			}
		}

		private bool CanOverrideLocally => _centralizableSetting.CanOverrideLocally;

		public bool HasLocalOverride
		{
			get => _centralizableSetting.HasLocalOverride;
		}

		public T CurrentValue
		{
			get => _centralizableSetting.CurrentValue;
			set
			{
				if (! Equals(_centralizableSetting.CurrentValue, value))
				{
					_centralizableSetting.CurrentValue = value;
					OnPropertyChanged(nameof(CurrentValue));
				}
			}
		}

		public bool IsChangeAllowedByParent
		{
			get => _isChangeAllowedByParent;
			set
			{
				_isChangeAllowedByParent = value;
				OnPropertyChanged(nameof(IsEnabled));
			}
		}

		[NotNull]
		public string ToolTip
		{
			get
			{
				CentralizableSetting<T> centralizableSetting = _centralizableSetting;

				return ManagedOptionsUtils.GetMessage(centralizableSetting);
			}
		}

		public bool IsEnabled => IsChangeAllowedByParent && CanOverrideLocally;

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
