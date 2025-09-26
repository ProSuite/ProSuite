using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.DomainModel.Core.DataModel.Xml;

namespace ProSuite.DomainModel.Persistence.Core.DataModel.Xml
{
	[UsedImplicitly]
	public class XmlLinearNetworksExporter : XmlLinearNetworksExchangeBase,
	                                         IXmlLinearNetworksExporter
	{
		public XmlLinearNetworksExporter([NotNull] ILinearNetworkRepository repository,
		                                 [NotNull] IUnitOfWork unitOfWork)
			: base(repository, unitOfWork) { }

		public void Export(string xmlFilePath, DdxModel model)
		{
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));
			// model may be null: don't filter by model, export all

			UnitOfWork.ReadOnlyTransaction(
				delegate
				{
					var document = new XmlLinearNetworksDocument();

					Populate(model, document);

					ExportDocument(document, xmlFilePath);
				});
		}

		#region Private methods

		private void Populate([CanBeNull] DdxModel model,
		                      [NotNull] XmlLinearNetworksDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));

			IList<LinearNetwork> networks = model == null
				                                ? Repository.GetAll()
				                                : Repository.GetByModelId(model.Id);

			foreach (LinearNetwork network in networks)
			{
				XmlLinearNetwork xml = CreateXmlNetwork(network);
				document.LinearNetworks.Add(xml);
			}

			// Sort by Network name:
			document.LinearNetworks.Sort(
				(a, b) =>
					string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
		}

		[NotNull]
		private static XmlLinearNetwork CreateXmlNetwork([NotNull] LinearNetwork network)
		{
			Assert.ArgumentNotNull(network, nameof(network));
			LinearNetworkDataset dataset = Assert.NotNull(network.NetworkDatasets.FirstOrDefault(),
			                                              "Network {0} without Network Dataset.",
			                                              network.Name);

			var xml = new XmlLinearNetwork
			          {
				          Name = network.Name,
				          Description = network.Description,
				          ModelReference = new XmlNamedEntity(dataset.Dataset.Model),
				          EnforceFlowDirection = network.EnforceFlowDirection,
				          CustomTolerance = network.CustomTolerance,
				          NetworkDatasets = GetNetworkDatasets(network.NetworkDatasets),
			          };

			return xml;
		}

		private static List<XmlLinearNetworkDataset> GetNetworkDatasets(
			IReadOnlyCollection<LinearNetworkDataset> datasets)
		{
			List<XmlLinearNetworkDataset> xmlNetworkDatasets = new List<XmlLinearNetworkDataset>();
			foreach (var dataset in datasets)
			{
				xmlNetworkDatasets.Add(new XmlLinearNetworkDataset
				                       {
					                       Dataset = dataset.Dataset.Name,
					                       WhereClause = dataset.WhereClause,
										   Splitting = dataset.Splitting,
					                       IsDefaultJunction = dataset.IsDefaultJunction,
					                       Model = dataset.Dataset.Model.Name
				                       });
			}

			return xmlNetworkDatasets;
		}

		private static void ExportDocument([NotNull] XmlLinearNetworksDocument document,
		                                   [NotNull] string xmlFilePath)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));

			XmlUtils.Serialize(document, xmlFilePath);
		}

		#endregion
	}
}
