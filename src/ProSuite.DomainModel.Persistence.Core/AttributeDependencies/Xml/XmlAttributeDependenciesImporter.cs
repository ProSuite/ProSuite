using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.AttributeDependencies;
using ProSuite.DomainModel.Core.AttributeDependencies.Repositories;
using ProSuite.DomainModel.Core.AttributeDependencies.Xml;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.AttributeDependencies.Xml
{
	[UsedImplicitly]
	public class XmlAttributeDependenciesImporter : XmlAttributeDependenciesExchangeBase,
	                                                IXmlAttributeDependenciesImporter
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IDatasetRepository _datasetRepository;

		public XmlAttributeDependenciesImporter(
			[NotNull] IAttributeDependencyRepository attributeDependencies,
			[NotNull] IDatasetRepository datasets,
			[NotNull] IUnitOfWork unitOfWork)
			: base(attributeDependencies, unitOfWork)
		{
			Assert.ArgumentNotNull(datasets, nameof(datasets));

			_datasetRepository = datasets;
		}

		public void Import(string xmlFilePath)
		{
			XmlAttributeDependenciesDocument document =
				XmlAttributeDependencyUtils.Deserialize(xmlFilePath);

			UnitOfWork.UseTransaction(() => ImportTx(document));
		}

		#region Non-public methods

		private void ImportTx([NotNull] XmlAttributeDependenciesDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));

			if (document.AttributeDependencies.Count < 1)
			{
				_msg.Info("No AttributeDependency in import document");
				return;
			}

			IDictionary<string, ObjectDataset> datasets = GetAllDatasets(_datasetRepository);

			foreach (XmlAttributeDependency xmlItem in document.AttributeDependencies)
			{
				ObjectDataset dataset;
				string key = GetKey(xmlItem.ModelReference.Name, xmlItem.Dataset);
				Assert.True(datasets.TryGetValue(key, out dataset),
				            "Model {0} not found or has no Dataset named {1}",
				            xmlItem.ModelReference.Name, xmlItem.Dataset);

				AttributeDependency import =
					XmlAttributeDependencyUtils.CreateAttributeDependency(xmlItem, dataset);

				AttributeDependency existing = Repository.Get(dataset);

				if (existing != null)
				{
					_msg.InfoFormat("Updating existing AttributeDependency '{0}'", import);
					XmlAttributeDependencyUtils.TransferProperties(import, existing);
				}
				else
				{
					_msg.InfoFormat("Adding new AttributeDependency '{0}'", import);
					Repository.Save(import);
				}
			}
		}

		private static IDictionary<string, ObjectDataset> GetAllDatasets(
			IDatasetRepository repository)
		{
			return
				repository.GetAll<ObjectDataset>().ToDictionary(
					ds => GetKey(ds.Model.Name, ds.Name));
		}

		private static string GetKey(string modelName, string datasetName)
		{
			return string.Concat(modelName, "\0", datasetName);
		}

		#endregion
	}
}
