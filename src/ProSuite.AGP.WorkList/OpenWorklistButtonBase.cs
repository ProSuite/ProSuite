using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;
using ProSuite.Commons.UI.Keyboard;

namespace ProSuite.AGP.WorkList
{
	[Obsolete]
	public abstract class OpenWorkListButtonBase : ButtonCommandBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		// TODO: Separate base tools with helper methods where necessary
		protected override async Task<bool> OnClickAsyncCore()
		{
			string path = GetWorklistPathCore();

			WorkEnvironmentBase environment = null;

			if (CanUseProductionModelIssueSchema() &&
			    ! KeyboardUtils.IsModifierPressed(Keys.Control, true))
			{
				environment =
					await ViewUtils.TryAsync(CreateProductionModelIssueWorkEnvironment(), _msg);
			}
			else
			{
				ViewUtils.Try(() =>
				{
					// has to be outside QueuedTask because of OpenItemDialog
					// AND outside of Task.Run because OpenItemDialog has to be
					// in UI thread.
					environment = CreateEnvironment(path);
				}, _msg);
			}

			if (environment == null)
			{
				_msg.Debug("Cannot open work list: environment is null");

				return false;
			}

			await ViewUtils.TryAsync(OpenWorklist(environment, path), _msg);

			return true;
		}

		protected virtual Task<WorkEnvironmentBase> CreateProductionModelIssueWorkEnvironment()
		{
			return null;
		}

		protected virtual bool CanUseProductionModelIssueSchema()
		{
			return false;
		}

		[CanBeNull]
		protected virtual string GetWorklistPathCore()
		{
			return null;
		}

		protected abstract Task OpenWorklist([NotNull] WorkEnvironmentBase environment,
		                                     string path = null);

		[CanBeNull]
		protected abstract WorkEnvironmentBase CreateEnvironment(string path = null);
	}
}
