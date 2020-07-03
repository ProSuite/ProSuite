using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Xml
{
	public static class XmlUtils
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		// see http://stackoverflow.com/questions/397250/unicode-regex-invalid-xml-characters
		private static readonly Regex _invalidXmlChars = new Regex(
			@"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFFFE\uFFFF]",
			RegexOptions.Compiled);

		/// <summary>
		/// Deserializes an object from an xml string, validating the xml string using a defined 
		/// xml schema.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="xml">The XML string.</param>
		/// <param name="xsdSchema">An xml string containing an xsd schema.</param>
		/// <returns></returns>
		[NotNull]
		public static T DeserializeString<T>([NotNull] string xml,
		                                     [NotNull] string xsdSchema)
		{
			Assert.ArgumentNotNullOrEmpty(xml, nameof(xml));
			Assert.ArgumentNotNullOrEmpty(xsdSchema, nameof(xsdSchema));

			using (var reader = new StringReader(xml))
			{
				return Deserialize<T>(reader, LoadSchema(xsdSchema));
			}
		}

		/// <summary>
		/// Deserializes an object from an xml file, validating the xml file using a defined 
		/// xml schema.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="xmlFilePath">The XML file path.</param>
		/// <param name="xsdSchema">An xml string containing an xsd schema.</param>
		/// <returns></returns>
		[NotNull]
		public static T DeserializeFile<T>([NotNull] string xmlFilePath,
		                                   [NotNull] string xsdSchema)
		{
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));
			Assert.ArgumentNotNullOrEmpty(xsdSchema, nameof(xsdSchema));

			return DeserializeFile<T>(xmlFilePath, LoadSchema(xsdSchema));
		}

		/// <summary>
		/// Deserializes an object from an xml file, validating the xml file using a defined xml schema.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="xmlFilePath">The XML file path.</param>
		/// <param name="xmlSchema">The XML schema.</param>
		/// <returns></returns>
		[NotNull]
		public static T DeserializeFile<T>([NotNull] string xmlFilePath,
		                                   [NotNull] XmlSchema xmlSchema)
		{
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));
			Assert.ArgumentNotNull(xmlSchema, nameof(xmlSchema));

			using (var stream = new StreamReader(xmlFilePath))
			{
				return Deserialize<T>(stream, xmlSchema);
			}
		}

		[NotNull]
		public static T Deserialize<T>([NotNull] TextReader textReader,
		                               [NotNull] XmlSchema xmlSchema)
		{
			Assert.ArgumentNotNull(textReader, nameof(textReader));
			Assert.ArgumentNotNull(xmlSchema, nameof(xmlSchema));

			var schemas = new XmlSchemaSet();
			schemas.Add(xmlSchema);

			var settings = new XmlReaderSettings
			               {
				               Schemas = schemas,
				               ValidationType = ValidationType.Schema,
				               ValidationFlags =
					               XmlSchemaValidationFlags.ProcessIdentityConstraints |
					               XmlSchemaValidationFlags.ReportValidationWarnings |
					               XmlSchemaValidationFlags.AllowXmlAttributes
			               };

			Exception firstException = null;

			settings.ValidationEventHandler +=
				delegate(object sender, ValidationEventArgs args)
				{
					if (args.Severity == XmlSeverityType.Error)
					{
						firstException = args.Exception;
					}
					else
					{
						_msg.WarnFormat(args.Message);
					}
				};

			var serializer = new XmlSerializer(typeof(T));

			T document;
			using (XmlReader reader = XmlReader.Create(textReader, settings))
			{
				document = (T) serializer.Deserialize(reader);
			}

			if (firstException != null)
			{
				throw firstException;
			}

			return document;
		}

		/// <summary>
		/// Serializes an object to a specified XML file path.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="xmlFilePath">The XML file path.</param>
		/// <remarks>NewLine characters are entitized in the exported file and are thus 
		/// restored correctly when deserializing using a normalizing reader.</remarks>
		public static void Serialize<T>([NotNull] T obj, [NotNull] string xmlFilePath)
		{
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));
			Assert.ArgumentNotNull(obj, nameof(obj));

			var serializer = new XmlSerializer(typeof(T));

			using (XmlWriter xmlWriter = XmlWriter.Create(xmlFilePath, GetWriterSettings()))
			{
				serializer.Serialize(xmlWriter, obj);
			}
		}

		/// <summary>
		/// Serializes an object an returns the resulting xml string.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj">The object to serialize.</param>
		/// <remarks>NewLine characters are entitized in the exported file and are thus 
		/// restored correctly when deserializing using a normalizing reader.</remarks>
		[NotNull]
		public static string Serialize<T>([NotNull] T obj)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));

			var serializer = new XmlSerializer(typeof(T));
			var sb = new StringBuilder();

			using (XmlWriter xmlWriter = XmlWriter.Create(sb, GetWriterSettings()))
			{
				serializer.Serialize(xmlWriter, obj);
			}

			return sb.ToString();
		}

		[NotNull]
		public static string Format([NotNull] string xml)
		{
			Assert.ArgumentNotNullOrEmpty(xml, nameof(xml));

			var stringWriter = new StringWriter();

			WriteFormatted(xml, stringWriter);

			return stringWriter.ToString();
		}

		public static void WriteFormatted([NotNull] string xml,
		                                  [NotNull] TextWriter textWriter)
		{
			Assert.ArgumentNotNullOrEmpty(xml, nameof(xml));
			Assert.ArgumentNotNull(textWriter, nameof(textWriter));

			var xmlWriter = new XmlTextWriter(textWriter) {Formatting = Formatting.Indented};

			var document = new XmlDocument();
			document.LoadXml(xml);

			document.WriteContentTo(xmlWriter);
			xmlWriter.Flush();
		}

		/// <summary>
		/// Escapes characters that are not valid for Xml.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns></returns>
		/// <remarks>Invalid characters are escaped as hexadecimal value in brackets, e.g. [0x0B]</remarks>
		[ContractAnnotation("text:notnull => notnull; text:null => null")]
		public static string EscapeInvalidCharacters([CanBeNull] string text)
		{
			return string.IsNullOrEmpty(text)
				       ? text
				       : _invalidXmlChars.Replace(text, EscapeInvalidCharacter);
		}

		[NotNull]
		private static string EscapeInvalidCharacter([NotNull] Match match)
		{
			string value = match.Value;

			if (value.Length == 0)
			{
				return value;
			}

			const string format = @"[0x{0:X02}]";

			if (value.Length == 1)
			{
				return string.Format(format, (int) value[0]);
			}

			var sb = new StringBuilder();

			foreach (char c in value)
			{
				sb.AppendFormat(format, (int) c);
			}

			return sb.ToString();
		}

		private static XmlWriterSettings GetWriterSettings()
		{
			return new XmlWriterSettings
			       {
				       Encoding = Encoding.UTF8,
				       NewLineHandling = NewLineHandling.Entitize,
				       Indent = true
			       };
		}

		[NotNull]
		private static XmlSchema LoadSchema([NotNull] string xsdSchema)
		{
			Assert.ArgumentNotNullOrEmpty(xsdSchema, nameof(xsdSchema));

			using (TextReader reader = new StringReader(xsdSchema))
			{
				using (XmlReader schemaReader = XmlReader.Create(reader))
				{
					return XmlSchema.Read(schemaReader, delegate { });
				}
			}
		}
	}
}
