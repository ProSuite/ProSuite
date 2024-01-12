using System;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Framework
{
	public abstract class ButtonCommandBase : Button
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Overrides of PlugIn

		protected override void OnUpdate()
		{
			try
			{
				OnUpdateCore();
			}
			catch (Exception e)
			{
				_msg.Error($"Error in {GetType().Name}.OnUpdate", e);
			}
		}

		#endregion

		#region Overrides of Button

		protected override async void OnClick()
		{
			try
			{
				_msg.VerboseDebug(() => $"{GetType().Name}.OnClick");

				bool success = await OnClickCore();

				if (! success)
				{
					_msg.Debug($"OnClickCore false for {Caption}");
				}
			}
			catch (Exception e)
			{
				ErrorHandler.HandleError(e, _msg);
			}
		}

		#endregion

		protected virtual void OnUpdateCore() { }

		protected virtual async Task<bool> OnClickCore()
		{
			return true;
		}
	}
}
