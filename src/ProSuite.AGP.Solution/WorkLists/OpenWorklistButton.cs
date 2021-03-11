using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	internal class OpenWorklistButton : Button
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected override async void OnClick()
		{
			await ViewUtils.TryAsync(async () =>
			{
				var window = FrameworkApplication.ActiveWindow as IProjectWindow;
				string path = window?.SelectedItems.FirstOrDefault()?.Path;

				if (string.IsNullOrEmpty(path))
				{
					return;
				}

				_msg.Debug($"Open work list from file {path}");

				string workListName = WorkListUtils.GetName(path);

				WorkEnvironmentBase environment = CreateEnvironment(path);

				if (environment == null)
				{
					return;
				}

				await QueuedTask.Run(() => WorkListsModule.Current.CreateWorkListAsync(environment, workListName));

				WorkListsModule.Current.ShowView(workListName);
			}, _msg);
		}

		[CanBeNull]
		public static WorkEnvironmentBase CreateEnvironment([NotNull] string path)
		{
			if (! File.Exists(path))
			{
				return null;
			}

			string extension = Path.GetExtension(path);

			if (string.Equals(extension, ".swl"))
			{
				return new InMemoryWorkEnvironment();
			}

			if (string.Equals(extension, ".iwl"))
			{
				throw new NotImplementedException("issue work list is not supported yet");
			}

			return null;
		}
	}
}
