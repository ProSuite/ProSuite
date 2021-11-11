using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList
{
	public abstract class OpenWorklistButtonBase : Button
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected override async void OnClick()
		{
			// has to be outside QueuedTask because of OpenItemDialog
			// AND ouside of Task.Run because OptenItemDialog has to be
			// in UI thread.

			string path = null;
			WorkEnvironmentBase environment = null;

			ViewUtils.Try(() =>
			{
				path = GetWorklistPathCore();

				environment = CreateEnvironment(path);
			}, _msg);

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
