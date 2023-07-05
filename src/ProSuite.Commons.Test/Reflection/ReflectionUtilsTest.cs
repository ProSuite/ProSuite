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
			           "ProSuite.Commons.Orm.NHibernate");
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

		[Test]
		public void CanGetDefaultValue()
		{
			Assert.AreEqual(0, typeof(int).GetDefaultValue());
			Assert.AreEqual(0D, typeof(double).GetDefaultValue());
			Assert.AreEqual(false, typeof(bool).GetDefaultValue());
			Assert.AreEqual('\0', typeof(char).GetDefaultValue());

			// Default value of all reference types is null:
			Assert.IsNull(typeof(string).GetDefaultValue());

			// Default value of nullable types is null (even though nullable is a value type):
			Assert.IsNull(typeof(int?).GetDefaultValue());

			// Null just doesn't make sense here, so expect an arg null exception:
			Assert.Throws<ArgumentNullException>(() => ReflectionUtils.GetDefaultValue(null));
		}

		[Test]
		public void CanGetConstantValue()
		{
			var type = typeof(TestClass);
			Assert.AreEqual(12, type.GetConstantValue("Foo"));
			Assert.IsNull(type.GetConstantValue("Bar")); // private constants are excluded by default
			Assert.AreEqual(true, type.GetConstantValue("Bar", true)); // but may be included upon request
			Assert.AreEqual(4.2, type.GetConstantValue("Baz"));
			Assert.AreEqual("Hey", type.GetConstantValue("Quux")); // base class is included
			Assert.IsNull(type.GetConstantValue("NoSuchField"));
			Assert.IsNull(type.GetConstantValue(null));
		}

		private class TestClass : TestBase
		{
			[UsedImplicitly] public const int Foo = 12;
			[UsedImplicitly] private const bool Bar = true;
			[UsedImplicitly] public static double Baz = 4.2;
		}

		private class TestBase
		{
			[UsedImplicitly] public const string Quux = "Hey";
		}
	}
}
