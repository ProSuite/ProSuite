using System;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Framework
{
	public abstract class ButtonCommandBase : Button
	{
		private int _updateErrorCounter;
		private const int MaxUpdateErrors = 10;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Overrides of PlugIn

		protected override void OnUpdate()
		{
			try
			{
				OnUpdateCore();
			}
			catch (Exception ex)
			{
				if (_updateErrorCounter < MaxUpdateErrors)
				{
					_msg.Error($"{GetType().Name}.{nameof(OnUpdate)}: {ex.Message}", ex);

					_updateErrorCounter += 1;

					if (_updateErrorCounter == MaxUpdateErrors)
					{
						_msg.Error("Will stop reporting errors here to avoid flooding the logs");
					}
				}
				//else: silently ignore to avoid flooding the logs
			}
		}

		#endregion

		#region Overrides of Button

		protected override async void OnClick()
		{
			Gateway.LogEntry(_msg);

			try
			{
				bool success = await OnClickCore();

				if (! success)
				{
					_msg.Debug($"OnClickCore false for {Caption}");
				}
			}
			catch (Exception ex)
			{
				Gateway.ShowError(ex, _msg);
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
