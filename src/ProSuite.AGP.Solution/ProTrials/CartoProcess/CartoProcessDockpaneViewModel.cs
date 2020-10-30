using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.ProTrials.CartoProcess
{
	internal class CartoProcessDockpaneViewModel : DockPane
	{
		private const string _dockPaneID = "ProSuite_ProTrials_CartoProcessDockpane";

		protected CartoProcessDockpaneViewModel() { }

		internal static void Show()
		{
			DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
			if (pane == null)
				return;

			pane.Activate();
		}

		private string _databaseName = "";
		public string DatabaseName
		{
			get => _databaseName;
			set
			{
				SetProperty(ref _databaseName, value, () => DatabaseName);
			}
		}

		public IList<GDBProjectItem> DatabaseItems
		{
			get => GetDatabaseItems();
		}

		private string _processName = "";
		public string ProcessName
		{
			get => _processName;
			set
			{
				SetProperty(ref _processName, value, () => ProcessName);
			}
		}

		public IList<CartoProcessItem> ProcessItems
		{
			get => GetProcessItems().ToList();
		}

		private string _configText = "Parameter = Value\r\nParameter = Value\r\netc.";
		public string ConfigText
		{
			get => _configText;
			set
			{
				SetProperty(ref _configText, value, () => ConfigText);
			}
		}

		private ICommand _runCommand;
		public ICommand RunCommand =>
			_runCommand ?? (_runCommand = new RelayCommand(RunProcess));

		private string _statusMessage = "";
		public string StatusMessage
		{
			get => _statusMessage;
			set
			{
				SetProperty(ref _statusMessage, value, () => StatusMessage);
			}
		}

		private Brush _statusColor = Brushes.LemonChiffon;
		public Brush StatusColor
		{
			get => _statusColor;
			set
			{
				SetProperty(ref _statusColor, value, () => StatusColor);
			}
		}

		private void RunProcess()
		{
			try
			{
				var config = CartoProcessConfig.FromString(ProcessName, ConfigText);

				var process = new AlignMarkers(); // TODO get by ProcessName from some repo
				bool isValid = process.Validate(config);

				var task = QueuedTask.Run(() => {}); // TODO remainder on MCT

				process.Initialize(config);

				var geodatabase = GetGeodatabase(DatabaseName);

				var context = new ProProcessingContext(geodatabase);
				var feedback = new ProProcessingFeedback();

				var canExecute = process.CanExecute(context);
				process.Execute(context, feedback);

				StatusMessage = "Completed";
				StatusColor = Brushes.PaleGreen;
			}
			catch (Exception ex)
			{
				StatusMessage = ex.Message;
				StatusColor = Brushes.LightSalmon;
			}
		}

		[CanBeNull]
		private static Geodatabase GetGeodatabase(string name)
		{
			GDBProjectItem item;
			if (string.IsNullOrWhiteSpace(name))
			{
				item = Project.Current.GetItems<GDBProjectItem>()
				              .FirstOrDefault(gdb => gdb.IsDefault && ! gdb.IsInvalid);
			}
			else
			{
				item = Project.Current.GetItems<GDBProjectItem>()
				              .FirstOrDefault(gdb => gdb.Name == name);
			}

			return item?.GetDatastore() as Geodatabase; // MCT
		}

		[NotNull]
		private static IList<GDBProjectItem> GetDatabaseItems()
		{
			return Project.Current.GetItems<GDBProjectItem>().ToList();
		}

		[NotNull]
		private static IEnumerable<CartoProcessItem> GetProcessItems()
		{
			yield return new CartoProcessItem {Name = nameof(AlignMarkers)};
			yield return new CartoProcessItem {Name = nameof(CreateAnnoMasks)};
		}
	}

	public class CartoProcessItem
	{
		public string Name { get; set; }
	}

	/// <summary>
	/// Button implementation to show the DockPane.
	/// </summary>
	internal class CartoProcessDockpane_ShowButton : Button
	{
		protected override void OnClick()
		{
			CartoProcessDockpaneViewModel.Show();
		}
	}
}
