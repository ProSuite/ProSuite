using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList
{
	public abstract class OpenWorkListButtonBase : Button
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected override async void OnClick()
		{
			// has to be outside QueuedTask because of OpenItemDialog
			// AND ouside of Task.Run because OptenItemDialog has to be
			// in UI thread.
			WorkEnvironmentBase environment = null;

			ViewUtils.Try(() => { environment = CreateEnvironment(); }, _msg);

			await ViewUtils.TryAsync(OnClickCore(environment), _msg);
		}

		protected abstract Task OnClickCore([NotNull] WorkEnvironmentBase environment);

		[CanBeNull]
		protected abstract WorkEnvironmentBase CreateEnvironment(string path = null);
	}
}
