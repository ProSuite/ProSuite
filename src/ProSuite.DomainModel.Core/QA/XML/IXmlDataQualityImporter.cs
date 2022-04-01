using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.XML
{
	public interface IXmlDataQualityImporter
	{
		/// <summary>
		/// Updates a specified collection of quality specifications based on the specified XML file.
		/// </summary>
		/// <param name="xmlFilePath">The XML file path.</param>
		/// <param name="qualitySpecificationsToUpdate">The quality specifications to update.</param>
		/// <param name="ignoreConditionsForUnknownDatasets">Indicates if quality conditions that reference unknown datasets should be ignored.</param>
		/// <param name="updateTestDescriptorNames">if set to <c>true</c> the names of existing 
		/// test descriptors are updated based on the names of test descriptors in the xml file which have the same implementation.</param>
		/// <param name="updateTestDescriptorProperties">if set to <c>true</c> the other properties of existing 
		/// test descriptors (except the name) are updated.</param>
		/// <returns></returns>
		[NotNull]
		IList<QualitySpecification> Import(
			[NotNull] string xmlFilePath,
			[NotNull] ICollection<QualitySpecification> qualitySpecificationsToUpdate,
			bool ignoreConditionsForUnknownDatasets,
			bool updateTestDescriptorNames,
			bool updateTestDescriptorProperties);

		/// <summary>
		/// Imports the specified XML file path.
		/// </summary>
		/// <param name="xmlFilePath">The XML file path.</param>
		/// <param name="importType">Specifies if only existing specifications should be updated, ignoring
		/// specifications in the xml file that don't exist in the data dictionary, or if
		/// all specifications in the file should be imported, updating existing ones and adding new ones.</param>
		/// <param name="ignoreConditionsForUnknownDatasets">Indicates if quality conditions that reference unknown datasets should be ignored.</param>
		/// <param name="updateTestDescriptorNames">if set to <c>true</c> the names of existing 
		/// test descriptors are updated based on the names of test descriptors in the xml file which have the same implementation.</param>
		/// <param name="updateTestDescriptorProperties">if set to <c>true</c> the other properties of existing 
		/// test descriptors (except the name) are updated.</param>
		/// <returns></returns>
		[NotNull]
		IList<QualitySpecification> Import(
			[NotNull] string xmlFilePath,
			QualitySpecificationImportType importType,
			bool ignoreConditionsForUnknownDatasets,
			bool updateTestDescriptorNames,
			bool updateTestDescriptorProperties);

		/// <summary>
		/// Imports the test descriptors only from the specified XML file path
		/// </summary>
		/// <param name="xmlFilePath">The XML file path.</param>
		/// <param name="updateTestDescriptorNames">if set to <c>true</c> the names of existing 
		/// test descriptors are updated based on the names of test descriptors in the xml file which have the same implementation.</param>
		/// <param name="updateTestDescriptorProperties">if set to <c>true</c> the other properties of existing 
		/// test descriptors (except the name) are updated.</param>
		void ImportTestDescriptors(
			[NotNull] string xmlFilePath,
			bool updateTestDescriptorNames,
			bool updateTestDescriptorProperties);
	}
}