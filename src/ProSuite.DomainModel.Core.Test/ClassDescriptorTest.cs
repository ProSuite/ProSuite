using NUnit.Framework;

namespace ProSuite.DomainModel.Core.Test
{
	[TestFixture]
	public class ClassDescriptorTest
	{
		[Test]
		public void CanInstantiateWithoutParameters()
		{
			var descriptor =
				new ClassDescriptor(
					"ProSuite.DomainModel.Core.ClassDescriptor",
					"ProSuite.DomainModel.Core");
			var target = descriptor.CreateInstance<ClassDescriptor>();

			Assert.IsNotNull(target);
			Assert.IsNull(target.AssemblyName);
			Assert.IsNull(target.TypeName);
		}

		[Test]
		public void CanInstantiateWithParameters()
		{
			const string typeName = "someTypeName";
			const string assemblyName = "someAssemblyName";

			var descriptor =
				new ClassDescriptor(
					"ProSuite.DomainModel.Core.ClassDescriptor",
					"ProSuite.DomainModel.Core");
			var target =
				descriptor.CreateInstance<ClassDescriptor>(typeName, assemblyName);

			Assert.IsNotNull(target);
			Assert.AreEqual(typeName, target.TypeName);
			Assert.IsNotNull(assemblyName, target.AssemblyName);
		}

		[Test]
		public void CanInstantiateWithType()
		{
			var descriptor = new ClassDescriptor(typeof(TestClass));

			var target = descriptor.CreateInstance<TestClass>("someName", 99);

			Assert.AreEqual("ProSuite.DomainModel.Core.Test.TestClass", descriptor.TypeName);
			Assert.AreEqual("ProSuite.DomainModel.Core.Test", descriptor.AssemblyName);

			Assert.AreEqual("someName", target.Name);
			Assert.AreEqual(99, target.Number);
		}
	}

	public class TestClass
	{
		public TestClass(string name, int number)
		{
			Name = name;
			Number = number;
		}

		public string Name { get; }

		public int Number { get; }
	}
}
