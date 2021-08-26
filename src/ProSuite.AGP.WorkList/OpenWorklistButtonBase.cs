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
			WorkEnvironmentBase environment = CreateEnvironment();

			await ViewUtils.TryAsync(OnClickCore(environment), _msg);
		}

		protected abstract Task OnClickCore(WorkEnvironmentBase environment);

		[CanBeNull]
		protected abstract WorkEnvironmentBase CreateEnvironment(string path = null);
	}
}
