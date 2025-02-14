using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.ManagedOptions
{
	public abstract class OptionsBase<TPartialOptions> : INotifyPropertyChanged
		where TPartialOptions : PartialOptionsBase
	{
		[CanBeNull]
		public TPartialOptions CentralOptions { get; protected set; }

		[NotNull]
		public TPartialOptions LocalOptions { get; protected set; }

		public OptionsBase<TPartialOptions> Clone()
		{
			TPartialOptions newCentralOptions =
				(TPartialOptions) CentralOptions?.Clone();

			var clone = (OptionsBase<TPartialOptions>)
				Activator.CreateInstance(GetType(), newCentralOptions,
				                         (TPartialOptions) LocalOptions.Clone());

			return clone;
		}

		public abstract void RevertToDefaults();

		public abstract bool HasLocalOverrides(NotificationCollection notifications);

		[CanBeNull]
		public abstract string GetLocalOverridesMessage();

		[NotNull]
		protected CentralizableSetting<T> InitializeSetting<T>(
			[NotNull] PropertyInfo overridableSettingPropertyInfo,
			T? factoryDefault)
			where T : struct
		{
			OverridableSetting<T> centralSetting =
				CentralOptions?.GetOverridableSetting<T>(overridableSettingPropertyInfo);

			// NOTE: only initialize local setting with a value if central setting is null
			//		 or change behavior in CentralizedSetting to return the central value
			//		 even if a local value is defined.
			T? initialSettingIfLocalSettingIsNull =
				centralSetting == null
					? factoryDefault
					: null;

			OverridableSetting<T> localSetting =
				LocalOptions.GetOverridableSetting(overridableSettingPropertyInfo,
				                                   initialSettingIfLocalSettingIsNull);

			var result = new CentralizableSetting<T>(centralSetting, localSetting,
			                                         factoryDefault);

			// Re-direct the property changed event from the centralizable setting to the option's
			// property changed event:
			result.PropertyChanged += (sender, eventArgs) =>
			{
				OnPropertyChanged(eventArgs, overridableSettingPropertyInfo.Name);
			};

			return result;
		}

		protected static bool HasLocalOverride<T>(
			[NotNull] CentralizableSetting<T> centralizableSetting,
			[NotNull] string settingDescription,
			[CanBeNull] NotificationCollection notifications) where T : struct
		{
			if (centralizableSetting.IsLocalOverrideDifferentFromCentralValue())
			{
				NotificationUtils.Add(notifications, "{0} ({1})", settingDescription,
				                      centralizableSetting.CurrentValue);
				return true;
			}

			return false;
		}

		[CanBeNull]
		protected string GetLocalOverridesMessage([NotNull] string optionsName)
		{
			string resultNotification = null;

			var notifications = new NotificationCollection();

			if (HasLocalOverrides(notifications))
			{
				resultNotification =
					string.Format("{0}: Default values were changed for: {1}  {2}",
					              optionsName, Environment.NewLine,
					              notifications.Concatenate(Environment.NewLine + "  "));
			}

			return resultNotification;
		}

		[CanBeNull]
		protected static string GetTooltipAppendix<T>(
			[CanBeNull] ICollection<DatasetSpecificValue<T>> datasetSpecificValues)
			where T : struct
		{
			string result = null;

			if (datasetSpecificValues != null && datasetSpecificValues.Count > 0)
			{
				result =
					$"{Environment.NewLine}Overrides for datasets:";

				foreach (var datasetSpecificValue in datasetSpecificValues)
				{
					result +=
						$"{Environment.NewLine}- {datasetSpecificValue.Dataset}: {datasetSpecificValue.Value}";
				}
			}

			return result;
		}

		#region INotifyPropertyChanged implementation

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(
			PropertyChangedEventArgs args,
			[NotNull] string overridablePropertyName)
		{
			// NOTE: After a property update, all kinds of secondary properties, such as HasOverride and
			// Tooltip fire the changed event too. Just fire on the actual properties' change:
			const string relevantPropertyName = nameof(CentralizableSetting<bool>.CurrentValue);

			if (args.PropertyName == relevantPropertyName)
			{
				PropertyChanged?.Invoke(
					this, new PropertyChangedEventArgs(overridablePropertyName));
			}
		}

		#endregion
	}
}
