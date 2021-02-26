using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Processing;
using ProSuite.Processing.Domain;

namespace ProSuite.AGP.CartoTrials.CartoProcess
{
	internal class CartoProcessDockpaneViewModel : DockPane
	{
		private const string _dockPaneID = "ProSuite_CartoTrials_CartoProcessDockpane";

		protected CartoProcessDockpaneViewModel() { }

		internal static void Show()
		{
			DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);

			pane?.Activate();
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

		private ProcessSelectionType _selectionType = ProcessSelectionType.SelectedFeatures;
		public ProcessSelectionType SelectionType
		{
			get => _selectionType;
			set
			{
				SetProperty(ref _selectionType, value, () => SelectionType);
			}
		}

		public IList<ProcessSelectionType> SelectionTypes
		{
			get => GetSelectionTypes().ToList();
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

		private async void RunProcess()
		{
			try
			{
				var config = CartoProcessConfig.FromString(ProcessName, ConfigText);

				var process = GetCartoProcess(ProcessName);
				if (process == null)
					throw new Exception($"No such carto process: {ProcessName}");

				bool isValid = process.Validate(config);
				if (!isValid)
					throw new Exception($"Config is not valid for process {ProcessName}");

				var gdbItem = GetDatabaseItem(DatabaseName);
				if (gdbItem == null)
					throw new Exception($"No such database item in project: {DatabaseName}");

				var edop = new EditOperation();

				await QueuedTask.Run(() =>
				{
					process.Initialize(config);

					using (var geodatabase = (Geodatabase) gdbItem.GetDatastore())
					{
						var context = new ProProcessingContext(geodatabase, MapView.Active);
						var feedback = new ProProcessingFeedback();

						context.SelectionType = SelectionType;

						var canExecute = process.CanExecute(context);
						process.Execute(context, feedback);
					}
				});

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
		private static CartoProcess GetCartoProcess(string name)
		{
			// TODO need some type search and Activator logic
			switch (name)
			{
				case nameof(AlignMarkers):
					return new AlignMarkers();
				case nameof(CalculateControlPoints):
					return new CalculateControlPoints();
				case nameof(CreateAnnoMasks):
					return new CreateAnnoMasks();
				default:
					return null;
			}
		}

		[CanBeNull]
		private static GDBProjectItem GetDatabaseItem(string name)
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

			return item;
		}

		[NotNull]
		private static IList<GDBProjectItem> GetDatabaseItems()
		{
			var list = Project.Current.GetItems<GDBProjectItem>().ToList();
			return list;
		}

		[NotNull]
		private static IEnumerable<CartoProcessItem> GetProcessItems()
		{
			yield return new CartoProcessItem {Name = nameof(AlignMarkers)};
			yield return new CartoProcessItem {Name = nameof(CreateAnnoMasks)};
			yield return new CartoProcessItem {Name = nameof(CalculateControlPoints)};
		}

		[NotNull]
		private static IEnumerable<ProcessSelectionType> GetSelectionTypes()
		{
			yield return ProcessSelectionType.SelectedFeatures;
			yield return ProcessSelectionType.VisibleExtent;
			yield return ProcessSelectionType.AllFeatures;
			// TODO ... within Perimeter
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
