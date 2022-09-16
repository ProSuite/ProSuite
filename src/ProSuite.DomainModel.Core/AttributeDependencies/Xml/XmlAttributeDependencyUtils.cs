using System;
using System.Collections.Generic;
using System.IO;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.Schemas;
using Attribute = ProSuite.DomainModel.Core.DataModel.Attribute;

namespace ProSuite.DomainModel.Core.AttributeDependencies.Xml
{
	public static class XmlAttributeDependencyUtils
	{
		[NotNull]
		public static XmlAttributeDependenciesDocument Deserialize(
			[NotNull] string xmlFilePath)
		{
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));
			Assert.ArgumentCondition(File.Exists(xmlFilePath),
			                         "File does not exist: {0}", xmlFilePath);

			string schema = Schema.ProSuite_AttributeDependencies_1_0;

			try
			{
				return XmlUtils.DeserializeFile<XmlAttributeDependenciesDocument>(xmlFilePath,
				                                                                  schema);
			}
			catch (Exception ex)
			{
				throw new XmlDeserializationException(
					$"Error deserializing file: {ex.Message}", ex);
			}
		}

		public static void ExportDocument(
			[NotNull] XmlAttributeDependenciesDocument document,
			[NotNull] string xmlFilePath)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));

			XmlUtils.Serialize(document, xmlFilePath);
		}

		[NotNull]
		public static XmlAttributeDependenciesDocument
			CreateXmlAttributeDependenciesDocument(
				[NotNull] IEnumerable<AttributeDependency> dependencies)
		{
			Assert.ArgumentNotNull(dependencies, nameof(dependencies));

			var document = new XmlAttributeDependenciesDocument();

			foreach (AttributeDependency dependency in dependencies)
			{
				XmlAttributeDependency xml = CreateXmlAttributeDependency(dependency);

				document.AttributeDependencies.Add(xml);
			}

			// Sort by Dataset name:
			document.AttributeDependencies.Sort(
				(a, b) =>
					string.Compare(a.Dataset, b.Dataset,
					               StringComparison.OrdinalIgnoreCase));

			return document;
		}

		[NotNull]
		public static AttributeDependency CreateAttributeDependency(
			[NotNull] XmlAttributeDependency xml, [NotNull] ObjectDataset dataset)
		{
			Assert.ArgumentNotNull(xml, nameof(xml));
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			var result = new AttributeDependency(dataset);

			foreach (XmlAttribute xmlAttribute in xml.SourceAttributes)
			{
				ObjectAttribute attribute = Assert.NotNull(
					dataset.GetAttribute(xmlAttribute.Name),
					"Dataset {0} has no Attribute named {1}",
					dataset, xmlAttribute.Name);
				result.SourceAttributes.Add(attribute);
			}

			foreach (XmlAttribute xmlAttribute in xml.TargetAttributes)
			{
				ObjectAttribute attribute = Assert.NotNull(
					dataset.GetAttribute(xmlAttribute.Name),
					"Dataset {0} has no Attribute named {1}",
					dataset, xmlAttribute.Name);
				result.TargetAttributes.Add(attribute);
			}

			foreach (XmlAttributeValueMapping xmlPair in xml.AttributeValueMappings)
			{
				var mapping = new AttributeValueMapping(
					xmlPair.SourceText, xmlPair.TargetText, xmlPair.Description);

				result.AttributeValueMappings.Add(mapping);
			}

			return result;
		}

		[NotNull]
		public static XmlAttributeDependency CreateXmlAttributeDependency(
			[NotNull] AttributeDependency dependency)
		{
			Assert.ArgumentNotNull(dependency, nameof(dependency));

			var xml = new XmlAttributeDependency
			          {
				          ModelReference = new XmlNamedEntity(dependency.Dataset.Model),
				          Dataset = dependency.Dataset.Name
			          };

			foreach (Attribute attribute in dependency.SourceAttributes)
			{
				xml.SourceAttributes.Add(new XmlAttribute {Name = attribute.Name});
			}

			foreach (Attribute attribute in dependency.TargetAttributes)
			{
				xml.TargetAttributes.Add(new XmlAttribute {Name = attribute.Name});
			}

			foreach (AttributeValueMapping mapping in dependency.AttributeValueMappings)
			{
				var xmlMapping = new XmlAttributeValueMapping
				                 {
					                 SourceText = mapping.SourceText,
					                 TargetText = mapping.TargetText,
					                 Description = mapping.Description
				                 };
				xml.AttributeValueMappings.Add(xmlMapping);
			}

			// Sort by SourceText (expected to be unique anyway)
			//	use NumericStringComparer for better results:
			var comparer = new NumericStringComparer();
			xml.AttributeValueMappings.Sort((a, b) =>
				                                comparer.Compare(a.SourceText, b.SourceText));

			return xml;
		}

		public static void TransferProperties([NotNull] AttributeDependency from,
		                                      [NotNull] AttributeDependency to)
		{
			Assert.ArgumentNotNull(from, nameof(from));
			Assert.ArgumentNotNull(to, nameof(to));

			to.SourceAttributes.Clear();
			foreach (Attribute attribute in from.SourceAttributes)
			{
				to.SourceAttributes.Add(attribute);
			}

			to.TargetAttributes.Clear();
			foreach (Attribute attribute in from.TargetAttributes)
			{
				to.TargetAttributes.Add(attribute);
			}

			to.AttributeValueMappings.Clear();
			foreach (AttributeValueMapping pair in from.AttributeValueMappings)
			{
				to.AttributeValueMappings.Add(pair);
			}
		}
	}
}
