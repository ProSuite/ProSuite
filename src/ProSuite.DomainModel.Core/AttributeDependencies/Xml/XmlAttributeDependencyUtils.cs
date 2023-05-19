using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProSuite.Commons.AttributeDependencies;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Db;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Text;
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

		#region Import/Export AttributeValueMappings

		private const string _delimiters = ",;";

		public static void ExportMappingsTxt([NotNull] AttributeDependency dependency,
		                                     [NotNull] TextWriter writer)
		{
			Assert.ArgumentNotNull(dependency, nameof(dependency));
			Assert.ArgumentNotNull(writer, nameof(writer));

			writer.WriteLine("# Lines starting with a hash are ignored.");
			writer.WriteLine("#");
			writer.WriteLine("# When writing numbers, use \"InvariantCulture\", that is:");
			writer.WriteLine("#    period (.) as decimal separator and no thousands separator.");
			writer.WriteLine("#    Correct example: 1234.5; bad example: 1'234,5.");
			writer.WriteLine("#");

			var sb = new StringBuilder();

			// # one, two, three => four, five # description
			writer.WriteLine(GetHeaderText(dependency, sb));

			foreach (AttributeValueMapping mapping in dependency.AttributeValueMappings)
			{
				sb.Length = 0; // clear
				sb.Append(mapping.SourceText);
				sb.Append(" => ");
				sb.Append(mapping.TargetText);
				if (! string.IsNullOrEmpty(mapping.Description))
				{
					sb.AppendFormat(" # {0}", mapping.Description);
				}

				writer.WriteLine(sb.ToString());
			}
		}

		private static string GetHeaderText(AttributeDependency dependency, StringBuilder sb)
		{
			const char delimiter = ',';
			Assert.True(_delimiters.IndexOf(delimiter) >= 0,
			            "delimiter must be one of: {0}", _delimiters);

			sb.Length = 0; // clear
			sb.Append("# ");
			sb.Append(StringUtils.Concatenate(
				          dependency.SourceAttributes.Select(attribute => attribute.Name),
				          delimiter.ToString()));
			sb.Append(" => ");
			sb.Append(StringUtils.Concatenate(
				          dependency.TargetAttributes.Select(attribute => attribute.Name),
				          delimiter.ToString()));
			sb.Append(" # description");

			return sb.ToString();
		}

		public static void ExportMappingsCsv(
			[NotNull] AttributeDependency dependency, [NotNull] TextWriter writer)
		{
			Assert.ArgumentNotNull(dependency, nameof(dependency));
			Assert.ArgumentNotNull(writer, nameof(writer));

			const char separator = ';';
			using (var csv = new CsvWriter(writer, separator))
			{
				var values = new List<object>();

				// Write row header line:
				values.AddRange(dependency.SourceAttributes.Select(attribute => attribute.Name)
				                          .Cast<object>());
				values.AddRange(dependency.TargetAttributes.Select(attribute => attribute.Name)
				                          .Cast<object>());
				values.Add("description");

				csv.WriteRecord(values);

				foreach (AttributeValueMapping mapping in dependency.AttributeValueMappings)
				{
					values.Clear();

					values.AddRange(mapping.SourceValues);
					values.AddRange(mapping.TargetValues);
					values.Add(mapping.Description);

					csv.WriteRecord(values);
				}
			}
		}

		public static void ImportMappingsTxt([NotNull] AttributeDependency dependency,
		                                     [NotNull] TextReader reader)
		{
			Assert.ArgumentNotNull(dependency, nameof(dependency));
			Assert.ArgumentNotNull(reader, nameof(reader));

			dependency.AttributeValueMappings.Clear();
			// drop all existing mappings (ensure HBM is cascading)

			int sourceCount = dependency.SourceAttributes.Count;
			int targetCount = dependency.TargetAttributes.Count;

			var lineno = 0;
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				lineno += 1;
				line = line.Trim();
				if (line.Length < 1)
				{
					continue; // skip blank line
				}

				if (line[0] == '#')
				{
					continue; // skip comment line
				}

				string sourceText, targetText, description;
				if (SplitLine(line, out sourceText, out targetText, out description))
				{
					var mapping = new AttributeValueMapping(sourceText, targetText,
					                                        description);

					Assert.True(sourceCount == mapping.SourceValues.Count,
					            "Line {0}: expected {1} source values, but got {2}",
					            lineno, sourceCount, mapping.SourceValues.Count);

					Assert.True(targetCount == mapping.TargetValues.Count,
					            "Line {0}: expected {1} target values, but got {2}",
					            lineno, targetCount, mapping.TargetValues.Count);

					dependency.AttributeValueMappings.Add(mapping);
				}
				else
				{
					Assert.Fail("Line {0}: invalid syntax", lineno);
				}
			}
		}

		private static bool SplitLine(string line, out string sourceText,
		                              out string targetText, out string description)
		{
			// Line format: "source => target [# description]"

			sourceText = null;
			targetText = null;
			description = null;

			int mapsToIndex = line.IndexOf("=>", StringComparison.Ordinal);

			if (mapsToIndex < 0)
			{
				return false;
			}

			sourceText = line.Substring(0, mapsToIndex).Trim();

			int commentIndex = line.IndexOf("#", mapsToIndex, StringComparison.Ordinal);

			if (commentIndex < 0)
			{
				targetText = line.Substring(mapsToIndex + 2).Trim();
			}
			else
			{
				int start = mapsToIndex + 2;
				int length = commentIndex - start;
				targetText = line.Substring(start, length).Trim();

				description = line.Substring(commentIndex + 1).Trim();
			}

			return true;
		}

		public static void ImportMappingsCsv(
			[NotNull] AttributeDependency dependency,
			[NotNull] TextReader reader)
		{
			Assert.ArgumentNotNull(dependency, nameof(dependency));
			Assert.ArgumentNotNull(reader, nameof(reader));

			const char delimiter = ',';
			Assert.True(_delimiters.IndexOf(delimiter) >= 0,
			            "delimiter must be one of: {0}", _delimiters);

			dependency.AttributeValueMappings.Clear();
			// drop all existing mappings (ensure HBM is cascading)

			int sourceCount = dependency.SourceAttributes.Count;
			int targetCount = dependency.TargetAttributes.Count;
			int columnCount = sourceCount + targetCount + 1;

			const char fieldSeparator = ';';
			using (var csv = new CsvReader(reader, fieldSeparator))
			{
				csv.SkipBlankLines = true;
				csv.SkipCommentLines = true;

				// Skip first (non-comment) line which declares row headers:
				if (! csv.ReadRecord())
				{
					throw new Exception("Need at least two records (but found none)");
				}

				try
				{
					while (csv.ReadRecord())
					{
						IList<string> values = csv.Values;

						if (values.Count != columnCount)
						{
							throw new FormatException(
								string.Format("Expect {0} fields but found {1}", columnCount,
								              values.Count));
						}

						var sourceValues = new List<object>();
						var targetValues = new List<object>();
						string description = string.Empty;

						for (var i = 0; i < values.Count; i++)
						{
							if (i < sourceCount)
							{
								string value = string.IsNullOrEmpty(values[i]) ? null : values[i];
								FieldType fieldType = dependency.SourceAttributes[i].FieldType;
								object typedValue =
									AttributeDependency.Convert(value, fieldType, null);
								sourceValues.Add(typedValue);
							}
							else if (i < sourceCount + targetCount)
							{
								string value = string.IsNullOrEmpty(values[i]) ? null : values[i];
								FieldType fieldType =
									dependency.TargetAttributes[i - sourceCount].FieldType;
								object typedValue =
									AttributeDependency.Convert(value, fieldType, null);
								targetValues.Add(typedValue);
							}
							else
							{
								description = values[i];
							}
						}

						var sb = new StringBuilder();
						string sourceText = AttributeDependencyUtils.Format(sourceValues, sb);
						string targetText = AttributeDependencyUtils.Format(targetValues, sb);

						var mapping = new AttributeValueMapping(sourceText, targetText,
						                                        description);

						dependency.AttributeValueMappings.Add(mapping);
					}
				}
				catch (Exception ex)
				{
					throw new FormatException(
						string.Format("{0} (line {1})", ex.Message, csv.LineNumber - 1), ex);
				}
			}
		}

		#endregion
	}
}
