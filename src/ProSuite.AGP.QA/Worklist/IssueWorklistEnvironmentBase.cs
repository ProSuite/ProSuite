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

namespace ProSuite.AGP.QA.Worklist
{
	public abstract class IssueWorklistEnvironmentBase : WorkEnvironmentBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly string _domainName = "CORRECTION_STATUS_CD";

		private readonly List<string> _issueFeatureClassNames = new List<string>
		                                                        {
			                                                        "IssueLines",
			                                                        "IssueMultiPatches",
			                                                        "IssuePoints", "IssuePolygons",
			                                                        "IssueRows"
		                                                        };

		[CanBeNull] private readonly string _path;

		protected IssueWorklistEnvironmentBase([CanBeNull] string path)
		{
			_path = path;
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
				new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(_path, UriKind.Absolute)))
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
					_path, _domainName, 100, "Not Corrected"),
				GeoprocessingUtils.AddCodedValueToDomainAsync(
					_path, _domainName, 200, "Corrected"));

			return true;
		}

		protected override IEnumerable<BasicFeatureLayer> GetLayers(Map map)
		{
			if (string.IsNullOrEmpty(_path))
			{
				yield break;
			}

			// todo daro: ensure layers are not already in map
			using (Geodatabase geodatabase =
				new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(_path, UriKind.Absolute)))
			)
			{
				IEnumerable<string> featureClassNames =
					geodatabase.GetDefinitions<FeatureClassDefinition>()
					           .Select(definition => definition.GetName())
					           .Where(name => _issueFeatureClassNames.Contains(name));

				foreach (string featureClassName in featureClassNames)
				{
					using (var featureClass =
						geodatabase.OpenDataset<FeatureClass>(featureClassName))
					{
						FeatureLayer featureLayer = LayerFactory.Instance.CreateFeatureLayer(
							featureClass, MapView.Active.Map, LayerPosition.AddToTop);

						featureLayer.SetExpanded(false);
						featureLayer.SetVisibility(false);

						yield return featureLayer;
					}
				}
			}
		}

		protected override async Task<BasicFeatureLayer> EnsureStatusFieldCoreAsync(
			BasicFeatureLayer featureLayer)
		{
			const string fieldName = "STATUS";

			// the GP tool is not going to fail on adding a field with the same name
			Task<bool> addField =
				GeoprocessingUtils.AddFieldAsync(featureLayer.Name, fieldName, "Status",
				                                 esriFieldType.esriFieldTypeInteger, null, null,
				                                 null, true, false, _domainName);

			Task<bool> assignDefaultValue =
				GeoprocessingUtils.AssignDefaultToFieldAsync(featureLayer.Name, fieldName, 100);

			await Task.WhenAll(addField, assignDefaultValue);

			return featureLayer;
		}

		protected override IWorkList CreateWorkListCore(IWorkItemRepository repository, string name)
		{
			return new IssueWorkList(repository, name);
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