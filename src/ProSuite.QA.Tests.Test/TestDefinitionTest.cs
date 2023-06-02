using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.QA.TestFactories;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class TestDefinitionTest
	{
		[Test]
		public void CanCreateTestFactories()
		{
			List<Type> refactoredTypes = new List<Type>
			                             {
				                             typeof(QaConstraintsListFactory),
				                             typeof(QaGdbConstraintFactory)
			                             };

			foreach (Type factoryType in refactoredTypes)
			{
				TestDescriptor descriptor = CreateTestFactoryDescriptor(factoryType);

				// Use case 1: Using instance info in DDX:
				IInstanceInfo instanceInfo = InstanceDescriptorUtils.GetInstanceInfo(descriptor);

				Assert.NotNull(instanceInfo);
				Assert.NotNull(instanceInfo.TestDescription);
				Assert.Greater(instanceInfo.TestCategories.Length, 0);

				TestFactoryDefinition factoryDefinition = (TestFactoryDefinition) instanceInfo;

				// Use case 2: Load actual factory:
				TestFactory testFactory = TestFactoryUtils.GetTestFactory(descriptor);

				Assert.NotNull(testFactory);

				QaFactoryBase qaFactory = (QaFactoryBase) testFactory;

				Assert.IsTrue(Compare(instanceInfo, testFactory));

				Assert.Greater(factoryDefinition.Parameters.Count, 0);
				Assert.Greater(qaFactory.Parameters.Count, 0);
			}
		}

		private bool Compare(IInstanceInfo instanceInfo1, IInstanceInfo instanceInfo2)
		{
			if (instanceInfo1.TestDescription != instanceInfo2.TestDescription)
			{
				return false;
			}

			for (var i = 0; i < instanceInfo1.TestCategories.Length; i++)
			{
				string category1 = instanceInfo1.TestCategories[i];
				string category2 = instanceInfo2.TestCategories[i];

				if (category1 != category2)
				{
					return false;
				}
			}

			if (instanceInfo1.Parameters.Count != instanceInfo2.Parameters.Count)
			{
				return false;
			}

			for (var i = 0; i < instanceInfo1.Parameters.Count; i++)
			{
				TestParameter parameter1 = instanceInfo1.Parameters[i];
				TestParameter parameter2 = instanceInfo2.Parameters[i];

				if (parameter1.Type != parameter2.Type)
				{
					return false;
				}

				if (parameter1.Name != parameter2.Name)
				{
					return false;
				}

				if (parameter1.Description != parameter2.Description)
				{
					return false;
				}

				// Boxed value types are not equal! -> Use Equals
				if (! Equals(parameter1.DefaultValue, parameter2.DefaultValue))
				{
					return false;
				}

				if (parameter1.IsConstructorParameter != parameter2.IsConstructorParameter)
				{
					return false;
				}
			}

			return true;
		}

		private static TestDescriptor CreateTestFactoryDescriptor(Type factoryType)
		{
			string testName = $"{factoryType.Name}";

			return new TestDescriptor(testName, new ClassDescriptor(factoryType));
		}
	}
}
