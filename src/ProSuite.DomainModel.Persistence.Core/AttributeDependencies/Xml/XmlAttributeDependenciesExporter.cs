using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.AttributeDependencies;
using ProSuite.DomainModel.Core.AttributeDependencies.Repositories;
using ProSuite.DomainModel.Core.AttributeDependencies.Xml;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Persistence.Core.AttributeDependencies.Xml
{
	[UsedImplicitly]
	public class XmlAttributeDependenciesExporter : XmlAttributeDependenciesExchangeBase,
	                                                IXmlAttributeDependenciesExporter
	{
		public XmlAttributeDependenciesExporter(
			[NotNull] IAttributeDependencyRepository attributeDependencies,
			[NotNull] IUnitOfWork unitOfWork)
			: base(attributeDependencies, unitOfWork) { }

		public void Export(string xmlFilePath,
		                   AttributeDependency entity)
		{
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));
			Assert.ArgumentNotNull(entity, nameof(entity));

			UnitOfWork.ReadOnlyTransaction(
				delegate
				{
					XmlAttributeDependenciesDocument document =
						XmlAttributeDependencyUtils.CreateXmlAttributeDependenciesDocument(
							new List<AttributeDependency> {entity});

					XmlAttributeDependencyUtils.ExportDocument(document, xmlFilePath);
				});
		}

		public void Export(string xmlFilePath, DdxModel model)
		{
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));
			// model may be null: don't filter by model, export all

			UnitOfWork.ReadOnlyTransaction(
				delegate
				{
					IList<AttributeDependency> dependencies = model == null
						                                          ? Repository.GetAll()
						                                          : Repository.GetByModelId(model.Id);

					XmlAttributeDependenciesDocument document =
						XmlAttributeDependencyUtils
							.CreateXmlAttributeDependenciesDocument(dependencies);

					XmlAttributeDependencyUtils.ExportDocument(document, xmlFilePath);
				});
		}
	}
}
