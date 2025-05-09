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

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
