using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.GP;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AGP.QA;

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

			return true;
		}

		public override IEnumerable<BasicFeatureLayer> LoadLayers()
		{
			return GetLayersCore(GetFeatureClassesCore());
		}

		protected override ILayerContainerEdit GetContainer()
		{
			var groupLayerName = "QA";

			GroupLayer groupLayer = MapView.Active.Map.FindLayers(groupLayerName)
			                               .OfType<GroupLayer>().FirstOrDefault();

			return groupLayer ??
			       LayerFactory.Instance.CreateGroupLayer(MapView.Active.Map, 0, groupLayerName);
		}

		protected override IEnumerable<BasicFeatureLayer> GetLayersCore(
			IEnumerable<FeatureClass> featureClasses)
		{
			ILayerContainerEdit layerContainer = GetContainer();

			if (layerContainer == null)
			{
				return Enumerable.Empty<BasicFeatureLayer>();
			}

			return featureClasses.Select(fc =>
			{
				FeatureLayer featureLayer =
					LayerFactory.Instance.CreateFeatureLayer(fc, layerContainer);

				featureLayer.SetExpanded(false);

				return featureLayer;
			});
		}

		protected override IEnumerable<FeatureClass> GetFeatureClassesCore()
		{
			if (string.IsNullOrEmpty(_path))
			{
				yield break;
			}

			// todo daro: ensure layers are not already in map
			using (Geodatabase geodatabase =
			       new Geodatabase(
				       new FileGeodatabaseConnectionPath(new Uri(_path, UriKind.Absolute))))
			{
				IEnumerable<string> featureClassNames =
					geodatabase.GetDefinitions<FeatureClassDefinition>()
					           .Select(definition => definition.GetName())
					           .Where(name => IssueGdbSchema.IssueFeatureClassNames.Contains(name));

				foreach (string featureClassName in featureClassNames)
				{
					var featureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName);

					yield return featureClass;
				}
			}
		}

		protected override async Task<FeatureClass> EnsureStatusFieldCoreAsync(
			FeatureClass featureClass)
		{
			const string fieldName = "STATUS";

			string path = featureClass.GetPath().LocalPath;

			// the GP tool is not going to fail on adding a field with the same name
			Task<bool> addField =
				GeoprocessingUtils.AddFieldAsync(path, fieldName, "Status",
				                                 esriFieldType.esriFieldTypeInteger, null, null,
				                                 null, true, false, _domainName);

			Task<bool> assignDefaultValue =
				GeoprocessingUtils.AssignDefaultToFieldAsync(path, fieldName, 100);

			await Task.WhenAll(addField, assignDefaultValue);

			return featureClass;
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
			IEnumerable<BasicFeatureLayer> featureLayers, IRepository stateRepository)
		{
			Dictionary<Geodatabase, List<Table>> tables = MapUtils.GetDistinctTables(featureLayers);

			return new IssueItemRepository(tables, stateRepository);
		}
	}
}
