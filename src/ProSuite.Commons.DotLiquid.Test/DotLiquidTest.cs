using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DotLiquid;
using DotLiquid.FileSystems;
using DotLiquid.NamingConventions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using NUnit.Framework;

namespace ProSuite.Commons.DotLiquid.Test
{
	[TestFixture]
	public class DotLiquidTest
	{
		[Test]
		public void RenderHierarchyTest()
		{
			var items = new List<Item>
			            {
				            new Item("A",
				                     new[]
				                     {
					                     new Item("A1"),
					                     new Item("A2")
				                     }),
				            new Item("B",
				                     new[]
				                     {
					                     new Item("B1",
					                              new[]
					                              {
						                              new Item("B11")
					                              })
				                     }),
				            new Item("C")
			            };

			Template.NamingConvention = new CSharpNamingConvention();
			LiquidUtils.RegisterSafeType<Item>(recurse : false);

			Hash hash = Hash.FromDictionary(new Dictionary<string, object> {{"items", items}});

			Template.FileSystem = new HardcodedFileSystem("<ul><li>{{ item.Name }}" +
			                                              "{% include 'item' with item.Children %}" +
			                                              "</li></ul>");

			Template template = Template.Parse("<html>{% include 'item' with items %}</html>");

			string output = template.Render(hash);

			Console.WriteLine(output);

			Assert.AreEqual(
				@"<html><ul><li>A<ul><li>A1</li></ul><ul><li>A2</li></ul></li></ul><ul><li>B<ul><li>B1<ul><li>B11</li></ul></li></ul></li></ul><ul><li>C</li></ul></html>",
				output);
		}

		[Test]
		public void RenderNameValuePairKeyAccessTest()
		{
			var dictionary = new Dictionary<string, object>
			                 {
				                 {
					                 "header",
					                 new NameValuePairs(new Dictionary<string, string>
					                                    {
						                                    {"NAME1", "VALUE1"},
						                                    {"NAME2", "VALUE2"},
						                                    {"NAME3", "VALUE3"},
					                                    })
				                 },
			                 };

			Template.NamingConvention = new CSharpNamingConvention();
			LiquidUtils.RegisterSafeType<NameValuePairs>();

			Hash hash = Hash.FromDictionary(dictionary);

			// Note: keys are case sensitive
			// Note: unknown name results in empty string
			Template template = Template.Parse("name 1: {{ header.Value.NAME1 }} " +
			                                   "name 2: {{ header.Value.NAME2 }} " +
			                                   "name 3: {{ header.Value.NAME3 }} " +
			                                   "name 4: {{ header.Value.NAME4 }}");

			string output = template.Render(hash);

			Console.WriteLine(output);

			Assert.AreEqual("name 1: VALUE1 name 2: VALUE2 name 3: VALUE3 name 4: ", output);
		}

		[Test]
		public void RenderIteratedNameValuePairsTest()
		{
			var input = new Dictionary<string, string>
			            {
				            {"NAME1", "VALUE1"},
				            {"NAME2", "VALUE2"},
				            {"NAME3", "VALUE3"},
			            };

			var dictionary = new Dictionary<string, object>
			                 {
				                 {"header", new NameValuePairs(input)},
			                 };

			Template.NamingConvention = new CSharpNamingConvention();
			LiquidUtils.RegisterSafeType<NameValuePairs>();

			RegisterSafeType<NameValuePair>();

			Hash hash = Hash.FromDictionary(dictionary);

			var sb = new StringBuilder();

			sb.AppendLine("{% for entry in header.Entries -%}");
			sb.AppendLine("- {{ entry.Name }}: {{ entry.Value }}");
			sb.AppendLine("{% endfor -%}");

			Template template = Template.Parse(sb.ToString());

			string output = template.Render(hash);

			Console.WriteLine(output);

			string expected = "- NAME1: VALUE1" + Environment.NewLine +
			                  "- NAME2: VALUE2" + Environment.NewLine +
			                  "- NAME3: VALUE3" + Environment.NewLine;
			Assert.AreEqual(expected, output);
		}

		[Test]
		public void RenderNameValuePairsTest()
		{
			var nameValuePairs = new Dictionary<string, string>
			                     {
				                     {"NAME1", "VALUE1"},
				                     {"NAME2", "VALUE2"}
			                     };

			var report = new TestReport
			             {
				             StartTime = new DateTime(2000, 12, 31, 23, 59, 58)
			             };

			var dictionary = new Dictionary<string, object>
			                 {
				                 {"header", nameValuePairs},
				                 {"report", report}
			                 };

			Template.NamingConvention = new CSharpNamingConvention();
			RegisterSafeType<TestReport>();

			Hash hash = Hash.FromDictionary(dictionary);

			// to switch to ruby date format: Liquid.UseRubyDateFormat = true;
			Template template = Template.Parse(
				@"start time: {{report.StartTime | Date: ""yyyy-MM-dd""}} name1: {{header.NAME1}} undefined name: {{header.NOT_DEFINED}}");

			string output = template.Render(hash);

			Console.WriteLine(output);

			Assert.AreEqual("start time: 2000-12-31 name1: VALUE1 undefined name: ", output);
		}

		[Test]
		public void RenderSimpleValueTest()
		{
			Template.NamingConvention = new CSharpNamingConvention();
			var report = new TestReport
			             {
				             StartTime = new DateTime(2000, 12, 31, 23, 59, 58)
			             };

			// to switch to ruby date format: Liquid.UseRubyDateFormat = true;
			Template template = Template.Parse(
				@"start time: {{StartTime | Date: ""yyyy-MM-dd HH:mm:ss""}}");

			// using a single model
			Hash hash = Hash.FromAnonymousObject(report);

			string output = template.Render(hash);

			Console.WriteLine(output);

			string expected = string.Format("start time: {0}",
			                                report.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
			Assert.AreEqual(expected, output);
		}

		[Test]
		public void RenderCollectionTest()
		{
			Template.NamingConvention = new CSharpNamingConvention();

			var report = new TestReport();

			report.AddVerifiedDataset(new VerifiedDataset("ds1", "ws1"));
			report.AddVerifiedDataset(new VerifiedDataset("ds2", "ws2"));

			// template syntax: https://github.com/Shopify/liquid/wiki/Liquid-for-Designers
			var sb = new StringBuilder();
			sb.AppendLine("verified datasets:");
			sb.AppendLine("{% for dataset in report.VerifiedDatasets -%}");
			sb.AppendLine("- dataset: {{dataset.Name}} - {{dataset.WorkspaceName | Upcase}}");
			sb.AppendLine("{% endfor -%}");

			// NOTE: filter casing also depends on naming convention (Upcase for C#, upcase for ruby)

			RegisterSafeType<TestReport>();
			RegisterSafeType<VerifiedDataset>();

			Template template = Template.Parse(sb.ToString());

			// alternative when there are multiple models
			Hash hash = Hash.FromDictionary(new Dictionary<string, object>
			                                {
				                                {"report", Hash.FromAnonymousObject(report)}
			                                });

			string output = template.Render(hash);
			Console.WriteLine(output);

			var expected = new StringBuilder();
			expected.AppendLine("verified datasets:");
			expected.AppendLine("- dataset: ds1 - WS1");
			expected.AppendLine("- dataset: ds2 - WS2");

			Assert.AreEqual(expected.ToString(), output);
		}

		[Test]
		public void LiquidUtilsRenderTest()
		{
			const string tempFolder = @"C:\Temp";
			Assert.True(Directory.Exists(tempFolder), "Directory {0} does not exist",
			            tempFolder);

			string templateFileName = Path.Combine(tempFolder, @"test.html.tpl");
			string reportFileName = Path.Combine(tempFolder, @"report.html");
			File.Delete(templateFileName);
			File.Delete(reportFileName);

			var report = new TestReport();
			report.StartTime = new DateTime(2000, 12, 31, 23, 59, 58);
			report.AddVerifiedDataset(new VerifiedDataset("ds1", "ws1"));
			report.AddVerifiedDataset(new VerifiedDataset("ds2", "ws2"));

			var sb = new StringBuilder();
			sb.AppendLine(
				"<html><head><title>LiquidUtilsRenderTest{{report.StartTime}}</title></head>");
			sb.AppendLine("<body>");
			sb.AppendLine("<table>");
			sb.AppendLine("{% for dataset in report.VerifiedDatasets -%}");
			sb.AppendLine(
				"<tr><td>dataset:</td><td>{{dataset.Name}}</td><td>{{dataset.WorkspaceName | Upcase}}</td></tr>");
			sb.AppendLine("{% endfor -%}");
			sb.AppendLine("</table>");
			sb.AppendLine("</body>");
			sb.AppendLine("</html>");
			FileSystemUtils.WriteTextFile(sb.ToString(), templateFileName);
			Assert.True(File.Exists(templateFileName), "{0} does not exist", templateFileName);

			LiquidUtils.RegisterSafeType<TestReport>();
			string output = LiquidUtils.Render(templateFileName, report, "report");
			Assert.True(output.Length > 0, "output is empty");

			FileSystemUtils.WriteTextFile(output, reportFileName);
			Assert.True(File.Exists(reportFileName), "{0} does not exist", reportFileName);
		}

		[NotNull]
		private static string[] GetMemberNames<T>()
		{
			return typeof(T).GetMembers().Select(m => m.Name).ToArray();
		}

		private static void RegisterSafeType<T>()
		{
			Template.RegisterSafeType(typeof(T), GetMemberNames<T>());
		}

		private class HardcodedFileSystem : IFileSystem
		{
			private readonly string _templateString;

			public HardcodedFileSystem([NotNull] string templateString)
			{
				_templateString = templateString;
			}

			public string ReadTemplateFile(Context context, string templateName)
			{
				return _templateString;
			}
		}

		private class Item
		{
			private readonly string _name;
			private readonly List<Item> _children = new List<Item>();

			public Item([NotNull] string name,
			            [CanBeNull] IEnumerable<Item> children = null)
			{
				_name = name;

				if (children != null)
				{
					_children.AddRange(children);
				}
			}

			[NotNull]
			[UsedImplicitly]
			public string Name
			{
				get { return _name; }
			}

			[NotNull]
			[UsedImplicitly]
			public List<Item> Children
			{
				get { return _children; }
			}
		}
	}
}
