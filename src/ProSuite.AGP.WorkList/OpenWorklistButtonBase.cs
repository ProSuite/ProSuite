using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;

namespace ProSuite.AGP.WorkList
{
	public abstract class OpenWorkListButtonBase : Button
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected override async void OnClick()
		{
			string path = null;
			WorkEnvironmentBase environment = null;

			ViewUtils.Try(() =>
			{
				path = GetWorklistPathCore();

				// has to be outside QueuedTask because of OpenItemDialog
				// AND outside of Task.Run because OpenItemDialog has to be
				// in UI thread.
				environment = CreateEnvironment(path);
			}, _msg);

			if (environment == null)
			{
				_msg.Debug("Cannot open work list: environment is null");

				return;
			}

			await ViewUtils.TryAsync(OnClickCore(environment, path), _msg);
		}

		[CanBeNull]
		protected virtual string GetWorklistPathCore()
		{
			return null;
		}

		protected abstract Task OnClickCore([NotNull] WorkEnvironmentBase environment,
		                                    string path = null);

		[CanBeNull]
		protected abstract WorkEnvironmentBase CreateEnvironment(string path = null);
	}
}
