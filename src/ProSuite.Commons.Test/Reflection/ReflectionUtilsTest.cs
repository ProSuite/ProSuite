using System;
using System.Reflection;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;

namespace ProSuite.Commons.Test.Reflection
{
	[TestFixture]
	public class ReflectionUtilsTest
	{
		[Test]
		public void CanGetPublicMemberCountForList()
		{
			WriteStats("ProSuite.Commons.Essentials",
			           "ProSuite.Commons.Logging",
			           "ProSuite.Commons",
			           "ProSuite.Commons.AO",
			           "ProSuite.Commons.UI",
			           "ProSuite.Commons.Sys",
			           "ProSuite.Commons.AGD",
			           "ProSuite.Commons.IoC",
			           "ProSuite.Commons.Orm.NHibernate",
			           "ProSuite.ProSuite.AGD.AttributeEditor",
			           "ProSuite.ProSuite.AGD.WorkLists");
		}

		private static void WriteStats(params string[] assemblyNames)
		{
			foreach (string assemblyName in assemblyNames)
			{
				Assembly assembly;
				try
				{
					assembly = Assembly.Load(assemblyName);
				}
				catch (Exception e)
				{
					Console.WriteLine(@"Error loading assembly {0}: {1}", assemblyName, e.Message);

					continue;
				}

				WriteStats(assembly);
				Console.WriteLine();
			}
		}

		private static void WriteStats([NotNull] Assembly assembly)
		{
			int classCount;
			int interfaceCount;
			int structCount;
			int typeCount = ReflectionUtils.GetPublicTypeCount(assembly,
			                                                   out classCount,
			                                                   out interfaceCount,
			                                                   out structCount);

			Console.WriteLine(@"{0}", assembly.GetName().Name);

			Console.WriteLine(@"- public types: {0}", typeCount);
			Console.WriteLine(@"  - classes: {0}", classCount);
			Console.WriteLine(@"  - interfaces: {0}", interfaceCount);
			Console.WriteLine(@"  - structs: {0}", structCount);

			int methodCount;
			int propertyCount;
			int fieldCount;
			int nestedTypeCount;
			int eventCount;
			int constructorCount;
			int memberCount = ReflectionUtils.GetPublicMemberCount(assembly,
				out methodCount,
				out propertyCount,
				out fieldCount,
				out nestedTypeCount,
				out eventCount,
				out constructorCount);

			Console.WriteLine(@"- public members: {0}", memberCount);
			Console.WriteLine(@"  - methods: {0}", methodCount);
			Console.WriteLine(@"  - properties: {0}", propertyCount);
			Console.WriteLine(@"  - fields: {0}", fieldCount);
			Console.WriteLine(@"  - nested types: {0}", nestedTypeCount);
			Console.WriteLine(@"  - events: {0}", eventCount);
			Console.WriteLine(@"  - constructors: {0}", constructorCount);
		}
	}
}
