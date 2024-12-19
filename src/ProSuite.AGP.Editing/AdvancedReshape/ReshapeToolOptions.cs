using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.AdvancedReshape
{
	public class ReshapeToolOptions : OptionsBase<PartialReshapeToolOptions>, INotifyPropertyChanged
	{
		public ReshapeToolOptions([CanBeNull] PartialReshapeToolOptions centralOptions,
		                          [CanBeNull] PartialReshapeToolOptions localOptions)
		{
			CentralOptions = centralOptions;
			LocalOptions = localOptions ?? new PartialReshapeToolOptions();
			CentralizableShowPreview =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.ShowPreview), true);
			CentralizableRemainInSketchMode =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.RemainInSketchMode), false);
			CentralizableMoveOpenJawEndJunction =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.MoveOpenJawEndJunction), false);
			CentralizableUseTopologyTypeSelection =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.UseTopologyTypeSelection),
					false);

			CentralizableShowPreview.PropertyChanged += (_, _) =>
				OnPropertyChanged(nameof(ShowPreview));
			CentralizableRemainInSketchMode.PropertyChanged += (_, _) =>
				OnPropertyChanged(nameof(RemainInSketchMode));
			CentralizableMoveOpenJawEndJunction.PropertyChanged += (_, _) =>
				OnPropertyChanged(nameof(MoveOpenJawEndJunction));
			CentralizableUseTopologyTypeSelection.PropertyChanged += (_, _) =>
				OnPropertyChanged(nameof(UseTopologyTypeSelection));
		}

		#region Centralizable Properties

		public CentralizableSetting<bool> CentralizableShowPreview { get; private set; }
		public CentralizableSetting<bool> CentralizableRemainInSketchMode { get; private set; }
		public CentralizableSetting<bool> CentralizableMoveOpenJawEndJunction { get; private set; }

		public CentralizableSetting<bool> CentralizableUseTopologyTypeSelection
		{
			get;
			private set;
		}

		#endregion

		#region Current Values

		public bool ShowPreview => CentralizableShowPreview.CurrentValue;
		public bool RemainInSketchMode => CentralizableRemainInSketchMode.CurrentValue;

		public bool MoveOpenJawEndJunction
		{
			get => CentralizableMoveOpenJawEndJunction.CurrentValue;
			set => CentralizableMoveOpenJawEndJunction.CurrentValue = value;
		}

		public bool UseTopologyTypeSelection => CentralizableUseTopologyTypeSelection.CurrentValue;

		#endregion

		public override void RevertToDefaults()
		{
			CentralizableShowPreview.RevertToDefault();
			CentralizableRemainInSketchMode.RevertToDefault();
			CentralizableMoveOpenJawEndJunction.RevertToDefault();
			CentralizableUseTopologyTypeSelection.RevertToDefault();
		}

		public override bool HasLocalOverrides(NotificationCollection notifications)
		{
			bool result = false;

			if (HasLocalOverride(CentralizableShowPreview, "Show the reshape preview",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableRemainInSketchMode,
			                     "Remain in sketch mode after reshape operation",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableMoveOpenJawEndJunction, "Move the end junction",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableUseTopologyTypeSelection,
			                     "Enable topology type selection", notifications))
			{
				result = true;
			}

			return result;
		}

		public override string GetLocalOverridesMessage()
		{
			const string optionsName = "Reshape Tool Options";
			return GetLocalOverridesMessage(optionsName);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
