using System;
using ProSuite.AGP.Editing.AdvancedReshape;
using ProSuite.AGP.Editing.Properties;
using ProSuite.AGP.Editing.Selection;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Core.GeometryProcessing.AdvancedReshape;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.YReshape
{
	public class YReshapeToolBase : AdvancedReshapeToolBase
	{
		private YReshapeToolOptions _yreshapeToolOptions;
		private OverridableSettingsProvider<YReshapeToolOptions> _settingsProvider;

		protected string OptionsFileName => "YReshapeToolOptions.xml";

		[CanBeNull]
		protected virtual string CentralConfigDir => null;

		protected virtual string LocalConfigDir =>
			EnvironmentUtils.ConfigurationDirectoryProvider.GetDirectory(AppDataFolder.Roaming);

		protected override SelectionSettings GetSelectionSettings()
		{
			throw new NotImplementedException();
		}

		protected override IAdvancedReshapeService MicroserviceClient { get; }

		protected YReshapeToolBase()
		{
			SelectionCursor = ToolUtils.GetCursor(Resources.YReshapeToolCursor);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.YReshapeToolCursorShift);
		}

		// TopGIS Code //

		protected override void OptionsInitialized()
		{
			AdvancedReshapeOptions.OpenJawReshapePolicy = OpenJawReshapePolicy.Allow;

			AdvancedReshapeOptions.ProtectOpenJawSettings();
		}
	}
}
