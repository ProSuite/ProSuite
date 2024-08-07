using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.Commons.Xml;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Core.Reports
{
	public class HtmlReportBuilder : ReportBuilderBase
	{
		private readonly List<KeyValuePair<string, string>> _headerItems =
			new List<KeyValuePair<string, string>>();

		private readonly XmlElement _htmlTable;

		private readonly TextWriter _textWriter;
		private readonly string _title;
		private readonly XmlDocument _xmlDocument;

		private const int _indexColumnCount = 3;
		private const string _noCategory = "No Category";

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlReportBuilder"/> class.
		/// </summary>
		/// <param name="textWriter">The text writer.</param>
		/// <param name="title">The title.</param>
		public HtmlReportBuilder([NotNull] TextWriter textWriter, [NotNull] string title)
		{
			Assert.ArgumentNotNull(textWriter, nameof(textWriter));

			_textWriter = textWriter;
			_title = title;

			var doc = new XmlDocument();

			XmlElement htmlRoot = doc.CreateElement("html");
			htmlRoot.SetAttribute("xmlns", "http://www.w3.org/1999/xhtml");

			doc.AppendChild(htmlRoot);

			XmlElement htmlHead = doc.CreateElement("head");
			htmlRoot.AppendChild(htmlHead);

			XmlElement htmlTitle = doc.CreateElement("title");
			htmlHead.AppendChild(htmlTitle);

			XmlElement htmlMeta = doc.CreateElement("meta");
			htmlMeta.SetAttribute("charset", "UTF-16");
			htmlHead.AppendChild(htmlMeta);

			htmlTitle.AppendChild(doc.CreateTextNode(title));

			XmlElement style = doc.CreateElement("style");
			style.SetAttribute("type", "text/css");
			style.AppendChild(doc.CreateTextNode(GetStyles()));
			htmlHead.AppendChild(style);

			XmlElement htmlBody = doc.CreateElement("body");
			htmlRoot.AppendChild(htmlBody);

			XmlElement htmlTable = doc.CreateElement("table");
			htmlBody.AppendChild(htmlTable);

			_xmlDocument = doc;
			_htmlTable = htmlTable;
		}

		#endregion

		#region IReportBuilder Members

		public override void AddHeaderItem(string name, string value)
		{
			_headerItems.Add(new KeyValuePair<string, string>(name, value));
		}

		public bool ExcludeHeadersAndIndex { get; set; }

		public override void WriteReport()
		{
			IncludedTestFactories.Sort();

			List<IncludedInstanceBase> includedTests =
				GetSortedTestClasses().Cast<IncludedInstanceBase>().ToList();

			List<IncludedInstanceBase> includedTransformers =
				GetSortedTransformerClasses().Cast<IncludedInstanceBase>().ToList();

			List<IncludedInstanceBase> includedIssueFilters =
				GetSortedIssueFilterClasses().Cast<IncludedInstanceBase>().ToList();

			includedTests.AddRange(IncludedTestFactories.Cast<IncludedInstanceBase>());

			if (! ExcludeHeadersAndIndex)
			{
				WriteHeader();

				WriteSubSectionHeader("Tests");
				WriteTestsIndex();

				WriteSubSectionHeader("Dataset Transformers");
				WriteTransformersIndex();

				WriteSubSectionHeader("Issue Filters");
				WriteIssueFiltersIndex();

				AppendSeparator();
			}

			AppendBody(includedTests, includedTransformers, includedIssueFilters);

			if (! ExcludeHeadersAndIndex)
			{
				AppendIndexTitle("Index");
				WriteAlphabeticalIndex();
			}

			XmlUtils.WriteFormatted(ReplaceBreaks(_xmlDocument.OuterXml), _textWriter);
		}

		private void AppendBody(List<IncludedInstanceBase> includedTests,
		                        List<IncludedInstanceBase> includedTransformers,
		                        List<IncludedInstanceBase> includedIssueFilters)
		{
			// Tests, TestFactories:
			foreach (IncludedInstanceBase includedTest in includedTests)
			{
				if (includedTest is IncludedInstanceClass includedTestClass)
				{
					if (includedTestClass.InstanceConstructors.Count <= 0)
					{
						continue;
					}

					AppendClassTitle(includedTestClass);

					AppendTestClassDescription(includedTestClass);

					foreach (
						IncludedInstanceConstructor includedTestConstructor in
						includedTestClass.InstanceConstructors)
					{
						AppendConstructorTitle(includedTestConstructor);

						AppendTestParameters(includedTestConstructor);
					}

					AppendSeparator();
				}
				else if (includedTest is IncludedTestFactory testFactory)
				{
					AppendTestFactoryTitle(testFactory);

					AppendTestClassDescription(testFactory);

					AppendTestParameters(testFactory);

					AppendSeparator();
				}
			}

			const string transformerImplementation = "Transformer class";
			AppendInstanceDocumentations(includedTransformers, transformerImplementation);

			const string issueFilterImplemantation = "Issue Filter class";
			AppendInstanceDocumentations(includedIssueFilters, issueFilterImplemantation);
		}

		private void AppendInstanceDocumentations(
			[NotNull] List<IncludedInstanceBase> includedIssueFilters,
			[NotNull] string implementationPattern)
		{
			foreach (IncludedInstanceBase filterInstance in includedIssueFilters)
			{
				AppendInstanceDocumentation(filterInstance, implementationPattern);
			}
		}

		private void AppendInstanceDocumentation(IncludedInstanceBase instance,
		                                         string implementationPattern)
		{
			var includedInstance = (IncludedInstanceClass) instance;

			if (includedInstance.InstanceConstructors.Count <= 0)
			{
				return;
			}

			AppendClassTitle(includedInstance);

			AppendClassDescription(includedInstance, implementationPattern);

			foreach (
				IncludedInstanceConstructor includedTestConstructor in
				includedInstance.InstanceConstructors)
			{
				AppendConstructorTitle(includedTestConstructor);

				AppendTestParameters(includedTestConstructor);
			}

			AppendSeparator();
		}

		#endregion

		#region Non-public

		private static string GetStyles()
		{
			var sb = new StringBuilder();

			sb.Append(" body { font-family: Verdana, Arial; } ");

			sb.Append(" table { border-collapse: collapse; }");

			sb.Append(
				" td { vertical-align: top; border: 1px solid black; padding: 5px; font-size: 0.7em; } ");

			sb.Append(" td.indexCol { border-style: none; } ");

			sb.Append(
				" td.reportTitle { font-size: 1.2em; font-weight: bold; vertical-align: middle; height: 40px; background-color:SlateBlue; color:white; }");

			sb.Append(
				" td.indexTitle { font-size: 1em; font-weight: bold; vertical-align: middle; height: 30px; background-color:SlateBlue; color:white; }");

			sb.Append(
				" td.title { font-size: 1em; font-weight: bold; vertical-align: middle; height: 35px; background-color:PowderBlue; } ");

			sb.Append(
				" td.obsoleteTitle { font-size: 1em; font-weight: bold; vertical-align: middle; height: 35px; background-color:PowderBlue; color:Red; text-decoration: line-through; } ");

			sb.Append(
				" td.constructorTitle { font-weight: bold; vertical-align: middle; height: 25px; background-color:PowderBlue; } ");

			sb.Append(
				" td.obsoleteConstructorTitle { font-weight: bold; vertical-align: middle; height: 25px; background-color:PowderBlue; color:Red; text-decoration: line-through; } ");

			sb.Append(" a.obsoleteIndex { color:Red; text-decoration: line-through; } ");

			sb.Append(" td.header {font-weight: bold; background-color:Gainsboro; } ");

			sb.Append(" td.code { font-family: Courier; } ");

			sb.Append(
				" td.sectionTitle { font-size: 1.1em; font-weight: bold; vertical-align: middle; height: 35px; background-color:SlateBlue; color:white; }");

			sb.Append(
				" td.subSectionTitle { font-size: 1.1em; font-weight: bold; vertical-align: middle; height: 32px; background-color:rebeccapurple; color:white; }");

			sb.Append(
				" .indexSectionTitle {font-weight: bold; font-size: 1.05em; margin-top:10px; margin-bottom:4px; } ");

			sb.Append(" .indexTable {table-layout: fixed; } ");

			sb.Append(
				"td.separator { border-left-style: none; border-right-style: none; height: 20px; }");

			sb.Append(" table.inner { border-collapse: collapse; border-style: hidden;}");

			return sb.ToString();
		}

		private void WriteHeader()
		{
			XmlElement titleRow = CreateTableRow();
			titleRow.AppendChild(CreateTableCell(_title, 3, "reportTitle"));
			_htmlTable.AppendChild(titleRow);

			foreach (KeyValuePair<string, string> pair in _headerItems)
			{
				AddHeaderRow(pair.Key, pair.Value);
			}

			AddHeaderRow("Test Classes",
			             IncludedTestClasses.Count.ToString(CultureInfo.InvariantCulture));
			AddHeaderRow("Test Class Constructors",
			             IncludedTestClasses.Values
			                                .Sum(included => included.InstanceConstructors.Count)
			                                .ToString(CultureInfo.InvariantCulture));
			AddHeaderRow("Test Factories",
			             IncludedTestFactories.Count.ToString(CultureInfo.InvariantCulture));
			AddHeaderRow("Transformer Classes",
			             IncludedTransformerClasses.Count.ToString(CultureInfo.InvariantCulture));
			AddHeaderRow("Issue Filter Classes",
			             IncludedFilterClasses.Count.ToString(CultureInfo.InvariantCulture));
			AddHeaderRow("Report Created", DateTime.Now.ToString(CultureInfo.InvariantCulture));
		}

		private void WriteSubSectionHeader(string sectionTitle)
		{
			XmlElement sectionRow = CreateTableRow();
			sectionRow.AppendChild(CreateTableCell(sectionTitle, 3, "subSectionTitle"));
			_htmlTable.AppendChild(sectionRow);
		}

		private void WriteIssueFiltersIndex()
		{
			var indexEntries = new List<IndexEntry>();

			foreach (IncludedInstanceClass filter in GetSortedIssueFilterClasses())
			{
				indexEntries.Add(new InstanceIndexEntry(filter));
			}

			if (indexEntries.Count == 0)
			{
				return;
			}

			RenderIndexEntries(indexEntries);
		}

		private void WriteTransformersIndex()
		{
			var categories = new Dictionary<string, List<IncludedInstanceBase>>();

			ExtractCategories(categories, IncludedTransformerClasses.Values);

			var indexEntries = new List<IndexEntry>();

			foreach (string category in GetSortedCategories(categories.Keys))
			{
				List<IncludedInstanceBase> instances = categories[category];

				instances.Sort();

				if (instances.Count <= 0)
				{
					continue;
				}

				indexEntries.Add(new SectionTitleIndexEntry(string.Format("{0}:", category)));

				foreach (IncludedInstanceBase test in instances)
				{
					indexEntries.Add(new InstanceIndexEntry(test));
				}
			}

			if (indexEntries.Count == 0)
			{
				return;
			}

			RenderIndexEntries(indexEntries);
		}

		private void WriteTestsIndex()
		{
			var categories = new Dictionary<string, List<IncludedInstanceBase>>();

			ExtractCategories(categories, IncludedTestClasses.Values);
			ExtractCategories(categories, IncludedTestFactories.ToArray());

			var indexEntries = new List<IndexEntry>();

			foreach (string category in GetSortedCategories(categories.Keys))
			{
				List<IncludedInstanceBase> categoryTests = categories[category];

				categoryTests.Sort();

				if (categoryTests.Count <= 0)
				{
					continue;
				}

				indexEntries.Add(new SectionTitleIndexEntry(string.Format("{0}:", category)));

				foreach (IncludedInstanceBase test in categoryTests)
				{
					indexEntries.Add(new InstanceIndexEntry(test));
				}
			}

			if (indexEntries.Count == 0)
			{
				return;
			}

			RenderIndexEntries(indexEntries);
		}

		private void WriteAlphabeticalIndex()
		{
			var indexEntries = new List<IndexEntry>();
			if (IncludedTestClasses.Count > 0)
			{
				indexEntries.Add(new SectionTitleIndexEntry("Tests:"));

				foreach (IncludedInstanceClass test in GetSortedTestClasses())
				{
					indexEntries.Add(new InstanceIndexEntry(test));
				}
			}

			if (IncludedTestFactories.Count > 0)
			{
				indexEntries.Add(new SectionTitleIndexEntry("Test Factories:"));

				foreach (IncludedTestFactory factory in IncludedTestFactories)
				{
					indexEntries.Add(new InstanceIndexEntry(factory));
				}
			}

			if (IncludedTransformerClasses.Count > 0)
			{
				indexEntries.Add(new SectionTitleIndexEntry("Transformers:"));
				foreach (IncludedInstanceClass transformer in GetSortedTransformerClasses())
				{
					indexEntries.Add(new InstanceIndexEntry(transformer));
				}
			}

			if (IncludedFilterClasses.Count > 0)
			{
				indexEntries.Add(new SectionTitleIndexEntry("Issue filters:"));
				foreach (IncludedInstanceClass issueFilter in GetSortedIssueFilterClasses())
				{
					indexEntries.Add(new InstanceIndexEntry(issueFilter));
				}
			}

			if (indexEntries.Count == 0)
			{
				return;
			}

			RenderIndexEntries(indexEntries);
		}

		private void RenderIndexEntries([NotNull] IList<IndexEntry> indexEntries)
		{
			XmlElement indexRow = CreateTableRow();

			int columnCount = Math.Min(_indexColumnCount, indexEntries.Count);

			var startIndex = 0;
			for (var i = 0; i < columnCount; i++)
			{
				int remainingCount = indexEntries.Count - startIndex;
				int remainingPerColumn = remainingCount / (columnCount - i);
				int nextColStartIndex = startIndex + remainingPerColumn;

				if (indexEntries[nextColStartIndex - 1] is SectionTitleIndexEntry)
				{
					nextColStartIndex--;
				}

				XmlElement column = CreateTableCell();
				column.SetAttribute("class", "indexCol");
				AddIndexEntries(indexEntries, column, startIndex, nextColStartIndex - 1);
				indexRow.AppendChild(column);

				startIndex = nextColStartIndex;
			}

			XmlElement indexTable = _xmlDocument.CreateElement("table");
			indexTable.SetAttribute("width", "100%");
			indexTable.SetAttribute("class", "indexTable");

			indexTable.AppendChild(indexRow);

			XmlElement parentRow = CreateTableRow();
			XmlElement parentCell = CreateTableCell(3);

			parentCell.AppendChild(indexTable);
			parentRow.AppendChild(parentCell);

			_htmlTable.AppendChild(parentRow);
		}

		private void AddIndexEntries([NotNull] IList<IndexEntry> indexEntries,
		                             [NotNull] XmlElement column,
		                             int startIndex,
		                             int endIndex)
		{
			for (int i = startIndex; i <= endIndex; i++)
			{
				indexEntries[i].Render(_xmlDocument, column);
			}
		}

		private static void ExtractCategories<T>(
			[NotNull] IDictionary<string, List<IncludedInstanceBase>> categories,
			[NotNull] IEnumerable<T> includedTests) where T : IncludedInstanceBase
		{
			foreach (T test in includedTests)
			{
				if (test.Categories.Count <= 0)
				{
					AddTestToCategory(
						test, test.Title.ToLower().StartsWith("tr") ? "Transformers" : _noCategory,
						categories);
				}

				else
				{
					foreach (string category in test.Categories)
					{
						AddTestToCategory(test, category, categories);
					}
				}
			}
		}

		private static void AddTestToCategory(
			[NotNull] IncludedInstanceBase test,
			[NotNull] string category,
			[NotNull] IDictionary<string, List<IncludedInstanceBase>> categories)
		{
			List<IncludedInstanceBase> tests;
			if (! categories.TryGetValue(category, out tests))
			{
				tests = new List<IncludedInstanceBase>();
				categories.Add(category, tests);
			}

			tests.Add(test);
		}

		[NotNull]
		private static IEnumerable<string> GetSortedCategories(
			[NotNull] IEnumerable<string> categories)
		{
			var result = new List<string>();

			var hasNoCategoryCategory = false;

			foreach (string category in categories)
			{
				if (! string.Equals(category, _noCategory))
				{
					result.Add(category);
				}
				else
				{
					hasNoCategoryCategory = true;
				}
			}

			result.Sort();

			if (hasNoCategoryCategory)
			{
				result.Add(_noCategory);
			}

			return result;
		}

		[NotNull]
		private IEnumerable<XmlElement> GetTestParameterRows(
			[NotNull] IncludedInstance includedInstance)
		{
			IInstanceInfo instanceInfo = includedInstance.InstanceInfo;

			var rows = new List<XmlElement>();

			if (includedInstance is IncludedInstanceConstructor)
			{
				XmlElement signatureRow = GetSignatureRow(instanceInfo);
				rows.Add(signatureRow);
				rows.Add(GetTestDescriptionTextRow(includedInstance));
			}

			// create Html-Row for Parameter Description title 
			XmlElement parameterHeadingRow = CreateTableRow();
			parameterHeadingRow.AppendChild(CreateTableCell("Parameter", "header"));
			parameterHeadingRow.AppendChild(CreateTableCell("Type", "header"));
			parameterHeadingRow.AppendChild(CreateTableCell("Description", "header"));
			rows.Add(parameterHeadingRow);

			foreach (TestParameter testParameter in instanceInfo.Parameters)
			{
				string parameterDescription =
					includedInstance.InstanceInfo.GetParameterDescription(testParameter.Name);

				XmlElement parameterRow = CreateTableRow();
				rows.Add(parameterRow);

				parameterRow.AppendChild(
					CreateTableCell(InstanceUtils.GetParameterNameString(testParameter)));
				parameterRow.AppendChild(
					CreateTableCell(InstanceUtils.GetParameterTypeString(testParameter)));
				parameterRow.AppendChild(
					CreateTableCell(parameterDescription ?? "<no description>"));
			}

			return rows;
		}

		private XmlElement GetSignatureRow([NotNull] IInstanceInfo instanceInfo)
		{
			string signature = InstanceUtils.GetTestSignature(instanceInfo);

			XmlElement signatureRow = CreateTableRow();
			signatureRow.AppendChild(CreateTableCell("Signature:"));
			signatureRow.AppendChild(CreateTableCell(signature, 2, "code"));
			return signatureRow;
		}

		private void AppendTestDescriptionText(IncludedInstanceBase test)
		{
			_htmlTable.AppendChild(GetTestDescriptionTextRow(test));
		}

		private XmlElement GetTestDescriptionTextRow(IncludedInstanceBase test)
		{
			string testDescription = test.Description;
			XmlElement descriptionRow = CreateTableRow();
			descriptionRow.AppendChild(CreateTableCell("Description:"));
			descriptionRow.AppendChild(CreateTableCell(testDescription ?? "<no description>", 2));
			return descriptionRow;
		}

		[NotNull]
		private XmlElement CreateTableRow()
		{
			return CreateTableRow(null);
		}

		[NotNull]
		private XmlElement CreateTableRow([CanBeNull] string cssClass)
		{
			XmlElement element = _xmlDocument.CreateElement("tr");

			if (! string.IsNullOrEmpty(cssClass))
			{
				element.SetAttribute("class", cssClass);
			}

			return element;
		}

		[NotNull]
		private XmlElement CreateTableCell([NotNull] string text)
		{
			return CreateTableCell(text, 1, null);
		}

		[NotNull]
		private XmlElement CreateTableCell([NotNull] string text, string cssClass)
		{
			return CreateTableCell(text, 1, cssClass);
		}

		[NotNull]
		private XmlElement CreateTableCell([NotNull] string text, int columnSpan)
		{
			return CreateTableCell(text, columnSpan, null);
		}

		[NotNull]
		private XmlElement CreateTableCell()
		{
			return CreateTableCell(1);
		}

		[NotNull]
		private XmlElement CreateTableCell(int columnSpan)
		{
			return CreateTableCell(columnSpan, string.Empty);
		}

		[NotNull]
		private XmlElement CreateTableCell(int columnSpan, string cssClass)
		{
			return CreateTableCell(string.Empty, columnSpan, cssClass);
		}

		[NotNull]
		private XmlElement CreateTableCell([NotNull] string text, int columnSpan,
		                                   string cssClass)
		{
			XmlElement cell = _xmlDocument.CreateElement("td");
			cell.AppendChild(CreateTextNode(text));

			if (columnSpan != 1)
			{
				cell.SetAttribute("colspan", columnSpan.ToString(CultureInfo.InvariantCulture));
			}

			if (! string.IsNullOrEmpty(cssClass))
			{
				cell.SetAttribute("class", cssClass);
			}

			return cell;
		}

		private XmlText CreateTextNode([NotNull] string text)
		{
			return _xmlDocument.CreateTextNode(text);
		}

		private static string ReplaceBreaks([NotNull] string text)
		{
			const string breakTag = "<br/>";
			const string newLine = "\n";
			string envNewLine = Environment.NewLine;

			if (text.IndexOf(envNewLine, StringComparison.Ordinal) >= 0)
			{
				text = text.Replace(envNewLine, breakTag);
			}

			if (text.IndexOf(newLine, StringComparison.Ordinal) >= 0)
			{
				text = text.Replace(newLine, breakTag);
			}

			return text;
		}

		private void AddHeaderRow(string name, string value)
		{
			XmlElement row = CreateTableRow();

			row.AppendChild(CreateTableCell(name));
			row.AppendChild(CreateTableCell(value, 2));

			_htmlTable.AppendChild(row);
		}

		private void AppendIndexTitle([NotNull] string sectionTitle)
		{
			XmlElement row = CreateTableRow();

			row.AppendChild(CreateTableCell(sectionTitle, 3, "indexTitle"));

			_htmlTable.AppendChild(row);
		}

		private void AppendSeparator()
		{
			XmlElement row = CreateTableRow();
			row.AppendChild(CreateTableCell(3, "separator"));
			_htmlTable.AppendChild(row);
		}

		private void AppendTestParameters([NotNull] IncludedInstance test)
		{
			foreach (XmlElement row in GetTestParameterRows(test))
			{
				_htmlTable.AppendChild(row);
			}
		}

		private void AppendTestFactoryTitle([NotNull] IncludedTestFactory test)
		{
			XmlElement row = CreateTableRow();
			XmlElement cell = CreateTableCell(test.Title, 3, test.Obsolete
				                                                 ? "obsoleteTitle"
				                                                 : "title");
			row.AppendChild(cell);
			cell.AppendChild(CreateAnchor(test.Key));

			_htmlTable.AppendChild(row);
		}

		private void AppendClassTitle([NotNull] IncludedInstanceClass instanceClass)
		{
			XmlElement row = CreateTableRow();
			XmlElement cell = CreateTableCell(instanceClass.Title, 3, instanceClass.Obsolete
				                                  ? "obsoleteTitle"
				                                  : "title");
			row.AppendChild(cell);
			cell.AppendChild(CreateAnchor(instanceClass.Key));

			_htmlTable.AppendChild(row);
		}

		private void AppendTestClassDescription(IncludedInstanceBase test)
		{
			string implementationPattern = "";

			if (test is IncludedInstanceClass)
			{
				implementationPattern = "Test class";
			}
			else if (test is IncludedTestFactory)
			{
				implementationPattern = "Test factory";
			}

			AppendClassDescription(test, implementationPattern);

			if (test is IncludedTestFactory)
			{
				_htmlTable.AppendChild(
					GetSignatureRow(((IncludedInstance) test).InstanceInfo));
			}

			AppendTestIssueCodes(test.IssueCodes);
		}

		private void AppendClassDescription([NotNull] IncludedInstanceBase instance,
		                                    [NotNull] string implementationPattern)
		{
			if (instance.Description != null)
			{
				AppendTestDescriptionText(instance);
			}

			XmlElement categoryRow = CreateTableRow();
			categoryRow.AppendChild(CreateTableCell("Categories:"));
			categoryRow.AppendChild(CreateTableCell(
				                        StringUtils.ConcatenateSorted(instance.Categories, ", "),
				                        2));

			_htmlTable.AppendChild(categoryRow);

			if (IncludeAssemblyInfo)
			{
				string assemblyInfo = string.Format(
					"{2} {1} in {0}",
					Path.GetFileName(instance.Assembly.Location),
					instance.InstanceType.FullName,
					implementationPattern);
				XmlElement assemblyRow = CreateTableRow();
				assemblyRow.AppendChild(CreateTableCell("Implementation:"));
				assemblyRow.AppendChild(CreateTableCell(assemblyInfo, 2));
				_htmlTable.AppendChild(assemblyRow);
			}
		}

		private void AppendTestIssueCodes(IEnumerable<IssueCode> issueCodes)
		{
			if (issueCodes == null)
			{
				return;
			}

			IList<IssueCode> codes = issueCodes as IList<IssueCode> ?? issueCodes.ToList();
			if (! codes.Any())
			{
				return;
			}

			var rows = new List<XmlElement>();

			XmlElement table = _xmlDocument.CreateElement("table");
			table.SetAttribute("width", "100%");
			table.SetAttribute("class", "inner");

			XmlElement headingRow = CreateTableRow();
			headingRow.AppendChild(CreateTableCell("Issue Code", "header"));
			headingRow.AppendChild(CreateTableCell("Description", "header"));
			table.AppendChild(headingRow);

			foreach (IssueCode issueCode in codes)
			{
				XmlElement issueCodeRow = CreateTableRow();
				issueCodeRow.AppendChild(CreateTableCell(issueCode.ID));
				XmlElement cell =
					CreateTableCell(issueCode.Description ?? "<Missing issue code description>");
				cell.SetAttribute("width", "100%");
				issueCodeRow.AppendChild(cell);
				rows.Add(issueCodeRow);
			}

			foreach (XmlElement xmlElement in rows)
			{
				table.AppendChild(xmlElement);
			}

			XmlElement parentRow = CreateTableRow();
			XmlElement parentCell = CreateTableCell(3);
			parentCell.SetAttribute("style", "padding: 0px;");

			parentCell.AppendChild(table);
			parentRow.AppendChild(parentCell);

			_htmlTable.AppendChild(parentRow);
		}

		private void AppendConstructorTitle([NotNull] IncludedInstanceConstructor test)
		{
			XmlElement row = CreateTableRow();
			XmlElement cell = CreateTableCell(test.Title, 3, test.Obsolete
				                                                 ? "obsoleteConstructorTitle"
				                                                 : "constructorTitle");
			row.AppendChild(cell);
			cell.AppendChild(CreateAnchor(test.Key));

			_htmlTable.AppendChild(row);
		}

		private XmlElement CreateAnchor([NotNull] string anchorId)
		{
			XmlElement anchor = _xmlDocument.CreateElement("a");

			anchor.SetAttribute("id", anchorId);

			return anchor;
		}

		#endregion
	}
}
