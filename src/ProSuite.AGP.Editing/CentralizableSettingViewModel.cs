using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing
{
	public class CentralizableSettingViewModel<T> : INotifyPropertyChanged where T : struct
	{
		private readonly CentralizableSetting<T> _centralizableSetting;
		private readonly IReadOnlyList<CentralizableSetting<bool>> _controllingParents;
		private bool _isChangeAllowedByParent;
		private int _decimals = 2; // Default to 2 decimal places
		private string _unitLabel = "meters";

		public CentralizableSettingViewModel(
			CentralizableSetting<T> centralizableSetting,
			[CanBeNull] IReadOnlyList<CentralizableSetting<bool>> controllingParents = null)
		{
			_centralizableSetting = centralizableSetting ??
								   throw new ArgumentNullException(nameof(centralizableSetting));

			_centralizableSetting.PropertyChanged += CentralizableSetting_PropertyChanged;

			_controllingParents = controllingParents ?? Array.Empty<CentralizableSetting<bool>>();
			UpdateIsChangeAllowedByParent(); 

			foreach (var parent in _controllingParents)
			{
				parent.PropertyChanged += Parent_PropertyChanged;
			}
		}

		private void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			UpdateIsChangeAllowedByParent();
		}

		private void UpdateIsChangeAllowedByParent()
		{
			IsChangeAllowedByParent = _controllingParents.Count == 0 ||
									_controllingParents.All(parent => parent.CurrentValue);
		}

		private void CentralizableSetting_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(_centralizableSetting.HasLocalOverride))
			{
				OnPropertyChanged(nameof(HasLocalOverride));
				OnPropertyChanged(nameof(ToolTip));
			}

			if (e.PropertyName == nameof(_centralizableSetting.CurrentValue))
			{
				OnPropertyChanged(nameof(CurrentValue));
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
				if (!Equals(_centralizableSetting.CurrentValue, value))
				{
					_centralizableSetting.CurrentValue = value;
					OnPropertyChanged(nameof(CurrentValue));
				}
			}
		}

		public bool IsChangeAllowedByParent
		{
			get => _isChangeAllowedByParent;
			private set
			{
				if (_isChangeAllowedByParent != value)
				{
					_isChangeAllowedByParent = value;
					OnPropertyChanged(nameof(IsChangeAllowedByParent));
					OnPropertyChanged(nameof(IsEnabled));
				}
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

		/// <summary>
		/// Gets or sets the number of decimal places to display for numeric values.
		/// This property is primarily used by NumericSpinner controls.
		/// </summary>
		public int Decimals
		{
			get => _decimals;
			set
			{
				if (_decimals != value)
				{
					_decimals = value;
					OnPropertyChanged(nameof(Decimals));
				}
			}
		}

		/// <summary>
		/// Gets or sets the unit label to display alongside the value.
		/// This property is primarily used by controls that display units.
		/// </summary>
		public string UnitLabel
		{
			get => _unitLabel;
			set
			{
				if (_unitLabel != value)
				{
					_unitLabel = value;
					OnPropertyChanged(nameof(UnitLabel));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
