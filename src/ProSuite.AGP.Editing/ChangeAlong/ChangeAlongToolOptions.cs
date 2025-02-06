using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public class ChangeAlongToolOptions : OptionsBase<PartialChangeAlongToolOptions>
	{
		public ChangeAlongToolOptions([CanBeNull] PartialChangeAlongToolOptions centralOptions,
		                              [CanBeNull] PartialChangeAlongToolOptions localOptions)
		{
			CentralOptions = centralOptions;
			LocalOptions = localOptions ?? new PartialChangeAlongToolOptions();
			CentralizableInsertVertices =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.InsertVertices), true);
			CentralizableExcludeCutLines =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.ExcludeCutLines), false);
		}

		#region Centralizable Properties

		public CentralizableSetting<bool> CentralizableInsertVertices { get; private set; }
		public CentralizableSetting<bool> CentralizableExcludeCutLines { get; private set; }

		#endregion

		#region Current Values

		public bool InsertVertices => CentralizableInsertVertices.CurrentValue;
		public bool ExcludeCutLines => CentralizableExcludeCutLines.CurrentValue;


		#endregion

		public override void RevertToDefaults()
		{
			CentralizableInsertVertices.RevertToDefault();
			CentralizableExcludeCutLines.RevertToDefault();
		}

		public override bool HasLocalOverrides(NotificationCollection notifications)
		{
			bool result = false;

			if (HasLocalOverride(CentralizableInsertVertices, "Insert vertices on targets for topological correctness",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableExcludeCutLines,
			                     "Exclude cut lines that are not completely within main map extent",
			                     notifications))
			{
				result = true;
			}

			return result;
		}

		public override string GetLocalOverridesMessage()
		{
			const string optionsName = "Change Along Tool Options";
			return GetLocalOverridesMessage(optionsName);
		}
	}
}
