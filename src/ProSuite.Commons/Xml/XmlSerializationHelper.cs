using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Xml
{
	public class XmlSerializationHelper<T> where T : class
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		// store in field for access by event handlers - only for duration of read
		private string _deserializingXmlFile;
		private Action<string> _receiveNotification;

		// cached XmlSerializer
		private XmlSerializer _serializer;

		public void SaveToFile([NotNull] T obj, [NotNull] string xmlFile)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));
			Assert.ArgumentNotNullOrEmpty(xmlFile, nameof(xmlFile));

			var writerSettings =
				new XmlWriterSettings
				{
					Encoding = Encoding.UTF8,
					NewLineHandling = NewLineHandling.Entitize,
					Indent = true
				};

			XmlSerializer serializer = GetSerializer();

			using (XmlWriter xmlWriter = XmlWriter.Create(xmlFile, writerSettings))
			{
				serializer.Serialize(xmlWriter, obj);
			}
		}

		/// <summary>
		/// Reads T from the provided xml file. Potential issues are logged as warning.
		/// </summary>
		/// <param name="xmlFile"></param>
		/// <returns></returns>
		[NotNull]
		public T ReadFromFile([NotNull] string xmlFile)
		{
			Action<string> logWarn =
				text => _msg.WarnFormat("Reading config file {0}: {1}", xmlFile, text);

			return ReadFromFile(xmlFile, logWarn);
		}

		/// <summary>
		/// Reads T from the provided xml file and sends potential issues to the
		/// specified delegate.
		/// </summary>
		/// <param name="xmlFile"></param>
		/// <param name="receiveNotification">The delegate to receive potential
		/// notifications. If null, potential issues will be logged on debug level.</param>
		/// <returns></returns>
		[NotNull]
		public T ReadFromFile([NotNull] string xmlFile,
		                      [CanBeNull] Action<string> receiveNotification)
		{
			Assert.ArgumentNotNullOrEmpty(xmlFile, nameof(xmlFile));
			Assert.ArgumentCondition(File.Exists(xmlFile), "file does not exist: {0}", xmlFile);

			XmlSerializer serializer = GetSerializer();

			FileStream stream = null;

			try
			{
				// make accessible for event handlers
				_deserializingXmlFile = xmlFile;
				_receiveNotification = receiveNotification;

				WireEvents(serializer);

				stream = new FileStream(xmlFile, FileMode.Open, FileAccess.Read);

				object result = serializer.Deserialize(stream);

				return (T) result;
			}
			finally
			{
				UnwireEvents(serializer);
				_deserializingXmlFile = null;
				_receiveNotification = null;

				if (stream != null)
				{
					stream.Close();
				}
			}
		}

		[NotNull]
		public T ReadFromString([NotNull] string xmlContent,
		                      [CanBeNull] Action<string> receiveNotification)
		{
			Assert.ArgumentNotNullOrEmpty(xmlContent, nameof(xmlContent));

			XmlSerializer serializer = GetSerializer();

			try
			{
				// make accessible for event handlers
				_receiveNotification = receiveNotification;

				WireEvents(serializer);

				using (var stream = new StringReader(xmlContent))
				{
					object result = serializer.Deserialize(stream);
					return (T) result;
				}
			}
			finally
			{
				UnwireEvents(serializer);
				_receiveNotification = null;
			}
		}

		public bool CanDeserializeString([NotNull] string xmlContent)
		{
			Assert.ArgumentNotNullOrEmpty(xmlContent, nameof(xmlContent));

			if (xmlContent.TrimStart().StartsWith("<") != true)
			{
				return false;
			}

			try
			{
				using (var reader = XmlReader.Create(new StringReader(xmlContent)))
				{
					return CanDeserialize(reader);
				}
			}
			catch
			{
				return false;
			}
		}

		public bool CanDeserialize([NotNull] XmlReader reader)
		{
			Assert.ArgumentNotNull(reader, nameof(reader));

			XmlSerializer serializer = GetSerializer();

			return serializer.CanDeserialize(reader);
		}


		[NotNull]
		private XmlSerializer GetSerializer()
		{
			return _serializer ?? (_serializer = new XmlSerializer(typeof(T)));
		}

		private void UnwireEvents(XmlSerializer serializer)
		{
			serializer.UnknownNode -= serializer_UnknownNode;
			serializer.UnknownAttribute -= serializer_UnknownAttribute;
			serializer.UnknownElement -= serializer_UnknownElement;
		}

		private void WireEvents(XmlSerializer serializer)
		{
			serializer.UnknownNode += serializer_UnknownNode;
			serializer.UnknownAttribute += serializer_UnknownAttribute;
			serializer.UnknownElement += serializer_UnknownElement;
		}

		#region Event handlers

		private void serializer_UnknownElement(object sender, XmlElementEventArgs e)
		{
			if (_receiveNotification != null)
			{
				_receiveNotification(string.Format("Ignored unknown element {0}", e.Element.Name));
			}
			else
			{
				_msg.DebugFormat("Ignored unknown element in config file {0}: {1}",
				                 _deserializingXmlFile, e.Element.Name);
			}
		}

		private void serializer_UnknownAttribute(object sender, XmlAttributeEventArgs e)
		{
			if (_receiveNotification != null)
			{
				_receiveNotification(string.Format("Ignored unknown attribute {0}", e.Attr.Name));
			}
			else
			{
				_msg.DebugFormat("Ignored unknown attribute in config file {0}: {1}",
				                 _deserializingXmlFile, e.Attr.Name);
			}
		}

		private void serializer_UnknownNode(object sender, XmlNodeEventArgs e)
		{
			if (_receiveNotification != null)
			{
				_receiveNotification(string.Format("Ignored unknown node {0}", e.Name));
			}
			else
			{
				_msg.DebugFormat("Ignored unknown node in config file {0}: {1}",
				                 _deserializingXmlFile, e.Name);
			}
		}

		#endregion
	}
}
