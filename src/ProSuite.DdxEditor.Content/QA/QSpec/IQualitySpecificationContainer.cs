using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public interface IQualitySpecificationContainer
	{
		[NotNull]
		IEnumerable<Item> GetQualitySpecificationItems(
			[NotNull] IQualitySpecificationContainerItem containerItem);

		[NotNull]
		IEnumerable<QualitySpecification> GetQualitySpecifications(
			bool includeSubCategories = false);

		[NotNull]
		QualitySpecificationItem CreateQualitySpecificationItem(
			[NotNull] IQualitySpecificationContainerItem containerItem);

		void ExportDatasetDependencies(
			[NotNull] ICollection<KeyValuePair<string, ICollection<QualitySpecification>>>
				qualitySpecificationsByFileName,
			[NotNull] IEnumerable<string> deletableFiles,
			[NotNull] ExportDatasetDependenciesOptions options);

		void ExportQualitySpecifications(
			[NotNull] IDictionary<string, ICollection<QualitySpecification>>
				specificationsByFileName,
			[NotNull] ICollection<string> deletableFiles,
			bool exportMetadata,
			bool? exportWorkspaceConnections,
			bool exportConnectionFilePaths,
			bool exportAllTestDescriptors,
			bool exportAllCategories,
			bool exportNotes);

		void ImportQualitySpecifications([NotNull] string fileName,
		                                 bool ignoreConditionsForUnknownDatasets,
		                                 bool updateTestDescriptorNames,
		                                 bool updateTestDescriptorProperties);
	}
}