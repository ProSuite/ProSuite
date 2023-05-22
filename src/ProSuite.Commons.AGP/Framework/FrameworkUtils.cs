using ArcGIS.Desktop.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Framework
{
	public static class FrameworkUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static void ToggleState([NotNull] string stateId, bool activate)
		{
			Assert.ArgumentNotNullOrEmpty(stateId, nameof(stateId));

			_msg.VerboseDebug(() => $"{(activate ? "Activate" : "Deactivate")} state {stateId}");

			if (activate)
			{
				FrameworkApplication.State.Activate(stateId);
			}
			else
			{
				FrameworkApplication.State.Deactivate(stateId);
			}
		}
	}
}
