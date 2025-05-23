using System.ComponentModel;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.ManagedOptions
{
	/// <summary>
	/// Provides a container for both a local and a central setting to allow
	/// managing local overrides of centrally defined settings.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class CentralizableSetting<T> : INotifyPropertyChanged where T : struct
	{
		[CanBeNull] private readonly OverridableSetting<T> _centralSetting;

		[NotNull] private readonly OverridableSetting<T> _localSetting;

		public CentralizableSetting(
			[CanBeNull] OverridableSetting<T> centralSetting,
			[NotNull] OverridableSetting<T> localSetting,
			T? factoryDefault)
		{
			Assert.ArgumentNotNull(localSetting, nameof(localSetting));

			_centralSetting = centralSetting;
			_localSetting = localSetting;
			FactoryDefault = factoryDefault;
		}

		[CanBeNull]
		public T? FactoryDefault { get; }

		public bool CanOverrideLocally => _centralSetting == null || _centralSetting.Override;

		public bool HasLocalOverride
		{
			get
			{
				// the XML tag can be missing or the value can be nil
				if (_centralSetting?.Value == null)
				{
					return false;
				}

				return _localSetting.Override;
			}
			private set
			{
				if (_localSetting.Override != value)
				{
					_localSetting.Override = value;
					NotifyCurrentValueChanged(nameof(HasLocalOverride));
				}
			}
		}

		public bool IsLocalOverrideDifferentFromCentralValue()
		{
			if (! HasLocalOverride)
			{
				return false;
			}

			return ! CurrentValue.Equals(CentralValue);
		}

		public bool HasCentralValue => _centralSetting?.Value != null;

		public T CurrentValue
		{
			get
			{
				T resultValue = default(T);

				if (CanOverrideLocally && _localSetting.Value != null)
				{
					resultValue = (T) _localSetting.Value;
				}
				else if (_centralSetting?.Value != null)
				{
					resultValue = (T) _centralSetting.Value;
				}
				else if (FactoryDefault != null)
				{
					resultValue = (T) FactoryDefault;
				}

				return resultValue;
			}
			set
			{
				// setter used by data binding
				SetLocalValue(value);
			}
		}

		public T? CentralValue => _centralSetting?.Value;

		public void RevertToDefault()
		{
			if (_centralSetting != null && _centralSetting.HasValue)
			{
				HasLocalOverride = false;
				SetLocalValue(null);
			}
			else if (FactoryDefault != null)
			{
				SetLocalValue(FactoryDefault);
			}
		}

		public void SetLocalValue(T? value)
		{
			if (! Equals(value, _localSetting.Value))
			{
				_localSetting.Value = value;
				NotifyCurrentValueChanged(nameof(CurrentValue));
			}

			HasLocalOverride = _centralSetting == null ||
			                   value != null && ! value.Equals(_centralSetting.Value);
		}

		[CanBeNull]
		public string TooltipAppendix { get; set; }

		private void NotifyCurrentValueChanged([NotNull] string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#region Implementation of INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}
