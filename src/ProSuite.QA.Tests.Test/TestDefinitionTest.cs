using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.TestFactories;
using ProSuite.QA.Tests.Constraints;
using ProSuite.QA.Tests.Test.TestData;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class TestDefinitionTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

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
				Console.WriteLine("Checking {0}", factoryType.Name);

				TestDescriptor descriptor = CreateTestFactoryDescriptor(factoryType);

				// Traditional way to get instance info:
				IInstanceInfo factoryInstanceInfo =
					InstanceDescriptorUtils.GetInstanceInfo(descriptor);

				Assert.NotNull(factoryInstanceInfo);
				Assert.NotNull(factoryInstanceInfo.TestDescription);
				Assert.Greater(factoryInstanceInfo.TestCategories.Length, 0);

				// Algorithm Definition:
				bool hasAlgorithmDefinition =
					InstanceDescriptorUtils.TryGetTestFactoryDefinition(
						descriptor, out TestFactoryDefinition factoryDefinition);

				Assert.IsTrue(hasAlgorithmDefinition,
				              $"TestFactoryDefinition is not implemented for {factoryType.Name}.");

				TestDescriptor factoryDefDescriptor = CreateTestFactoryDescriptor(factoryType);

				// Use case 1: Using instance info in DDX:
				IInstanceInfo factoryDefInstanceInfo =
					InstanceDescriptorUtils.GetInstanceInfo(factoryDefDescriptor, true);

				Assert.NotNull(factoryDefInstanceInfo);
				Assert.NotNull(factoryDefInstanceInfo.TestDescription);
				Assert.Greater(factoryDefInstanceInfo.TestCategories.Length, 0);

				Assert.IsTrue(AssertEqual(factoryDefInstanceInfo, factoryInstanceInfo));

				// Use case 2: Load actual factory:
				TestFactory testFactory = TestFactoryUtils.GetTestFactory(descriptor);

				Assert.NotNull(testFactory);

				QaFactoryBase qaFactory = (QaFactoryBase) testFactory;

				Assert.IsTrue(AssertEqual(factoryDefInstanceInfo, testFactory));

				Assert.Greater(factoryDefinition.Parameters.Count, 0);
				Assert.Greater(qaFactory.Parameters.Count, 0);
			}
		}

		[Test]
		public void CanCreateTests()
		{
			List<Type> refactoredTypes = new List<Type>
			                             {
				                             typeof(Qa3dConstantZ),
				                             typeof(QaConstraint),
				                             typeof(QaSimpleGeometry),
				                             typeof(QaSurfacePipe)
			                             };

			foreach (Type testType in refactoredTypes)
			{
				// One is used internally to create using a the definition.
				int constructorCount = testType.GetConstructors().Length - 1;

				bool lastConstructorIsInternallyUsed =
					InstanceUtils.IsInternallyUsed(testType, constructorCount);

				Assert.IsTrue(lastConstructorIsInternallyUsed,
				              $"Last constructor not internally used in {testType.Name}");

				for (int constructorIdx = 0;
				     constructorIdx < constructorCount;
				     constructorIdx++)
				{
					Console.WriteLine("Checking {0}({1})", testType.Name, constructorIdx);

					CompareMetadata(testType, constructorIdx);

					TestDescriptor testImplDescriptor =
						CreateTestDescriptor(testType, constructorIdx);

					ClassDescriptor classDescriptor = testImplDescriptor.Class;
					Assert.NotNull(classDescriptor);

					bool hasAlgorithmDefinition =
						InstanceDescriptorUtils.TryGetAlgorithmDefinitionType(
							classDescriptor, out Type definitionType);

					Assert.IsTrue(hasAlgorithmDefinition);
					Assert.NotNull(definitionType);

					TestDescriptor testDefDescriptor =
						CreateTestDescriptor(definitionType, constructorIdx);

					TestFactory testDefinitionFactory =
						TestFactoryUtils.GetTestDefinitionFactory(testImplDescriptor);

					Assert.NotNull(testDefinitionFactory);

					QualityCondition testCondition = new QualityCondition("qc", testImplDescriptor);
					QualityCondition testDefCondition =
						new QualityCondition("qc", testDefDescriptor);

					testDefinitionFactory.Condition = testDefCondition;
					InstanceConfigurationUtils.InitializeParameterValues(
						testDefinitionFactory, testDefCondition);

					// The factory of the implementations
					TestFactory testFactory = TestFactoryUtils.CreateTestFactory(testCondition);

					Assert.NotNull(testFactory);

					var model = new InMemoryTestDataModel("simple model");

					foreach (TestParameter parameter in testFactory.Parameters)
					{
						if (parameter.Type == typeof(ConstraintNode))
						{
							// TODO
							continue;
						}

						object defaultVal =
							CreateDefaultValue(
								TestParameterTypeUtils.GetParameterType(parameter.Type), model);

						if (defaultVal is string stringVal)
						{
							TestParameterValueUtils.AddParameterValue(
								testCondition, parameter.Name, stringVal);
							TestParameterValueUtils.AddParameterValue(
								testDefCondition, parameter.Name, stringVal);
						}
						else if (defaultVal is Dataset datasetVal)
						{
							TestParameterValueUtils.AddParameterValue(
								testCondition, parameter.Name, datasetVal);
							TestParameterValueUtils.AddParameterValue(
								testDefCondition, parameter.Name, datasetVal);
						}
					}

					IList<ITest> testsOrig = testFactory.CreateTests(
						new SimpleDatasetOpener(model));

					IList<ITest> testsNew = testDefinitionFactory.CreateTests(
						new SimpleDatasetOpener(model));

					Assert.AreEqual(1, testsOrig.Count);
					Assert.AreEqual(1, testsNew.Count);

					ReflectionCompare.RecrusiveReflectionCompare(testsOrig[0], testsNew[0]);
				}
			}
		}

		private static void CompareMetadata(Type testType,
		                                    int constructorIdx)
		{
			TestDescriptor testDescriptor = CreateTestDescriptor(testType, constructorIdx);

			Assert.NotNull(testDescriptor.Class);

			// Using instance info in DDX:
			IInstanceInfo testInstanceInfo =
				InstanceDescriptorUtils.GetInstanceInfo(testDescriptor);

			Assert.NotNull(testInstanceInfo);
			Assert.NotNull(testInstanceInfo.TestDescription);
			Assert.Greater(testInstanceInfo.TestCategories.Length, 0);

			// Algorithm Definition:
			bool hasAlgorithmDefinition =
				InstanceDescriptorUtils.TryGetAlgorithmDefinitionType(
					testDescriptor.Class, out Type definitionType);

			Assert.IsTrue(hasAlgorithmDefinition);

			// Using instance definition:
			InstanceDescriptor instanceDefDescriptor =
				CreateTestDescriptor(definitionType, constructorIdx);

			IInstanceInfo instanceDefInfo =
				InstanceDescriptorUtils.GetInstanceInfo(instanceDefDescriptor,
				                                        tryAlgorithmDefinition: true);

			Assert.NotNull(instanceDefInfo);
			Assert.NotNull(instanceDefInfo.TestDescription);
			Assert.Greater(instanceDefInfo.TestCategories.Length, 0);

			Assert.IsTrue(AssertEqual(testInstanceInfo, instanceDefInfo));

			// Use case 2: Load actual factory:
			TestFactory testFactory = TestFactoryUtils.GetTestFactory(testDescriptor);
			Assert.NotNull(testFactory);

			// and definition factory
			TestFactory testDefinitionFactory =
				TestFactoryUtils.GetTestDefinitionFactory(testDescriptor);
			Assert.NotNull(testDefinitionFactory);

			// and compare:
			Assert.IsTrue(AssertEqual(testFactory, testDefinitionFactory));

			Assert.Greater(testInstanceInfo.Parameters.Count, 0);
			Assert.Greater(testDefinitionFactory.Parameters.Count, 0);
		}

		private static object CreateDefaultValue(TestParameterType testParameterType,
		                                         InMemoryTestDataModel model)
		{
			switch (testParameterType)
			{
				case TestParameterType.String:
					return "string";

				case TestParameterType.Integer:
					return "1";

				case TestParameterType.Double:
					return "0.4";

				case TestParameterType.DateTime:
					return "1.1.2007";

				case TestParameterType.Boolean:
					return "true";

				case TestParameterType.Dataset:
					return model.GetObjectDataset();

				case TestParameterType.ObjectDataset:
					return model.GetObjectDataset();

				case TestParameterType.VectorDataset:
					return model.GetVectorDataset();

				case TestParameterType.TableDataset:
					return model.GetObjectDataset();

				case TestParameterType.TerrainDataset:

					return model.GetTerrainDataset();

				case TestParameterType.TopologyDataset:
					return new VerifiedTopologyDataset("topology");

				case TestParameterType.GeometricNetworkDataset:
					throw new NotImplementedException("Geometric Networks are de-supported");

				case TestParameterType.RasterMosaicDataset:
					return model.GetMosaicDataset();

				case TestParameterType.RasterDataset:
					return model.GetRasterDataset();

				case TestParameterType.CustomScalar:
					// e.g. attribute dependency mapping:
					return new Dictionary<string, string>();
			}

			throw new ArgumentException("Unhandled type " + testParameterType);
		}

		private static bool AssertEqual(IInstanceInfo instanceInfo1,
		                                IInstanceInfo instanceInfo2)
		{
			if (instanceInfo1.TestDescription != instanceInfo2.TestDescription)
			{
				throw new AssertionException("Different TestDescription");
			}

			for (var i = 0; i < instanceInfo1.TestCategories.Length; i++)
			{
				string category1 = instanceInfo1.TestCategories[i];
				string category2 = instanceInfo2.TestCategories[i];

				if (category1 != category2)
				{
					throw new AssertionException($"Different TestCategory at index {i}");
				}
			}

			if (instanceInfo1.Parameters.Count != instanceInfo2.Parameters.Count)
			{
				throw new AssertionException("Different number of parameters");
			}

			for (var i = 0; i < instanceInfo1.Parameters.Count; i++)
			{
				TestParameter parameter1 = instanceInfo1.Parameters[i];
				TestParameter parameter2 = instanceInfo2.Parameters[i];

				TestParameterType parameter1Type =
					TestParameterTypeUtils.GetParameterType(parameter1.Type);
				TestParameterType parameter2Type =
					TestParameterTypeUtils.GetParameterType(parameter2.Type);

				if (parameter1Type != parameter2Type)
				{
					throw new AssertionException(
						$"Different parameter type for parameter {i} ({parameter1.Name})");
				}

				if (parameter1.Name != parameter2.Name)
				{
					throw new AssertionException($"Different parameter name for parameter {i}");
				}

				if (parameter1.Description != parameter2.Description)
				{
					throw new AssertionException(
						$"Different parameter description for parameter {i}");
				}

				// Boxed value types are not equal! -> Use Equals
				if (! Equals(parameter1.DefaultValue, parameter2.DefaultValue))
				{
					throw new AssertionException(
						$"Different parameter default value for parameter {i}");
				}

				if (parameter1.IsConstructorParameter != parameter2.IsConstructorParameter)
				{
					throw new AssertionException(
						$"Different parameter IsConstructorParameter for parameter {i}");
				}
			}

			return true;
		}

		private static TestDescriptor CreateTestFactoryDescriptor(Type factoryType)
		{
			string testName = $"{factoryType.Name}";

			return new TestDescriptor(testName, new ClassDescriptor(factoryType));
		}

		private static TestDescriptor CreateTestDescriptor(Type type,
		                                                   int constructorIndex)
		{
			string testName = $"{type.Name}";

			return new TestDescriptor(testName, new ClassDescriptor(type),
			                          constructorIndex);
		}
	}

	public class ReflectionCompare
	{
		public List<PropertyInfo> SimpleReflectionCompare<T>(T first, T second)
			where T : class
		{
			List<PropertyInfo> differences = new List<PropertyInfo>();

			foreach (PropertyInfo property in first.GetType().GetProperties())
			{
				object value1 = property.GetValue(first, null);
				object value2 = property.GetValue(second, null);

				if (! value1.Equals(value2))
				{
					differences.Add(property);
				}
			}

			return differences;
		}

		public static List<KeyValuePair<Type, PropertyInfo>> RecrusiveReflectionCompare<T>(
			T first, T second)
			where T : class
		{
			var differences = new List<KeyValuePair<Type, PropertyInfo>>();

			var parentType = first.GetType();

			void CompareObject(object obj1, object obj2, PropertyInfo info)
			{
				if (! obj1.Equals(obj2))
				{
					differences.Add(new KeyValuePair<Type, PropertyInfo>(parentType, info));
				}
			}

			foreach (PropertyInfo property in parentType.GetProperties())
			{
				if (property.GetIndexParameters().Length != 0)
				{
					continue;
				}

				if (property.Name == "Capacity")
				{
					// Lists have some internal capacity that is not relevant but often different
					continue;
				}

				object value1 = property.GetValue(first, null);
				object value2 = property.GetValue(second, null);

				if (property.PropertyType == typeof(string))
				{
					if (string.IsNullOrEmpty(value1 as string) !=
					    string.IsNullOrEmpty(value2 as string))
					{
						CompareObject(value1, value2, property);
					}
				}
				else if (property.PropertyType.IsPrimitive)
				{
					CompareObject(value1, value2, property);
				}
				else
				{
					if (value1 == null && value2 == null)
					{
						continue;
					}

					differences.Concat(RecrusiveReflectionCompare(value1, value2));
				}
			}

			return differences;
		}
	}
}
