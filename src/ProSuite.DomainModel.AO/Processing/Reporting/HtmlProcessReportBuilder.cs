using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.Processing;

namespace ProSuite.DomainModel.AO.Processing.Reporting
{
	public class HtmlProcessReportBuilder : IProcessReportBuilder
	{
		[NotNull] private readonly string _title;
		[NotNull] private readonly List<KeyValuePair<string, string>> _headerItems;
		[NotNull] private readonly List<IncludedProcessType> _processTypes;

		public HtmlProcessReportBuilder([NotNull] string title)
		{
			Assert.ArgumentNotNullOrEmpty(title, nameof(title));

			_title = title;
			_headerItems = new List<KeyValuePair<string, string>>();
			_processTypes = new List<IncludedProcessType>();
		}

		#region IProcessReportBuilder

		public bool IncludeAssemblyInfo { get; set; }

		public bool IncludeObsolete { get; set; }

		public void AddHeaderItem(string name, string value = null)
		{
			Assert.ArgumentNotNull(name, nameof(name));

			_headerItems.Add(new KeyValuePair<string, string>(name, value));
		}

		public void AddProcessType(Type processType,
		                           string registeredName = null,
		                           string registeredDescription = null)
		{
			string key = string.Format("T{0:000}", 1 + _processTypes.Count);

			var candidate = new IncludedProcessType(processType, key, registeredName,
			                                        registeredDescription);

			if (! IncludeObsolete && candidate.Obsolete)
			{
				return;
			}

			_processTypes.Add(candidate);
		}

		public void WriteReport(Stream stream)
		{
			Assert.ArgumentNotNull(stream, nameof(stream));

			// Add a few more header items:
			AddHeaderItem("Process Types",
			              _processTypes.Count.ToString(CultureInfo.InvariantCulture));
			AddHeaderItem("Report Created", DateTime.Now.ToString(CultureInfo.InvariantCulture));

			// Sort process types by type name:
			_processTypes.Sort((a, b) => string.Compare(a.Name, b.Name,
			                                            StringComparison.OrdinalIgnoreCase));

			var settings = new XmlWriterSettings();
			settings.ConformanceLevel = ConformanceLevel.Document;
			settings.Encoding = Encoding.UTF8;
			// Hint: Use new UTF8Encoding(false) to omit the BOM
			settings.Indent = true;
			settings.OmitXmlDeclaration = true; // we want HTML

			using (XmlWriter writer = XmlWriter.Create(stream, settings))
			{
				// Start off with: <html xmlns="http://www.w3.org/1999/xhtml">
				writer.WriteStartElement("html", "http://www.w3.org/1999/xhtml");

				WriteHead(writer);

				WriteBody(writer);

				writer.WriteEndElement(); // html
			}
		}

		#endregion

		#region Private methods

		private void WriteHead(XmlWriter writer)
		{
			writer.WriteStartElement("head");

			writer.WriteElementString("title", _title);

			writer.WriteStartElement("style");
			writer.WriteAttributeString("type", "text/css");

			writer.WriteValue(
				@"
body { font-family: Verdana, Arial, sans-serif; }
table { border-collapse: collapse; }
td { vertical-align: top; border: 1px solid black; padding: 5px; font-size: 0.7em; }
td.indexCol { border-style: none; }
td.reportTitle { font-size: 1.2em; font-weight: bold; vertical-align: middle; height: 40px; background-color:SlateBlue; color:white; }
td.obsoleteTitle { font-size: 1em; font-weight: bold; vertical-align: middle; height: 35px; background-color:PowderBlue; color:Red; text-decoration: line-through; }
td.obsoleteCell { font-weight: bold; color:Red; }
td.sectionTitle { font-size: 1.1em; font-weight: bold; vertical-align: middle; height: 35px; background-color:SlateBlue; color:white; }
td.entryTitle { font-size: 1em; font-weight: bold; vertical-align: middle; height: 35px; background-color:PowderBlue; }
td.indexTitle { font-size: 1em; font-weight: bold; vertical-align: middle; height: 30px; background-color:SlateBlue; color:white; }
td.headerCell { font-weight: bold; background-color:Gainsboro; }
td.separator { border-left-style: none; border-right-style: none; height: 20px; }
a.obsoleteIndex { color:Red; text-decoration: line-through; }
");

			writer.WriteEndElement(); // style

			writer.WriteEndElement(); // head
		}

		private void WriteBody(XmlWriter writer)
		{
			writer.WriteStartElement("body");

			writer.WriteStartElement("table");

			WriteReportTitleRow(writer, _title);

			foreach (KeyValuePair<string, string> item in _headerItems)
			{
				WriteReportHeaderRow(writer, item.Key, item.Value);
			}

			WriteSeparatorRow(writer);

			WriteSectionTitleRow(writer, "Process Types");

			WriteSeparatorRow(writer);

			foreach (IncludedProcessType processType in _processTypes)
			{
				WriteEntryTitleRow(writer, processType);

				if (processType.Obsolete && ! string.IsNullOrEmpty(processType.ObsoleteMessage))
				{
					WriteEntryItemRow(writer, "Obsolete", processType.ObsoleteMessage, true);
				}

				WriteEntryItemRow(writer, "Description", processType.Description);

				if (! string.IsNullOrEmpty(processType.RegisteredName))
				{
					WriteEntryItemRow(writer, "Registered As", processType.RegisteredName);
				}

				if (! string.IsNullOrEmpty(processType.RegisteredDescription))
				{
					WriteEntryItemRow(writer, "Described As", processType.RegisteredDescription);
				}

				if (IncludeAssemblyInfo)
				{
					string fileName = Path.GetFileName(processType.Assembly.Location);
					string implementation = string.Format("{0} in {1}",
					                                      processType.ProcessType.FullName,
					                                      fileName);
					WriteEntryItemRow(writer, "Implementation", implementation);
				}

				WriteParameterHeaderRow(writer);
				foreach (ProcessTypeParameter parameter in processType.ProcessTypeParameters)
				{
					WriteParameterRow(writer, parameter);
				}

				WriteSeparatorRow(writer);
			}

			WriteIndex(writer);

			writer.WriteEndElement(); // table

			writer.WriteEndElement(); // body
		}

		private void WriteIndex(XmlWriter writer)
		{
			WriteSectionTitleRow(writer, "Index");

			if (_processTypes.Count < 1)
			{
				return;
			}

			var list = new List<IncludedProcessType>(_processTypes);
			list.Sort(delegate(IncludedProcessType a, IncludedProcessType b)
			{
				int order = string.Compare(a.Name, b.Name,
				                           StringComparison.CurrentCultureIgnoreCase);
				return order == 0
					       ? string.CompareOrdinal(a.Key, b.Key)
					       : order;
			});

			// <tr><td colspan=3><b>Process Types:</b><br/> t1, t2, etc.</td></tr>);
			writer.WriteStartElement("tr");
			writer.WriteStartElement("td");
			writer.WriteAttributeString("colspan", "3");

			writer.WriteElementString("b", "Process Types:");
			writer.WriteStartElement("br");
			writer.WriteEndElement(); // br - does this create an empty <br/>?

			string lastTypeName = null;
			for (var i = 0; i < list.Count; i++)
			{
				IncludedProcessType processType = list[i];

				if (i > 0)
				{
					writer.WriteString(", ");
					writer.WriteString(Environment.NewLine);
				}

				if (string.Equals(processType.Name, lastTypeName))
				{
					// (<a href="#key">other instance</a>)
					writer.WriteString("(");
					writer.WriteStartElement("a");
					writer.WriteAttributeString("href", string.Concat("#", processType.Key));
					writer.WriteValue("another instance");
					writer.WriteEndElement(); // a
					writer.WriteString(")");
				}
				else
				{
					// <a href="#key" title="desc">name</a>
					writer.WriteStartElement("a");
					writer.WriteAttributeString("href", string.Concat("#", processType.Key));
					if (! string.IsNullOrEmpty(processType.Description))
					{
						writer.WriteAttributeString("title", processType.Description);
					}

					writer.WriteValue(processType.Name);
					writer.WriteEndElement(); // a
				}

				lastTypeName = processType.Name;
			}

			writer.WriteEndElement(); // td
			writer.WriteEndElement(); // tr

			// Part 2: the "Registered As" index

			list.Sort(delegate(IncludedProcessType a, IncludedProcessType b)
			{
				int order = string.Compare(a.RegisteredName, b.RegisteredName,
				                           StringComparison.CurrentCultureIgnoreCase);
				return order == 0
					       ? string.CompareOrdinal(a.Key, b.Key)
					       : order;
			});

			if (string.IsNullOrEmpty(list[0].RegisteredName))
			{
				return;
			}

			// <tr><td colspan=3><b>Registered As:</b><br/> t1, t2, etc.</td></tr>);
			writer.WriteStartElement("tr");
			writer.WriteStartElement("td");
			writer.WriteAttributeString("colspan", "3");

			writer.WriteElementString("b", "Registered As:");
			writer.WriteStartElement("br");
			writer.WriteEndElement(); // br - does this create an empty <br/>?
			string lastRegisteredName = null;
			for (var i = 0; i < list.Count; i++)
			{
				IncludedProcessType processType = list[i];

				if (i > 0)
				{
					writer.WriteString(", ");
					writer.WriteString(Environment.NewLine);
				}

				if (string.Equals(processType.RegisteredName, lastRegisteredName))
				{
					// (<a href="#key">duplicate</a>)
					writer.WriteString("(");
					writer.WriteStartElement("a");
					writer.WriteAttributeString("href", string.Concat("#", processType.Key));
					writer.WriteValue("duplicate");
					writer.WriteEndElement(); // a
					writer.WriteString(")");
				}
				else
				{
					// <a href="#key" title="reg.desc">reg.name</a>
					writer.WriteStartElement("a");
					writer.WriteAttributeString("href", string.Concat("#", processType.Key));
					if (! string.IsNullOrEmpty(processType.RegisteredDescription))
					{
						writer.WriteAttributeString("title", processType.RegisteredDescription);
					}

					writer.WriteValue(Assert.NotNull(processType.RegisteredName));
					writer.WriteEndElement(); // a
				}

				lastRegisteredName = processType.RegisteredName;
			}

			writer.WriteEndElement(); // td
			writer.WriteEndElement(); // tr
		}

		private static void WriteReportTitleRow(XmlWriter writer, string title)
		{
			writer.WriteStartElement("tr");
			writer.WriteStartElement("td");
			writer.WriteAttributeString("colspan", "3");
			writer.WriteAttributeString("class", "reportTitle");
			writer.WriteValue(title ?? string.Empty);
			writer.WriteEndElement(); // td
			writer.WriteEndElement(); // tr
		}

		private static void WriteReportHeaderRow(XmlWriter writer, string name, string value)
		{
			writer.WriteStartElement("tr");

			writer.WriteStartElement("td");
			writer.WriteValue(name ?? string.Empty);
			writer.WriteEndElement(); // td

			writer.WriteStartElement("td");
			writer.WriteAttributeString("colspan", "2");
			writer.WriteValue(value ?? string.Empty);
			writer.WriteEndElement(); // td

			writer.WriteEndElement(); // tr
		}

		private static void WriteSeparatorRow(XmlWriter writer)
		{
			writer.WriteStartElement("tr");
			writer.WriteStartElement("td");
			writer.WriteAttributeString("colspan", "3");
			writer.WriteAttributeString("class", "separator");
			writer.WriteEndElement(); // td
			writer.WriteEndElement(); // tr
		}

		private static void WriteSectionTitleRow(XmlWriter writer, string title)
		{
			writer.WriteStartElement("tr");
			writer.WriteStartElement("td");
			writer.WriteAttributeString("colspan", "3");
			writer.WriteAttributeString("class", "sectionTitle");
			writer.WriteValue(title ?? string.Empty);
			writer.WriteEndElement(); // td
			writer.WriteEndElement(); // tr
		}

		private static void WriteEntryTitleRow(XmlWriter writer,
		                                       IncludedProcessType processType)
		{
			writer.WriteStartElement("tr");

			writer.WriteStartElement("td");
			writer.WriteAttributeString("colspan", "3");
			writer.WriteAttributeString("class",
			                            processType.Obsolete ? "obsoleteTitle" : "entryTitle");

			writer.WriteValue(processType.Name);

			if (! string.IsNullOrEmpty(processType.Key))
			{
				writer.WriteStartElement("a");
				writer.WriteAttributeString("id", processType.Key);
				writer.WriteEndElement(); // a
			}

			writer.WriteEndElement(); // td

			writer.WriteEndElement(); // tr
		}

		private static void WriteEntryItemRow(XmlWriter writer, string name, string value,
		                                      bool obsoleteStyle = false)
		{
			writer.WriteStartElement("tr");

			writer.WriteStartElement("td");
			if (obsoleteStyle)
			{
				writer.WriteAttributeString("class", "obsoleteCell");
			}

			writer.WriteValue(name ?? string.Empty);
			writer.WriteEndElement(); // td

			writer.WriteStartElement("td");
			writer.WriteAttributeString("colspan", "2");
			if (obsoleteStyle)
			{
				writer.WriteAttributeString("class", "obsoleteCell");
			}

			WriteMultiLineText(writer, value);

			writer.WriteEndElement(); // td

			writer.WriteEndElement(); // tr
		}

		private static void WriteParameterHeaderRow(XmlWriter writer)
		{
			writer.WriteStartElement("tr");

			writer.WriteStartElement("td");
			writer.WriteAttributeString("class", "headerCell");
			writer.WriteValue("Parameter");
			writer.WriteEndElement(); // td

			writer.WriteStartElement("td");
			writer.WriteAttributeString("class", "headerCell");
			writer.WriteValue("Type");
			writer.WriteEndElement(); // td

			writer.WriteStartElement("td");
			writer.WriteAttributeString("class", "headerCell");
			writer.WriteValue("Description");
			writer.WriteEndElement(); // td

			writer.WriteEndElement(); // tr
		}

		private static void WriteParameterRow(XmlWriter writer,
		                                      ProcessTypeParameter processTypeParameter)
		{
			writer.WriteStartElement("tr");
			writer.WriteElementString("td", processTypeParameter.Name);
			writer.WriteElementString("td", processTypeParameter.Type);

			writer.WriteStartElement("td");
			WriteMultiLineText(writer, processTypeParameter.Description);
			writer.WriteEndElement(); // td

			writer.WriteEndElement(); // tr
		}

		private static void WriteMultiLineText(XmlWriter writer, string multiLineText)
		{
			IList<string> lines = StringUtils.SplitAndTrim(multiLineText, "\n");

			var first = true;
			foreach (string line in lines)
			{
				if (! first)
				{
					writer.WriteStartElement("br");
					writer.WriteEndElement(); // br - does this create an empty <br/>?
				}

				first = false;
				writer.WriteValue(line);
			}
		}

		#endregion

		#region Nested type: IncludedProcessType

		private class IncludedProcessType
		{
			#region Properties

			[NotNull] public readonly Type ProcessType;
			[NotNull] public readonly string Key;
			[CanBeNull] public readonly string RegisteredName;
			[CanBeNull] public readonly string RegisteredDescription;
			[NotNull] public readonly string Name;
			[NotNull] public readonly string Description;
			public readonly bool Obsolete;
			[CanBeNull] public readonly string ObsoleteMessage;
			[NotNull] public readonly Assembly Assembly;
			[NotNull] public readonly IList<ProcessTypeParameter> ProcessTypeParameters;

			#endregion

			public IncludedProcessType([NotNull] Type processType, [NotNull] string key,
			                           [CanBeNull] string registeredName,
			                           [CanBeNull] string registeredDescription)
			{
				Assert.ArgumentNotNull(processType, nameof(processType));
				Assert.ArgumentNotNullOrEmpty(key, nameof(key)); // needed for internal links

				ProcessType = processType;
				Key = key;
				RegisteredName = registeredName;
				RegisteredDescription = registeredDescription;

				Name = processType.Name;
				Description = GdbProcessUtils.GetProcessDescription(processType);
				Obsolete = GdbProcessUtils.IsObsolete(processType, out ObsoleteMessage);
				Assembly = processType.Assembly;

				IList<PropertyInfo> props = GdbProcessUtils.GetProcessParameters(processType);
				ProcessTypeParameters = new List<ProcessTypeParameter>(props.Count);

				foreach (PropertyInfo property in props)
				{
					string name = property.Name;
					string type = GdbProcessUtils.GetParameterDisplayType(property);
					string description = GdbProcessUtils.GetParameterDescription(property);

					ProcessTypeParameters.Add(new ProcessTypeParameter(name, type, description));
				}
			}
		}

		#endregion

		#region Nested type: ProcessTypeParameter

		private readonly struct ProcessTypeParameter
		{
			[NotNull] public readonly string Name;
			[NotNull] public readonly string Type;
			[NotNull] public readonly string Description;

			public ProcessTypeParameter([NotNull] string name, [NotNull] string type,
			                            [NotNull] string description)
			{
				Name = name;
				Type = type;
				Description = description;
			}
		}

		#endregion
	}
}
