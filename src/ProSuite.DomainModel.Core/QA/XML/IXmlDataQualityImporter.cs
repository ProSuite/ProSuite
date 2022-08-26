using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public interface IXmlDataQualityImporter
	{
		/// <summary>
		/// Updates a specified collection of quality specifications based on the specified XML file.
		/// </summary>
		/// <param name="xmlFilePath">The XML file path.</param>
		/// <param name="qualitySpecificationsToUpdate">The quality specifications to update.</param>
		/// <param name="ignoreConditionsForUnknownDatasets">Indicates if quality conditions that reference unknown datasets should be ignored.</param>
		/// <param name="updateDescriptorNames">if set to <c>true</c> the names of existing 
		/// descriptors are updated based on the names of descriptors in the xml file which have the same implementation.</param>
		/// <param name="updateDescriptorProperties">if set to <c>true</c> the other properties of existing 
		/// descriptors (except the name) are updated.</param>
		/// <returns></returns>
		[NotNull]
		IList<QualitySpecification> Import(
			[NotNull] string xmlFilePath,
			[NotNull] ICollection<QualitySpecification> qualitySpecificationsToUpdate,
			bool ignoreConditionsForUnknownDatasets,
			bool updateDescriptorNames,
			bool updateDescriptorProperties);

		/// <summary>
		/// Imports the specified XML file path.
		/// </summary>
		/// <param name="xmlFilePath">The XML file path.</param>
		/// <param name="importType">Specifies if only existing specifications should be updated, ignoring
		/// specifications in the xml file that don't exist in the data dictionary, or if
		/// all specifications in the file should be imported, updating existing ones and adding new ones.</param>
		/// <param name="ignoreConditionsForUnknownDatasets">Indicates if quality conditions that reference unknown datasets should be ignored.</param>
		/// <param name="updateDescriptorNames">if set to <c>true</c> the names of existing 
		/// descriptors are updated based on the names of descriptors in the xml file which have the same implementation.</param>
		/// <param name="updateDescriptorProperties">if set to <c>true</c> the other properties of existing 
		/// descriptors (except the name) are updated.</param>
		/// <returns></returns>
		[NotNull]
		IList<QualitySpecification> Import(
			[NotNull] string xmlFilePath,
			QualitySpecificationImportType importType,
			bool ignoreConditionsForUnknownDatasets,
			bool updateDescriptorNames,
			bool updateDescriptorProperties);

		/// <summary>
		/// Imports only the instance descriptors from the specified XML file path
		/// </summary>
		/// <param name="xmlFilePath">The XML file path.</param>
		/// <param name="updateDescriptorNames">if set to <c>true</c> the names of existing 
		/// descriptors are updated based on the names of descriptors in the xml file which have the same implementation.</param>
		/// <param name="updateDescriptorProperties">if set to <c>true</c> the other properties of existing 
		/// descriptors (except the name) are updated.</param>
		void ImportInstanceDescriptors(
			[NotNull] string xmlFilePath,
			bool updateDescriptorNames,
			bool updateDescriptorProperties);
	}
}
