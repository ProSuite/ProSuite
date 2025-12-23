using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Selection
{
	public abstract class SelectionWorkListEnvironmentBase : WorkEnvironmentBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected override string FileSuffix => ".swl";

		public override string GetDisplayName()
		{
			string currentName = Path.GetFileNameWithoutExtension(Project.Current.Name);
			var now = DateTime.Now.ToString("yyyy_MM_dd_HHmmss");

			return $"{currentName}_{now}";
		}

		protected override T GetLayerContainerCore<T>(MapView mapView)
		{
			return mapView.Map as T;
		}

		protected override IWorkItemStateRepository CreateStateRepositoryCore(
			string path, string workListName, string displayName)
		{
			Type type = GetWorkListTypeCore<SelectionWorkList>();

			return new XmlSelectionItemStateRepository(path, workListName, displayName, type);
		}

		protected override Task<IWorkItemRepository> CreateItemRepositoryCoreAsync(
			IWorkItemStateRepository stateRepository)
		{
			// todo: (daro) inject map as parameter. If layer is in toc
			// WorkItemTable is called before MapView.Active is initialized.
			Map map = MapView.Active.Map;

			var watch = Stopwatch.StartNew();

			Task<IWorkItemRepository> result;

			try
			{
				string path = stateRepository.WorkListDefinitionFilePath;

				IList<ISourceClass> sourceClasses;

				if (File.Exists(path))
				{
					XmlWorkListDefinition definition = XmlWorkItemStateRepository.Import(path);

					sourceClasses = WorkListUtils.CreateSourceClasses(map, definition).ToList();
				}
				else
				{
					sourceClasses = WorkListUtils.CreateSourceClasses(map).ToList();
				}

				result = Task.FromResult<IWorkItemRepository>(
					new SelectionItemRepository(sourceClasses, stateRepository));
			}
			finally
			{
				_msg.DebugStopTiming(watch, "Created selection work item repository");
			}

			return result;
		}

		protected override IWorkList CreateWorkListCore(IWorkItemRepository repository,
		                                                string uniqueName,
		                                                string displayName)
		{
			return new SelectionWorkList(repository, new MapViewContext(), GetAreaOfInterest(),
			                             uniqueName, displayName);
		}
	}
}
