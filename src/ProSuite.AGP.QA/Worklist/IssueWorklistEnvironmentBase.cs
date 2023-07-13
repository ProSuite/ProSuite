using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.GP;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.AGP.QA.WorkList
{
	public abstract class IssueWorkListEnvironmentBase : WorkEnvironmentBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly string _domainName = "CORRECTION_STATUS_CD";

		[CanBeNull] private readonly string _path;

		protected IssueWorkListEnvironmentBase([CanBeNull] string path)
		{
			if (path != null && path.EndsWith(".iwl", StringComparison.InvariantCultureIgnoreCase))
			{
				// It's the definition file
				string gdbPath = WorkListUtils.GetIssueGeodatabasePath(path);

				_path = gdbPath ?? throw new ArgumentException(
					        $"The issue work list {path} references a geodatabase that does not exist.");
			}
			else
			{
				_path = path;
			}
		}

		public override string FileSuffix => ".iwl";

		protected override async Task<bool> TryPrepareSchemaCoreAsync()
		{
			if (_path == null)
			{
				_msg.Debug($"{nameof(_path)} is null");
				return false;
			}

			Stopwatch watch = Stopwatch.StartNew();

			using (Geodatabase geodatabase =
			       new Geodatabase(
				       new FileGeodatabaseConnectionPath(new Uri(_path, UriKind.Absolute)))
			      )
			{
				if (geodatabase.GetDomains()
				               .Any(domain => string.Equals(_domainName, domain.GetName())))
				{
					_msg.Debug($"Domain {_domainName} already exists in {_path}");
					return true;
				}
			}

			// the GP tool is going to fail on creating a domain with the same name
			await Task.WhenAll(
				GeoprocessingUtils.CreateDomainAsync(_path, _domainName,
				                                     "Correction status for work list"),
				GeoprocessingUtils.AddCodedValueToDomainAsync(
					_path, _domainName, (int) IssueCorrectionStatus.NotCorrected, "Not Corrected"),
				GeoprocessingUtils.AddCodedValueToDomainAsync(
					_path, _domainName, (int) IssueCorrectionStatus.Corrected, "Corrected"));

			_msg.DebugStopTiming(watch, "Prepared schema - domain");

			return true;
		}

		public override void LoadLayers()
		{
			AddToMapCore(GetTablesCore());
		}

		protected override T GetContainerCore<T>()
		{
			var groupLayerName = "QA";

			GroupLayer groupLayer = MapView.Active.Map.FindLayers(groupLayerName)
			                               .OfType<GroupLayer>().FirstOrDefault();

			if (groupLayer == null)
			{
				return
					LayerFactory.Instance.CreateGroupLayer(
						MapView.Active.Map, 0, groupLayerName) as T;
			}

			return groupLayer as T;
		}

		protected override void AddToMapCore(IEnumerable<Table> tables)
		{
			var groupLayer = GetContainerCore<GroupLayer>();

			foreach (var table in tables)
			{
				_msg.DebugFormat("Adding table {0} to map...", table.GetName());

				if (table is FeatureClass fc)
				{
					FeatureLayer featureLayer =
						LayerFactory.Instance.CreateLayer<FeatureLayer>(
							new FeatureLayerCreationParams(fc), groupLayer);

					featureLayer.SetExpanded(false);
					featureLayer.SetVisibility(false);

					// TODO: Support lyrx files as symbol layers.
					// So far, just make the symbols red:	
					CIMSimpleRenderer renderer = featureLayer.GetRenderer() as CIMSimpleRenderer;

					if (renderer != null)
					{
						CIMSymbolReference symbol = renderer.Symbol;
						symbol.Symbol.SetColor(new CIMRGBColor() { R = 250 });
						featureLayer.SetRenderer(renderer);
					}

					continue;
				}

				StandaloneTableFactory.Instance.CreateStandaloneTable(
					new StandaloneTableCreationParams(table), groupLayer);
			}
		}

		// todo daro to DatasetUtils?
		protected override IEnumerable<Table> GetTablesCore()
		{
			if (string.IsNullOrEmpty(_path))
			{
				return Enumerable.Empty<Table>();
			}

			// todo daro: ensure layers are not already in map
			// todo daro: inline
			using Geodatabase geodatabase =
				new Geodatabase(
					new FileGeodatabaseConnectionPath(new Uri(_path, UriKind.Absolute)));

			return DatasetUtils.OpenTables(geodatabase, IssueGdbSchema.IssueFeatureClassNames)
			                   .ToList();
		}

		protected override async Task<Table> EnsureStatusFieldCoreAsync(Table table)
		{
			const string fieldName = "STATUS";

			Stopwatch watch = Stopwatch.StartNew();

			string path = table.GetPath().LocalPath;

			// the GP tool is not going to fail on adding a field with the same name
			// But it still takes hell of a long time...
			TableDefinition tableDefinition = table.GetDefinition();

			if (tableDefinition.FindField(fieldName) < 0)
			{
				Task<bool> addField =
					GeoprocessingUtils.AddFieldAsync(path, fieldName, "Status",
					                                 FieldType.Integer, null, null,
					                                 null, true, false, _domainName);

				Task<bool> assignDefaultValue =
					GeoprocessingUtils.AssignDefaultToFieldAsync(path, fieldName, 100);

				await Task.WhenAll(addField, assignDefaultValue);

				_msg.DebugStopTiming(watch, "Prepared schema - status field on {0}", path);
			}

			return table;
		}

		protected override IWorkList CreateWorkListCore(IWorkItemRepository repository,
		                                                string uniqueName,
		                                                string displayName)
		{
			return new IssueWorkList(repository, uniqueName, displayName);
		}

		protected override IRepository CreateStateRepositoryCore(string path, string workListName)
		{
			Type type = GetWorkListTypeCore<IssueWorkList>();

			return new XmlWorkItemStateRepository(path, workListName, type);
		}

		protected override IWorkItemRepository CreateItemRepositoryCore(
			IEnumerable<Table> tables, IRepository stateRepository)
		{
			Stopwatch watch = Stopwatch.StartNew();

			var result = new IssueItemRepository(WorkListUtils.GetDistinctTables(tables),
			                                     stateRepository);

			_msg.DebugStopTiming(watch, "Created issue work item repository");

			return result;
		}
	}
}
