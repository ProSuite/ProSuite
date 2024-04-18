using System;
using System.Collections;
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
using ProSuite.QA.Tests.ParameterTypes;
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
				                             typeof(QaBorderSense),
				                             typeof(QaCoplanarRings),
				                             typeof(QaConstraint),
				                             typeof(QaCurve),
				                             typeof(QaDateFieldsWithoutTime),
				                             typeof(QaEmptyNotNullTextFields),
				                             typeof(QaExtent),
				                             typeof(QaFlowLogic),
				                             typeof(QaGdbRelease),
				                             typeof(QaGeometryConstraint),
				                             //typeof(QaGroupConstraints),
				                             typeof(QaHorizontalSegments),
				                             typeof(QaSimpleGeometry),
				                             typeof(QaSurfacePipe),
				                             typeof(QaValue),
				                             typeof(QaWithinBox),
											 typeof(QaWithinZRange),
				                             typeof(QaZDifferenceOther),
				                             typeof(QaZDifferenceSelf),
			                             };

			foreach (Type testType in refactoredTypes)
			{
				Assert.IsFalse(InstanceUtils.HasInternallyUsedAttribute(testType),
				               "Internally used tests are only used by factories and do not require a TestDefinition");

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

					// NOTE: The instantiation of the tests and the comparisons of the values are
					// performed in the AreParametersEqual test.
				}
			}
		}

		private record TestDefinitionCase(
			Type TestType,
			int ConstructorIndex = -1,
			object[] ConstructorValues = null,
			Dictionary<string, object> OptionalParamValues = null);

		[Test]
		public void AreParametersEqual()
		{
			var model = new InMemoryTestDataModel("simple model");

			var testCases = new List<TestDefinitionCase>();

			//// Test cases with automatic parameter value generation:
			testCases.AddRange(CreateDefaultValueTestCases(typeof(Qa3dConstantZ)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaBorderSense)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaCoplanarRings)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaConstraint)));

			// TODO: Implement Definition
			//testCases.AddRange(CreateDefaultValueTestCases(typeof(QaEmptyNotNullTextFields)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaExtent)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaFlowLogic)));

			// TODO: Add special case 
			//testCases.AddRange(CreateDefaultValueTestCases(typeof(QaGdbRelease)));
			//testCases.AddRange(CreateDefaultValueTestCases(typeof(QaGeometryConstraint)));
			//testCases.AddRange(CreateDefaultValueTestCases(typeof(QaGroupConstraints)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaHorizontalSegments)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSimpleGeometry)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSurfacePipe)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaValue)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaWithinZRange)));

			//
			// Special Cases
			//
			// Manually create values for special cases, such as optional parameters or
			// difficult assertions:
			AddQaCurveTestCases(model, testCases); //example optional parameters
			AddQaDateFieldsWithoutTimeCases(model, testCases); //example for assertions requiring special parameter values		
			AddQaVertexCoincidenceSelf(model, testCases);												   //			
			AddQaWithinBox(model, testCases);
			AddQaZDifferenceOther(model, testCases);
			AddQaZDifferenceSelf(model, testCases);

			foreach (TestDefinitionCase testCase in testCases)
			{
				Type testType = testCase.TestType;
				int constructorIdx = testCase.ConstructorIndex;

				Console.WriteLine("Checking constructor index {0} for test {1}", constructorIdx,
				                  testType.Name);

				object[] constructorValues = testCase.ConstructorValues;

				// Optional parameters Name-Value pairs, null if not specified (or no optional parameters):
				Dictionary<string, object> optionalParamValues = testCase.OptionalParamValues;

				TestDescriptor testImplDescriptor =
					CreateTestDescriptor(testType, constructorIdx);

				ClassDescriptor classDescriptor = testImplDescriptor.Class;
				Assert.NotNull(classDescriptor);

				TestFactory testDefinitionFactory =
					TestFactoryUtils.GetTestDefinitionFactory(testImplDescriptor);

				Assert.NotNull(testDefinitionFactory);

				bool hasAlgorithmDefinition =
					InstanceDescriptorUtils.TryGetAlgorithmDefinitionType(
						classDescriptor, out Type definitionType);

				Assert.IsTrue(hasAlgorithmDefinition,
				              $"{testCase.TestType.Name} has no TestDefinition class");

				TestDescriptor testDefDescriptor =
					CreateTestDescriptor(definitionType, constructorIdx);

				QualityCondition testCondition =
					new QualityCondition("qc", testImplDescriptor);
				QualityCondition testDefCondition =
					new QualityCondition("qc", testDefDescriptor);

				testDefinitionFactory.Condition = testDefCondition;
				InstanceConfigurationUtils.InitializeParameterValues(
					testDefinitionFactory, testDefCondition);

				// The factory of the implementations
				TestFactory testFactory = TestFactoryUtils.CreateTestFactory(testCondition);

				Assert.NotNull(testFactory);

				ConstructorInfo constructorInfo = testType.GetConstructors()[constructorIdx];

				Assert.AreEqual(constructorInfo.GetParameters().Length,
				                constructorValues.Length,
				                $"Wrong numbers of constructor parameters for constructor index {constructorIdx}");

				// Create the test parameter values:

				int constructorParameterCount =
					testFactory.Parameters.Count(p => p.IsConstructorParameter);
				for (var i = 0; i < constructorParameterCount; i++)
				{
					TestParameter parameter = testFactory.Parameters[i];
					object value = constructorValues[i];

					string parameterName = parameter.Name;

					AddParameterValue(parameterName, value, testCondition, testDefCondition);
				}

				if (optionalParamValues != null)
				{
					foreach (KeyValuePair<string, object> optionalParam in
					         optionalParamValues)
					{
						AddParameterValue(optionalParam.Key, optionalParam.Value, testCondition,
						                  testDefCondition);
					}
				}

				IList<ITest> testsOrig = testFactory.CreateTests(
					new SimpleDatasetOpener(model));

				IList<ITest> testsNew = testDefinitionFactory.CreateTests(
					new SimpleDatasetOpener(model));

				Assert.AreEqual(1, testsOrig.Count, "Special Case: Multiple tests created.");
				Assert.AreEqual(1, testsNew.Count);

				ReflectionCompare.RecrusiveReflectionCompare(testsOrig[0], testsNew[0]);
			}
		}

		private static void AddQaCurveTestCases(InMemoryTestDataModel model,
		                                        ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("AllowedNonLinearSegmentTypes",
			                   new List<NonLinearSegmentType> { NonLinearSegmentType.Bezier });
			optionalValues.Add("GroupIssuesBySegmentType", true);

			testCases.Add(new TestDefinitionCase(typeof(QaCurve), 0,
			                                     new object[]
			                                     { model.GetVectorDataset() },
			                                     optionalValues));
		}

		private static void AddQaDateFieldsWithoutTimeCases(
			InMemoryTestDataModel model, ICollection<TestDefinitionCase> testCases)
		{
			testCases.Add(new TestDefinitionCase(typeof(QaDateFieldsWithoutTime), 0,
			                                     new object[]
			                                     { model.GetVectorDataset() }));
			testCases.Add(new TestDefinitionCase(typeof(QaDateFieldsWithoutTime), 1,
			                                     new object[]
			                                     { model.GetVectorDataset(), "MY_DATE_FIELD1" }));
			testCases.Add(new TestDefinitionCase(typeof(QaDateFieldsWithoutTime), 2,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     new[] { "MY_DATE_FIELD1", "MY_DATE_FIELD2" }
			                                     }));
		}

		private static void AddQaVertexCoincidenceSelf(InMemoryTestDataModel model,
		                                               ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("PointTolerance", -1);
			optionalValues.Add("EdgeTolerance", -1);
			optionalValues.Add("RequireVertexOnNearbyEdge", true);
			optionalValues.Add("CoincidenceTolerance", 0);
			optionalValues.Add("Is3D", false);
			optionalValues.Add("VerifyWithinFeature", true);
			optionalValues.Add("ZTolerance", 0);
			optionalValues.Add("ZCoincidenceTolerance", 0);
			optionalValues.Add("ReportCoordinates", true);

			testCases.Add(new TestDefinitionCase(typeof(QaVertexCoincidenceSelf), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
			                                     },
			                                     optionalValues));

			testCases.Add(new TestDefinitionCase(typeof(QaVertexCoincidenceSelf), 1,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     }
			                                     },
			                                     optionalValues));

			testCases.Add(new TestDefinitionCase(typeof(QaVertexCoincidenceSelf), 2,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     "G1.CountryCode <> G2.CountryCode"
			                                     },
			                                     optionalValues));
		}

		private static void AddQaZDifferenceOther(InMemoryTestDataModel model,
		                                          ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("RelevantRelationCondition", "U.EdgeLevel > L.EdgeLevel");
			optionalValues.Add("MinimumZDifferenceExpression", "U.EdgeLevel > L.EdgeLevel");
			optionalValues.Add("MaximumZDifferenceExpression", "U.EdgeLevel > L.EdgeLevel");
			optionalValues.Add("UseDistanceFromReferenceRingPlane", true);
			optionalValues.Add("ReferenceRingPlaneCoplanarityTolerance", 1);
			optionalValues.Add("IgnoreNonCoplanarReferenceRings", true);

			testCases.Add(new TestDefinitionCase(typeof(QaZDifferenceOther), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     1, ZComparisonMethod.IntersectionPoints,
				                                     "U.EdgeLevel > L.EdgeLevel"
			                                     },
			                                     optionalValues));

			testCases.Add(new TestDefinitionCase(typeof(QaZDifferenceOther), 1,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1,
				                                     ZComparisonMethod.IntersectionPoints,
				                                     "U.EdgeLevel > L.EdgeLevel"
			                                     },
			                                     optionalValues));

			testCases.Add(new TestDefinitionCase(typeof(QaZDifferenceOther), 2,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     1,
				                                     2,
				                                     ZComparisonMethod.IntersectionPoints,
				                                     "U.EdgeLevel > L.EdgeLevel"
			                                     },
			                                     optionalValues));

			testCases.Add(new TestDefinitionCase(typeof(QaZDifferenceOther), 3,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1,
				                                     2,
				                                     ZComparisonMethod.IntersectionPoints,
				                                     "U.EdgeLevel > L.EdgeLevel"
			                                     },
			                                     optionalValues));
		}

		private static void AddQaWithinBox(InMemoryTestDataModel model,
		                                   ICollection<TestDefinitionCase> testCases)
		{
			testCases.Add(new TestDefinitionCase(typeof(QaWithinBox), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     2600000, 1200000, 2610000, 1210000
			                                     }));

			testCases.Add(new TestDefinitionCase(typeof(QaWithinBox), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     2600000, 1200000, 2610000, 1210000,
				                                     true
			                                     }));
		}

		private static void AddQaZDifferenceSelf(InMemoryTestDataModel model,
		                                         ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("MinimumZDifferenceExpression", "U.EdgeLevel > L.EdgeLevel");
			optionalValues.Add("MaximumZDifferenceExpression", "U.EdgeLevel > L.EdgeLevel");

			testCases.Add(new TestDefinitionCase(typeof(QaZDifferenceSelf), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     1,
				                                     ZComparisonMethod.IntersectionPoints,
				                                     "U.EdgeLevel > L.EdgeLevel"
			                                     },
			                                     optionalValues));

			testCases.Add(new TestDefinitionCase(typeof(QaZDifferenceSelf), 1,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1,
				                                     ZComparisonMethod.IntersectionPoints,
				                                     "U.EdgeLevel > L.EdgeLevel"
			                                     },
			                                     optionalValues));

			testCases.Add(new TestDefinitionCase(typeof(QaZDifferenceSelf), 2,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     1,
				                                     2,
				                                     ZComparisonMethod.IntersectionPoints,
				                                     "U.EdgeLevel > L.EdgeLevel"
			                                     },
			                                     optionalValues));

			testCases.Add(new TestDefinitionCase(typeof(QaZDifferenceSelf), 3,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1,
				                                     2,
				                                     ZComparisonMethod.IntersectionPoints,
				                                     "U.EdgeLevel > L.EdgeLevel"
			                                     },
			                                     optionalValues));
		}

		private static void AddParameterValue(string parameterName, object value,
		                                      QualityCondition testCondition,
		                                      QualityCondition testDefCondition)
		{
			// NOTE: For lists, multiple TestParameterValues can be added for the same TestParameter.
			if (value is IEnumerable enumerable and not string)
			{
				foreach (object singleValue in enumerable)
				{
					AddSingleParameterValue(parameterName, singleValue, testCondition,
					                        testDefCondition);
				}
			}
			else
			{
				AddSingleParameterValue(parameterName, value, testCondition, testDefCondition);
			}
		}

		private static void AddSingleParameterValue(string parameterName, object value,
		                                            QualityCondition testCondition,
		                                            QualityCondition testDefCondition)
		{
			if (value is Dataset datasetVal)
			{
				TestParameterValueUtils.AddParameterValue(
					testCondition, parameterName, datasetVal);
				TestParameterValueUtils.AddParameterValue(
					testDefCondition, parameterName, datasetVal);
			}
			else
			{
				string stringVal = Convert.ToString(value);
				TestParameterValueUtils.AddParameterValue(
					testCondition, parameterName, stringVal);
				TestParameterValueUtils.AddParameterValue(
					testDefCondition, parameterName, stringVal);
			}
		}

		private static IEnumerable<TestDefinitionCase> CreateDefaultValueTestCases(Type testType)
		{
			// One is used internally to create using a the definition.
			int constructorCount = testType.GetConstructors().Length - 1;

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

				QualityCondition testCondition = new QualityCondition("qc", testImplDescriptor);

				// The factory of the implementations
				TestFactory testFactory = TestFactoryUtils.CreateTestFactory(testCondition);

				Assert.NotNull(testFactory);

				var model = new InMemoryTestDataModel("simple model");

				var parameterList = new List<object>();
				foreach (TestParameter parameter in testFactory.Parameters)
				{
					object defaultVal =
						CreateDefaultValue(
							TestParameterTypeUtils.GetParameterType(parameter.Type), model);

					parameterList.Add(defaultVal);
				}

				TestDefinitionCase result = new TestDefinitionCase(testType,
					constructorIdx, parameterList.ToArray());

				bool hasOptionalParameters =
					testFactory.Parameters.Any(p => ! p.IsConstructorParameter);

				Assert.False(hasOptionalParameters,
				             $"The type {testType.Name} has optional parameters. Use manual configuration!");

				yield return result;
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
					return "OBJECTID";

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

				if (property.Name == "SyncRoot")
				{
					// Arrays have a SyncRoot property that results in stck overflow
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
