using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Xml;
using ProSuite.Commons.Collections;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.DomainModel.Core.QA.Xml;

namespace ProSuite.DomainModel.Persistence.Core.QA.Xml
{
	[UsedImplicitly]
	public class XmlDataQualityExporter : XmlDataQualityExchangeBase,
	                                      IXmlDataQualityExporter
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlDataQualityExporter"/> class.
		/// </summary>
		/// <param name="instanceConfigurations">The instance configurations repository.</param>
		/// <param name="instanceDescriptors">The instance descriptor repository.</param>
		/// <param name="qualitySpecifications">The quality specifications repository.</param>
		/// <param name="categories">The data quality category repository</param>
		/// <param name="datasets">The datasets repository.</param>
		/// <param name="unitOfWork">The unit of work.</param>
		/// <param name="workspaceConverter">The workspace converter implementation that creates
		/// XML workspaces from models.</param>
		public XmlDataQualityExporter(
			[NotNull] IInstanceConfigurationRepository instanceConfigurations,
			[NotNull] IInstanceDescriptorRepository instanceDescriptors,
			[NotNull] IQualitySpecificationRepository qualitySpecifications,
			[CanBeNull] IDataQualityCategoryRepository categories,
			[NotNull] IDatasetRepository datasets,
			[NotNull] IUnitOfWork unitOfWork,
			[NotNull] IXmlWorkspaceConverter workspaceConverter)
			: base(instanceConfigurations, instanceDescriptors, qualitySpecifications,
			       categories, datasets, unitOfWork, workspaceConverter) { }

		#endregion

		#region IXmlDataQualityExporter Members

		public void Export(QualitySpecification qualitySpecification,
		                   string xmlFilePath,
		                   bool exportMetadata,
		                   bool? exportWorkspaceConnections,
		                   bool exportConnectionFilePaths,
		                   bool exportAllDescriptors,
		                   bool exportAllCategories,
		                   bool exportNotes)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));

			Export(new[] { qualitySpecification }, xmlFilePath,
			       exportMetadata,
			       exportWorkspaceConnections,
			       exportConnectionFilePaths,
			       exportAllDescriptors,
			       exportAllCategories,
			       exportNotes);
		}

		public void Export(IEnumerable<QualitySpecification> qualitySpecifications,
		                   string xmlFilePath,
		                   bool exportMetadata,
		                   bool? exportWorkspaceConnections,
		                   bool exportConnectionFilePaths,
		                   bool exportAllDescriptors,
		                   bool exportAllCategories,
		                   bool exportNotes)
		{
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));

			CultureInfo origCulture = Thread.CurrentThread.CurrentCulture;

			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

				UnitOfWork.ReadOnlyTransaction(
					delegate
					{
						ICollection<QualitySpecification> collection =
							CollectionUtils.GetCollection(qualitySpecifications);

						Reattach(collection);

						IList<InstanceDescriptor> descriptors =
							exportAllDescriptors
								? GetAllInstanceDescriptors()
								: null;

						IList<DataQualityCategory> categories =
							exportAllCategories && Categories != null
								? Categories.GetAll()
								: null;

						CreateXmlDataQualityDocument(
							collection,
							descriptors,
							categories,
							exportMetadata,
							exportWorkspaceConnections ?? ExportWorkspaceConnections,
							exportConnectionFilePaths,
							exportAllDescriptors,
							exportAllCategories,
							exportNotes,
							out XmlDataQualityDocument30 document);

						ExportXmlDocument(document, xmlFilePath);
					});
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = origCulture;
			}
		}

		public bool ExportWorkspaceConnections { get; set; } = true;

		#endregion

		#region Non-public members

		private void CreateXmlDataQualityDocument<T>(
			[NotNull] ICollection<QualitySpecification> qualitySpecifications,
			[CanBeNull] IList<InstanceDescriptor> descriptors,
			[CanBeNull] IList<DataQualityCategory> categories,
			bool exportMetadata,
			bool exportWorkspaceConnections,
			bool exportConnectionFilePaths,
			bool exportAllDescriptors,
			bool exportAllCategories,
			bool exportNotes,
			out T result)
			where T : XmlDataQualityDocument, new()
		{
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));

			result = new T();

			IDictionary<DdxModel, string> workspaceIdsByModel =
				AddWorkspaces(qualitySpecifications,
				              result,
				              exportWorkspaceConnections,
				              exportConnectionFilePaths);

			XmlDataQualityUtils.Populate(result,
			                             workspaceIdsByModel,
			                             qualitySpecifications.ToList(),
			                             descriptors,
			                             categories,
			                             exportMetadata,
			                             exportAllDescriptors,
			                             exportAllCategories,
			                             exportNotes);
		}

		[NotNull]
		private IDictionary<DdxModel, string> AddWorkspaces(
			[NotNull] IEnumerable<QualitySpecification> qualitySpecifications,
			[NotNull] XmlDataQualityDocument document,
			bool exportWorkspaceConnections,
			bool exportConnectionFilePaths)
		{
			var result = new Dictionary<DdxModel, string>();

			foreach (QualitySpecification qualitySpecification in qualitySpecifications)
			{
				foreach (QualityCondition qualityCondition in qualitySpecification.Elements.Select(
					         element => element.QualityCondition))
				{
					foreach (Dataset dataset in qualityCondition.GetDatasetParameterValues(
						         true, true))
					{
						DdxModel model = dataset.Model;
						if (result.ContainsKey(model))
						{
							continue;
						}

						XmlWorkspace xmlWorkspace = WorkspaceConverter.CreateXmlWorkspace(
							model, exportWorkspaceConnections, exportConnectionFilePaths);

						document.AddWorkspace(xmlWorkspace);

						result.Add(model, xmlWorkspace.ID);
					}
				}
			}

			return result;
		}

		private static void ExportXmlDocument<T>(
			[NotNull] T document, [NotNull] string xmlFilePath)
			where T : XmlDataQualityDocument
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));

			using (XmlWriter xmlWriter =
			       XmlWriter.Create(xmlFilePath, XmlUtils.GetWriterSettings()))
			{
				XmlDataQualityUtils.ExportXmlDocument(document, xmlWriter);
			}
		}

		#endregion
	}
}
