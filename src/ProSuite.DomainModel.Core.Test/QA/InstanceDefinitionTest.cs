using log4net.Core;
using NUnit.Framework;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.DomainModel.Core.Test.QA
{
	[TestFixture]
	public class InstanceDefinitionTest
	{
		[Test]
		public void CanCreateFromTransformerDescriptor()
		{
			var name = "name";
			var classDescriptor = new ClassDescriptor(typeof(IssueCode));

			TransformerDescriptor trans1 =
				new TransformerDescriptor(
					name, classDescriptor, 2, "just a test");

			TransformerDescriptor trans2 =
				new TransformerDescriptor(
					name, classDescriptor, 2, "just another test");

			var transDef1 = InstanceDefinition.CreateFrom(trans1);
			Assert.AreEqual(name, transDef1.Name);

			var transDef2 = InstanceDefinition.CreateFrom(trans2);
			Assert.IsTrue(transDef1.Equals(transDef2));
		}

		[Test]
		public void CanCreateFromTestDescriptor()
		{
			var name = "name";
			var classDescriptor = new ClassDescriptor(typeof(IssueCode));

			TestDescriptor test1 =
				new TestDescriptor(
					name, classDescriptor, 2, false, true, "just a test");

			TestDescriptor test2 =
				new TestDescriptor(
					"differentName", classDescriptor, 2, true, true, "just another test");

			TestDescriptor test3 =
				new TestDescriptor(
					name, classDescriptor, 1, true, true, "just another test");

			var testDef1 = InstanceDefinition.CreateFrom(test1);
			Assert.AreEqual(name, testDef1.Name);

			// Only the class descriptor and constructors are compared!
			var testDef2 = InstanceDefinition.CreateFrom(test2);
			Assert.IsTrue(testDef1.Equals(testDef2));

			// Different constructor
			var testDef3 = InstanceDefinition.CreateFrom(test3);
			Assert.IsFalse(testDef1.Equals(testDef3));
		}

		[Test]
		public void CanCreateFromTestDescriptorUsingFactory()
		{
			var name = "name";
			var classDescriptor = new ClassDescriptor(typeof(IssueCode));
			var differentClassDescriptor = new ClassDescriptor(typeof(ErrorCode));

			TestDescriptor test1 =
				new TestDescriptor(
					name, classDescriptor, false, true, "just a test");

			TestDescriptor test2 =
				new TestDescriptor(
					"different Name", classDescriptor, false, true, "just another test");

			TestDescriptor test3 =
				new TestDescriptor(
					name, differentClassDescriptor, false, true, "just another test");

			var testDef1 = InstanceDefinition.CreateFrom(test1);
			Assert.AreEqual(name, testDef1.Name);

			var testDef2 = InstanceDefinition.CreateFrom(test2);
			Assert.IsTrue(testDef1.Equals(testDef2));

			var testDef3 = InstanceDefinition.CreateFrom(test3);
			Assert.IsFalse(testDef1.Equals(testDef3));
		}
	}
}
