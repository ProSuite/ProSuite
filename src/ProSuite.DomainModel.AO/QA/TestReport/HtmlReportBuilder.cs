using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	public class HtmlReportBuilder : IReportBuilder
	{
		private readonly List<KeyValuePair<string, string>> _headerItems =
			new List<KeyValuePair<string, string>>();

		private readonly XmlElement _htmlTable;

		private readonly IDictionary<Type, IncludedTestClass> _includedTestClasses =
			new Dictionary<Type, IncludedTestClass>();

		private readonly List<IncludedTestFactory> _includedTestFactories =
			new List<IncludedTestFactory>();

		private readonly TextWriter _textWriter;
		private readonly string _title;
		private readonly XmlDocument _xmlDocument;

		private bool _includeAssemblyInfo;
		private bool _includeObsolete;

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

		public bool IncludeAssemblyInfo
		{
			get { return _includeAssemblyInfo; }
			set { _includeAssemblyInfo = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether obsolete tests or factories are
		/// included.
		/// </summary>
		/// <value><c>true</c> if obsolete tests or factories should be included; otherwise, <c>false</c>.</value>
		public bool IncludeObsolete
		{
			get { return _includeObsolete; }
			set { _includeObsolete = value; }
		}

		public void AddHeaderItem(string name, string value)
		{
			_headerItems.Add(new KeyValuePair<string, string>(name, value));
		}

		public void IncludeTestFactory(Type testFactoryType)
		{
			var testFactory = new IncludedTestFactory(testFactoryType);

			if (! _includeObsolete && testFactory.Obsolete)
			{
				return;
			}

			if (testFactory.InternallyUsed)
			{
				return;
			}

			_includedTestFactories.Add(testFactory);
		}

		public void IncludeTest(Type testType, int constructorIndex)
		{
			var newTestClass = false;
			IncludedTestClass testClass;
			if (! _includedTestClasses.TryGetValue(testType, out testClass))
			{
				testClass = new IncludedTestClass(testType);

				if (! _includeObsolete && testClass.Obsolete)
				{
					return;
				}

				if (testClass.InternallyUsed)
				{
					return;
				}

				// this test class is to be added, if the constructor is not obsolete
				newTestClass = true;
			}

			if (testClass.Obsolete)
			{
				return;
			}

			IncludedTestConstructor testConstructor =
				testClass.CreateTestConstructor(constructorIndex);

			if (! _includeObsolete && testConstructor.Obsolete)
			{
				return;
			}

			if (testConstructor.InternallyUsed)
			{
				return;
			}

			testClass.IncludeConstructor(testConstructor);

			if (newTestClass)
			{
				_includedTestClasses.Add(testType, testClass);
			}
		}

		public void WriteReport()
		{
			_includedTestFactories.Sort();

			List<IncludedTestBase> includedTests =
				GetSortedTestClasses().Cast<IncludedTestBase>().ToList();

			includedTests.AddRange(_includedTestFactories.Cast<IncludedTestBase>());

			WriteHeader();

			WriteCategoryIndex();

			AppendSeparator();

			if (includedTests.Count > 0)

			{
				foreach (IncludedTestBase includedTest in includedTests)
				{
					if (includedTest is IncludedTestClass)
					{
						var includedTestClass = (IncludedTestClass) includedTest;
						if (includedTestClass.TestConstructors.Count <= 0)
						{
							continue;
						}

						AppendTestClassTitle(includedTestClass);

						AppendTestClassDescription(includedTestClass);

						foreach (
							IncludedTestConstructor includedTestConstructor in
							includedTestClass.TestConstructors)
						{
							AppendTestConstructorTitle(includedTestConstructor);

							AppendTestParameters(includedTestConstructor);
						}

						AppendSeparator();
					}
					else if (includedTest is IncludedTestFactory)
					{
						var test = (IncludedTestFactory) includedTest;

						AppendTestFactoryTitle(test);

						AppendTestClassDescription(test);

						AppendTestParameters(test);

						AppendSeparator();
					}
				}

				AppendIndexTitle("Index");
				WriteAlphabeticalIndex();
			}

			XmlUtils.WriteFormatted(ReplaceBreaks(_xmlDocument.OuterXml), _textWriter);
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
				" .indexSectionTitle {font-weight: bold; font-size: 1.05em; margin-top:10px; margin-bottom:4px; } ");

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
			             _includedTestClasses.Count.ToString(CultureInfo.InvariantCulture));
			AddHeaderRow("Test Class Constructors",
			             GetTestConstructorCount(_includedTestClasses.Values)
				             .ToString(CultureInfo.InvariantCulture));
			AddHeaderRow("Test Factories",
			             _includedTestFactories.Count.ToString(CultureInfo.InvariantCulture));
			AddHeaderRow("Report Created", DateTime.Now.ToString(CultureInfo.InvariantCulture));
		}

		private void WriteCategoryIndex()
		{
			var categories = new Dictionary<string, List<IncludedTestBase>>();

			ExtractCategories(categories, _includedTestClasses.Values);
			ExtractCategories(categories, _includedTestFactories.ToArray());

			var indexEntries = new List<IndexEntry>();

			foreach (string category in GetSortedCategories(categories.Keys))
			{
				List<IncludedTestBase> categoryTests = categories[category];

				categoryTests.Sort();

				if (categoryTests.Count <= 0)
				{
					continue;
				}

				indexEntries.Add(new SectionTitleIndexEntry(string.Format("{0}:", category)));

				foreach (IncludedTestBase test in categoryTests)
				{
					indexEntries.Add(new TestIndexEntry(test));
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
			if (_includedTestClasses.Count > 0)
			{
				indexEntries.Add(new SectionTitleIndexEntry("Tests:"));

				foreach (IncludedTestClass test in GetSortedTestClasses())
				{
					indexEntries.Add(new TestClassIndexEntry(test));
				}
			}

			if (_includedTestFactories.Count > 0)
			{
				indexEntries.Add(new SectionTitleIndexEntry("Test Factories:"));
				foreach (IncludedTestFactory factory in _includedTestFactories)
				{
					indexEntries.Add(new TestFactoryIndexEntry(factory));
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
			[NotNull] IDictionary<string, List<IncludedTestBase>> categories,
			[NotNull] IEnumerable<T> includedTests) where T : IncludedTestBase
		{
			foreach (T test in includedTests)
			{
				if (test.Categories.Count <= 0)
				{
					AddTestToCategory(test, _noCategory, categories);
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
			[NotNull] IncludedTestBase test,
			[NotNull] string category,
			[NotNull] IDictionary<string, List<IncludedTestBase>> categories)
		{
			List<IncludedTestBase> tests;
			if (! categories.TryGetValue(category, out tests))
			{
				tests = new List<IncludedTestBase>();
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

		private static int GetTestConstructorCount(
			[NotNull] IEnumerable<IncludedTestClass> includedTestClasses)
		{
			var result = 0;

			foreach (IncludedTestClass includedTestClass in includedTestClasses)
			{
				result += includedTestClass.TestConstructors.Count;
			}

			return result;
		}

		[NotNull]
		private IEnumerable<IncludedTestClass> GetSortedTestClasses()
		{
			var result = new List<IncludedTestClass>(_includedTestClasses.Values);

			result.Sort();

			return result;
		}

		[NotNull]
		private IEnumerable<XmlElement> GetTestParameterRows([NotNull] IncludedTest test)
		{
			TestFactory testFactory = test.TestFactory;

			var rows = new List<XmlElement>();

			if (test is IncludedTestConstructor)
			{
				XmlElement signatureRow = GetSignatureRow(testFactory);
				rows.Add(signatureRow);
				rows.Add(GetTestDescriptionTextRow(test));
			}

			// create Html-Row for Parameter Description title 
			XmlElement parameterHeadingRow = CreateTableRow();
			parameterHeadingRow.AppendChild(CreateTableCell("Parameter", "header"));
			parameterHeadingRow.AppendChild(CreateTableCell("Type", "header"));
			parameterHeadingRow.AppendChild(CreateTableCell("Description", "header"));
			rows.Add(parameterHeadingRow);

			foreach (TestParameter testParameter in testFactory.Parameters)
			{
				string parameterDescription =
					test.TestFactory.GetParameterDescription(testParameter.Name);

				XmlElement parameterRow = CreateTableRow();
				rows.Add(parameterRow);

				parameterRow.AppendChild(CreateTableCell(testParameter.Name));
				parameterRow.AppendChild(
					CreateTableCell(TestImplementationUtils.GetParameterTypeString(testParameter)));
				parameterRow.AppendChild(
					CreateTableCell(parameterDescription ?? "<no description>"));
			}

			return rows;
		}

		private XmlElement GetSignatureRow([NotNull] TestFactory testFactory)
		{
			string signature = TestImplementationUtils.GetTestSignature(testFactory);

			XmlElement signatureRow = CreateTableRow();
			signatureRow.AppendChild(CreateTableCell("Signature:"));
			signatureRow.AppendChild(CreateTableCell(signature, 2, "code"));
			return signatureRow;
		}

		private void AppendTestDescriptionText(IncludedTestBase test)
		{
			_htmlTable.AppendChild(GetTestDescriptionTextRow(test));
		}

		private XmlElement GetTestDescriptionTextRow(IncludedTestBase test)
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

		private void AppendTestParameters([NotNull] IncludedTest test)
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

		private void AppendTestClassTitle([NotNull] IncludedTestClass test)
		{
			XmlElement row = CreateTableRow();
			XmlElement cell = CreateTableCell(test.Title, 3, test.Obsolete
				                                                 ? "obsoleteTitle"
				                                                 : "title");
			row.AppendChild(cell);
			cell.AppendChild(CreateAnchor(test.Key));

			_htmlTable.AppendChild(row);
		}

		private void AppendTestClassDescription(IncludedTestBase test)
		{
			if (test.Description != null)
			{
				AppendTestDescriptionText(test);
			}

			XmlElement categoryRow = CreateTableRow();
			categoryRow.AppendChild(CreateTableCell("Categories:"));
			categoryRow.AppendChild(CreateTableCell(test.GetCommaSeparatedCategories(), 2));

			_htmlTable.AppendChild(categoryRow);

			if (_includeAssemblyInfo)
			{
				var implementationPattern = "";

				if (test is IncludedTestClass)
				{
					implementationPattern = "Test class";
				}
				else if (test is IncludedTestFactory)
				{
					implementationPattern = "Test factory";
				}

				string assemblyInfo = string.Format(
					"{2} {1} in {0}",
					Path.GetFileName(test.Assembly.Location),
					test.TestType.FullName,
					implementationPattern);
				XmlElement assemblyRow = CreateTableRow();
				assemblyRow.AppendChild(CreateTableCell("Implementation:"));
				assemblyRow.AppendChild(CreateTableCell(assemblyInfo, 2));
				_htmlTable.AppendChild(assemblyRow);
			}

			if (test is IncludedTestFactory)
			{
				_htmlTable.AppendChild(
					GetSignatureRow(((IncludedTest) test).TestFactory));
			}

			AppendTestIssueCodes(test.IssueCodes);
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

		private void AppendTestConstructorTitle([NotNull] IncludedTestConstructor test)
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
