using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using ProSuite.Commons.Collections;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.DomainModel.Core.DataModel.Xml;

namespace ProSuite.DomainModel.Persistence.Core.DataModel.Xml
{
	[UsedImplicitly]
	public class XmlLinearNetworksImporter : XmlLinearNetworksExchangeBase,
	                                         IXmlLinearNetworksImporter
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IModelRepository _models;

		public XmlLinearNetworksImporter([NotNull] ILinearNetworkRepository repository,
		                                 [NotNull] IModelRepository models,
		                                 [NotNull] IUnitOfWork unitOfWork)
			: base(repository, unitOfWork)
		{
			Assert.ArgumentNotNull(models, nameof(models));

			_models = models;
		}

		public void Import(string xmlFilePath)
		{
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));
			Assert.True(File.Exists(xmlFilePath), "File does not exist: {0}", xmlFilePath);

			XmlLinearNetworksDocument document = ReadFile(xmlFilePath);

			UnitOfWork.UseTransaction(delegate
			{
				ImportTx(document);

				UnitOfWork.Commit();
			});
		}

		#region Non-public methods

		private void ImportTx([NotNull] XmlLinearNetworksDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));

			if (document.LinearNetworks == null || document.LinearNetworks.Count < 1)
			{
				_msg.Info("No Networks in imported document");
				return;
			}

			var existing = new SimpleSet<LinearNetwork>(Repository.GetAll());
			var imports = new SimpleSet<LinearNetwork>();
			foreach (XmlLinearNetwork xmlNetwork in document.LinearNetworks)
			{
				LinearNetwork import = CreateNetwork(xmlNetwork);
				Assert.False(imports.Contains(import),
				             "Duplicate Network {0} in import file", import);
				imports.Add(import);

				LinearNetwork existingNetwork;
				if (existing.TryGetValue(import, out existingNetwork))
				{
					_msg.InfoFormat("Updating existing Network '{0}'", existingNetwork.Name);
					TransferProperties(import, existingNetwork);
				}
				else
				{
					_msg.InfoFormat("Adding new Network '{0}'", xmlNetwork.Name);
					Repository.Save(import);
					existing.Add(import);
				}
			}
		}

		[NotNull]
		private LinearNetwork CreateNetwork([NotNull] XmlLinearNetwork xml)
		{
			Assert.ArgumentNotNull(xml, nameof(xml));
			Assert.NotNullOrEmpty(xml.Name, "Imported Network has no name");
			Assert.NotNull(xml.ModelReference, "Imported Network {0} has no model reference",
			               xml.Name);

			DdxModel model = _models.Get(xml.ModelReference.Name);
			Assert.NotNull(model, "Imported Network {0} references non-existing Model {1}",
			               xml.Name, xml.ModelReference.Name);

			var network = new LinearNetwork(name: xml.Name,
			                                SetNetworkDatasets(model, xml.NetworkDatasets))
			              {
				              Description = xml.Description,
				              CustomTolerance = xml.CustomTolerance,
				              EnforceFlowDirection = xml.EnforceFlowDirection
			              };

			return network;
		}

		[NotNull]
		private static List<LinearNetworkDataset> SetNetworkDatasets(
			[NotNull] DdxModel model,
			[NotNull] IEnumerable<XmlLinearNetworkDataset> xmlNetworkDatasets)
		{
			List<LinearNetworkDataset> linearNetworkDatasets = new List<LinearNetworkDataset>();
			foreach (var xmlNetworkDataset in xmlNetworkDatasets)
			{
				VectorDataset dataset = Assert.NotNull(
					model.GetDatasetByModelName<VectorDataset>(xmlNetworkDataset.Dataset),
					"VectorDataset not found {0}", xmlNetworkDataset.Dataset);
				var linearNetworkDataset = new LinearNetworkDataset(dataset);
				linearNetworkDataset.IsDefaultJunction = xmlNetworkDataset.IsDefaultJunction;
				linearNetworkDataset.WhereClause = xmlNetworkDataset.WhereClause;
				linearNetworkDataset.Splitting = xmlNetworkDataset.Splitting;

				linearNetworkDatasets.Add(linearNetworkDataset);
			}

			return linearNetworkDatasets;
		}

		private static void TransferProperties([NotNull] LinearNetwork from,
		                                       [NotNull] LinearNetwork to)
		{
			Assert.ArgumentNotNull(from, nameof(@from));
			Assert.ArgumentNotNull(to, nameof(to));

			to.Name = from.Name;
			to.Description = from.Description;
			to.EnforceFlowDirection = from.EnforceFlowDirection;
			to.CustomTolerance = from.CustomTolerance;

			to.ClearNetworkDatasets();
			foreach (LinearNetworkDataset linearNetworkDataset in from.NetworkDatasets)
			{
				to.AddNetworkDataset(linearNetworkDataset);
			}
		}

		private static XmlLinearNetworksDocument ReadFile([NotNull] string xmlFilePath)
		{
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));

			using (var stream = new FileStream(xmlFilePath, FileMode.Open, FileAccess.Read))
			{
				var serializer = new XmlSerializer(typeof(XmlLinearNetworksDocument));

				return (XmlLinearNetworksDocument) serializer.Deserialize(stream);
			}
		}

		#endregion
	}
}
