using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.ParameterTypes;
using ProSuite.QA.TestFactories;
using ProSuite.QA.Tests.Coincidence;
using ProSuite.QA.Tests.IssueFilters;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Transformers;
using FieldInfo = System.Reflection.FieldInfo;
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

		private static List<Type> RefactoredTypes
		{
			get
			{
				List<Type> refactoredTypes = new List<Type>
				                             {
					                             typeof(Qa3dConstantZ),
					                             typeof(QaBorderSense),
					                             typeof(QaCentroids),
					                             typeof(QaConstraint),
					                             typeof(QaContainedPointsCount),
					                             typeof(QaContainsOther),
					                             typeof(QaCoplanarRings),
					                             typeof(QaCrossesOther),
					                             typeof(QaCrossesSelf),
					                             typeof(QaCurve),
					                             typeof(QaDangleCount),
					                             typeof(QaDateFieldsWithoutTime),
					                             typeof(QaDuplicateGeometrySelf),
					                             typeof(QaEdgeMatchBorderingLines),
					                             typeof(QaEdgeMatchBorderingPoints),
					                             typeof(QaEdgeMatchCrossingAreas),
					                             typeof(QaEdgeMatchCrossingLines),
					                             typeof(QaEmptyNotNullTextFields),
					                             typeof(QaExportTables),
					                             typeof(QaExtent),
					                             typeof(QaFlowLogic),
					                             typeof(QaForeignKey),
					                             typeof(QaFullCoincidence),
					                             typeof(QaGdbConstraint),
					                             typeof(QaGdbRelease),
					                             typeof(QaGeometryConstraint),
					                             typeof(QaGroupConnected),
					                             typeof(QaGroupConstraints),
					                             typeof(QaHorizontalSegments),
					                             typeof(QaInteriorIntersectsOther),
					                             typeof(QaInteriorIntersectsSelf),
					                             typeof(QaInteriorRings),
					                             typeof(QaIntersectionMatrixOther),
					                             typeof(QaIntersectionMatrixSelf),
					                             typeof(QaIntersectsOther),
					                             typeof(QaIntersectsSelf),
					                             typeof(QaIsCoveredByOther),
					                             typeof(QaLineConnectionFieldValues),
					                             typeof(QaLineGroupConstraints),
					                             typeof(QaLineIntersect),
					                             typeof(QaLineIntersectAngle),
					                             typeof(QaLineIntersectZ),
					                             typeof(QaMaxArea),
					                             typeof(QaMaxLength),
					                             typeof(QaMaxSlope),
					                             typeof(QaMaxVertexCount),
					                             typeof(QaMeasures),
					                             typeof(QaMeasuresAtPoints),
					                             typeof(QaMinAngle),
					                             typeof(QaMinArea),
					                             typeof(QaMinIntersect),
					                             typeof(QaMinLength),
					                             typeof(QaMinMeanSegmentLength),
					                             typeof(QaMinNodeDistance),
					                             typeof(QaMinSegAngle),
					                             typeof(QaMonotonicMeasures),
					                             typeof(QaMonotonicZ),
					                             typeof(QaMpAllowedPartTypes),
					                             typeof(QaMpConstantPointIdsPerRing),
					                             typeof(QaMpFootprintHoles),
					                             typeof(QaMpHorizontalAzimuths),
					                             typeof(QaMpHorizontalHeights),
					                             typeof(QaMpHorizontalPerpendicular),
					                             typeof(QaMpNonIntersectingRingFootprints),
					                             typeof(QaMpSinglePartFootprint),
					                             typeof(QaMpVertexNotNearFace),
					                             typeof(QaMpVerticalFaces),
					                             typeof(QaMultipart),
					                             typeof(QaMustBeNearOther),
					                             typeof(QaMustIntersectMatrixOther),
					                             typeof(QaMustIntersectOther),
					                             typeof(QaMustTouchOther),
					                             typeof(QaMustTouchSelf),
					                             typeof(QaNeighbourAreas),
					                             typeof(QaNoBoundaryLoops),
					                             typeof(QaNoClosedPaths),
					                             typeof(QaNodeLineCoincidence),
					                             typeof(QaNoGaps),
					                             typeof(QaNonEmptyGeometry),
					                             typeof(QaNotNear),
					                             typeof(QaNoTouchingParts),
					                             typeof(QaOrphanNode),
					                             typeof(QaOverlapsOther),
					                             typeof(QaOverlapsSelf),
					                             typeof(QaPartCoincidenceOther),
					                             typeof(QaPartCoincidenceSelf),
					                             typeof(QaPointNotNear),
					                             typeof(QaPointOnLine),
					                             typeof(QaPseudoNodes),
					                             typeof(QaRegularExpression),
					                             typeof(QaRequiredFields),
					                             typeof(QaRouteMeasuresContinuous),
					                             typeof(QaRouteMeasuresUnique),
					                             typeof(QaRowCount),
					                             typeof(QaSchemaFieldAliases),
					                             typeof(QaSchemaFieldDomainCodedValues),
					                             typeof(QaSchemaFieldDomainDescriptions),
					                             typeof(QaSchemaFieldDomainNameRegex),
					                             typeof(QaSchemaFieldDomainNames),
					                             typeof(QaSchemaFieldDomains),
					                             typeof(QaSchemaFieldNameRegex),
					                             typeof(QaSchemaFieldNames),
					                             typeof(QaSchemaFieldProperties),
					                             typeof(QaSchemaFieldPropertiesFromTable),
					                             typeof(QaSchemaReservedFieldNameProperties),
					                             typeof(QaSchemaReservedFieldNames),
					                             typeof(QaSchemaSpatialReference),
					                             typeof(QaSegmentLength),
					                             typeof(QaSimpleGeometry),
					                             typeof(QaSliverPolygon),
					                             typeof(QaSmooth),
					                             typeof(QaSurfacePipe),
					                             typeof(QaSurfaceSpikes),
					                             typeof(QaSurfaceVertex),
					                             typeof(QaTouchesOther),
					                             typeof(QaTouchesSelf),
					                             typeof(QaTrimmedTextFields),
					                             typeof(QaUnique),
					                             typeof(QaUnreferencedRows),
					                             typeof(QaValidCoordinateFields),
					                             typeof(QaValidDateValues),
					                             typeof(QaValidNonLinearSegments),
					                             typeof(QaValidUrls),
					                             typeof(QaValue),
					                             typeof(QaVertexCoincidence),
					                             typeof(QaVertexCoincidenceOther),
					                             typeof(QaVertexCoincidenceSelf),
					                             typeof(QaWithinBox),
					                             typeof(QaWithinZRange),
					                             typeof(QaZDifferenceOther),
					                             typeof(QaZDifferenceSelf),
					                             typeof(QaGdbTopology),
												 typeof(QaTopoNotNear)
				                             };
				return refactoredTypes;
			}
		}

		[Test]
		public void CanCreateTests()
		{
			foreach (Type testType in RefactoredTypes)
			{
				Assert.IsFalse(InstanceUtils.HasInternallyUsedAttribute(testType),
				               "Internally used tests are only used by factories and do not require a TestDefinition");

				// One is used internally to create using the definition.
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

					if (! InstanceUtils.IsObsolete(testType, constructorIdx))
					{
						bool hasNoCategory = TestTypeHasNoCategory(testType);

						CompareTestMetadata(testType, constructorIdx, hasNoCategory);
					}

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

		private static List<TestDefinitionCase> DefineTestCases(TestDataModel model)
		{
			var testCases = new List<TestDefinitionCase>();

			// Test cases with automatic parameter value generation:
			testCases.AddRange(CreateDefaultValueTestCases(typeof(Qa3dConstantZ), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaBorderSense), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaCentroids), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaConstraint), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaContainsOther), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaCoplanarRings), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaCrossesOther), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaCrossesSelf), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaDangleCount), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaDuplicateGeometrySelf), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaEmptyNotNullTextFields), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaExtent), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaFlowLogic), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaForeignKey), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaGdbConstraint), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaHorizontalSegments), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaLineConnectionFieldValues), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMaxArea), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMaxLength), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMaxVertexCount), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMeasures), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMeasuresAtPoints), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMinArea), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMinIntersect), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMinLength), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMinMeanSegmentLength), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMinNodeDistance), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMonotonicMeasures), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMpAllowedPartTypes), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMpConstantPointIdsPerRing), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMpHorizontalAzimuths), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMpHorizontalHeights), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMpHorizontalPerpendicular), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMpVerticalFaces), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMultipart), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMustIntersectOther), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMustTouchOther), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMustTouchSelf), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaNoClosedPaths), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaNonEmptyGeometry), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaOrphanNode), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaOverlapsSelf), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaOverlapsOther), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaPointOnLine), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaRequiredFields), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaRouteMeasuresContinuous), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaRouteMeasuresUnique), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaFieldAliases), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaFieldDomainCodedValues), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaFieldDomainNameRegex), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaFieldDomainNames), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaFieldDomains), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaFieldNameRegex), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaFieldNames), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaFieldProperties), model));
			testCases.AddRange(
				CreateDefaultValueTestCases(typeof(QaSchemaFieldPropertiesFromTable), model));
			testCases.AddRange(
				CreateDefaultValueTestCases(typeof(QaSchemaReservedFieldNameProperties), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaReservedFieldNames), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSegmentLength), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSimpleGeometry), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSurfacePipe), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSurfaceSpikes), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSurfaceVertex), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaNoTouchingParts), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaUnique), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaUnreferencedRows), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaValidNonLinearSegments), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaValue), model));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaWithinZRange), model));

			//
			// Special Cases
			//
			// Manually create values for special cases, such as optional parameters or
			// difficult assertions:
			AddQaContainedPointsCountCases(model, testCases);
			AddQaCurveCases(model, testCases); //example optional parameters
			AddQaDateFieldsWithoutTimeCases(
				model, testCases); //example for assertions requiring special parameter values
			AddQaEdgeMatchBorderingLinesCases(model, testCases);
			AddQaEdgeMatchBorderingPointsCases(model, testCases);
			AddQaEdgeMatchCrossingAreasCases(model, testCases);
			AddQaEdgeMatchCrossingLinesCases(model, testCases);
			AddQaExportTablesCases(model, testCases);
			AddQaFullCoincidenceCases(model, testCases);
			AddQaGdbReleaseCases(model, testCases);
			AddQaGeometryConstraintCases(model, testCases);
			AddQaGroupConnectedCases(model, testCases);
			AddQaGroupConstraintsCases(model, testCases);
			AddQaInteriorIntersectsOtherCases(model, testCases);
			AddQaInteriorIntersectsSelfCases(model, testCases);
			AddQaInteriorRingsCases(model, testCases);
			AddQaIntersectionMatrixOtherCases(model, testCases);
			AddQaIntersectionMatrixSelfCases(model, testCases);
			AddQaIntersectsOtherCases(model, testCases);
			AddQaIntersectsSelfCases(model, testCases);
			AddQaIsCoveredByOtherCases(model, testCases);
			AddQaLineGroupConstraintsCases(model, testCases);
			AddQaLineIntersectAngleCases(model, testCases);
			AddQaLineIntersectCases(model, testCases);
			AddQaLineIntersectZCases(model, testCases);
			AddQaMaxSlopeCases(model, testCases);
			AddQaMinAngleCases(model, testCases);
			AddQaMinSegAngleCases(model, testCases);
			AddQaMonotonicZCases(model, testCases);
			AddQaMpFootprintHolesCases(model, testCases);
			AddQaMpNonIntersectingRingFootprintsCases(model, testCases);
			AddQaMpSinglePartFootprintCases(model, testCases);
			AddQaMpVertexNotNearFaceCases(model, testCases);
			AddQaMustBeNearOtherCases(model, testCases);
			AddQaMustIntersectMatrixOtherCases(model, testCases);
			AddQaNeighbourAreasCases(model, testCases);
			AddQaNoBoundaryLoopsCases(model, testCases);
			AddQaNodeLineCoincidenceCases(model, testCases);
			AddQaNoGapsCases(model, testCases);
			AddQaNotNearCases(model, testCases);
			AddQaPartCoincidenceOtherCases(model, testCases);
			AddQaPartCoincidenceSelfCases(model, testCases);
			AddQaPointNotNearCases(model, testCases);
			AddQaPseudoNodesCases(model, testCases);
			AddQaRegularExpressionCases(model, testCases);
			AddQaRowCountCases(model, testCases);
			AddQaSchemaFieldDomainDescriptionsCases(model, testCases);
			AddQaSchemaSpatialReferenceCases(model, testCases);
			AddQaSliverPolygonCases(model, testCases);
			AddQaSmoothCases(model, testCases);
			AddQaTouchesOtherCases(model, testCases);
			AddQaTouchesSelfCases(model, testCases);
			AddQaTrimmedTextFieldsCases(model, testCases);
			AddQaValidCoordinateFieldsCases(model, testCases);
			AddQaValidDateValuesCases(model, testCases);
			AddQaValidUrlsCases(model, testCases);
			AddQaVertexCoincidenceCases(model, testCases);
			AddQaVertexCoincidenceOtherCases(model, testCases);
			AddQaVertexCoincidenceSelfCases(model, testCases);
			AddQaWithinBoxCases(model, testCases);
			AddQaZDifferenceOtherCases(model, testCases);
			AddQaZDifferenceSelfCases(model, testCases);

			// Cannot be tested using an in-memory workspace, as it requires a real geodatabase.
			AddQaGdbTopologyCases(model, testCases);

			AddQaTopoNotNearCases(model, testCases);

			return testCases;
		}

		[Test]
		public void AreParametersEqual()
		{
			var model = new TestDataModel("simple_model", false);

			List<TestDefinitionCase> testCases = DefineTestCases(model);

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

				List<KeyValuePair<Type, MemberInfo>> differences =
					ReflectionCompare.RecursiveReflectionCompare(testsOrig[0], testsNew[0], true);

				foreach (KeyValuePair<Type, MemberInfo> difference in differences)
				{
					Console.WriteLine("Difference: {0} {1}", difference.Key.Name,
					                  difference.Value.Name);
				}

				Assert.AreEqual(0, differences.Count,
				                $"Differences found for {testType.Name} constructor index {constructorIdx}:");
			}
		}

		#region Methods to add special TestDefinitionCases

		private static void AddQaContainedPointsCountCases(TestDataModel model,
		                                                   ICollection<TestDefinitionCase>
			                                                   testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("PolylineUsage", "asIs");

			testCases.Add(new TestDefinitionCase(typeof(QaContainedPointsCount), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetPointDataset(),
				                                     1,
				                                     "POLYGON.FACILITY_ID = POINT.FACILITY_ID"
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaContainedPointsCount), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetPointDataset(),
				                                     1,
				                                     2,
				                                     "POLYGON.FACILITY_ID = POINT.FACILITY_ID"
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaContainedPointsCount), 2,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetPointDataset(),
				                                     1,
				                                     2,
				                                     "POLYGON.FACILITY_ID = POINT.FACILITY_ID",
				                                     false
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaContainedPointsCount), 3,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetPointDataset()
				                                     },
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetPointDataset()
				                                     },
				                                     1,
				                                     "POLYGON.FACILITY_ID = POINT.FACILITY_ID"
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaContainedPointsCount), 4,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetPointDataset()
				                                     },
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetPointDataset()
				                                     },
				                                     1,
				                                     2,
				                                     "POLYGON.FACILITY_ID = POINT.FACILITY_ID",
				                                     false
			                                     },
			                                     optionalValues));
		}

		private static void AddQaCurveCases(TestDataModel model,
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
			TestDataModel model, ICollection<TestDefinitionCase> testCases)
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

		private static void AddQaEdgeMatchBorderingLinesCases(TestDataModel model,
		                                                      ICollection<TestDefinitionCase>
			                                                      testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("LineClass1BorderMatchCondition", "LINE.STATE_ID = BORDER.STATE_ID");
			optionalValues.Add("LineClass2BorderMatchCondition", "LINE.STATE_ID = BORDER.STATE_ID");
			optionalValues.Add("BorderingLineMatchCondition", "LINE1.STATE_ID <> LINE2.STATE_ID");
			optionalValues.Add("BorderingLineAttributeConstraint", "LINE1.TYPE =LINE2.TYPE");
			optionalValues.Add("BorderingLineEqualAttributes", "FIELD1,FIELD2:#,FIELD3");
			optionalValues.Add("BorderingLineEqualAttributeOptions",
			                   new[] { "FIELD_NAME:OPTION1", "FIELD_NAME:OPTION2" });
			optionalValues.Add("ReportIndividualAttributeConstraintViolations", false);
			optionalValues.Add("IsBorderingLineAttributeConstraintSymmetric", false);
			optionalValues.Add("AllowDisjointCandidateFeatureIfBordersAreNotCoincident", false);
			optionalValues.Add("AllowNoFeatureWithinSearchDistance", false);
			optionalValues.Add("AllowNonCoincidentEndPointsOnBorder", false);
			optionalValues.Add("AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled",
			                   false);

			testCases.Add(new TestDefinitionCase(typeof(QaEdgeMatchBorderingLines), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     2
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaEdgeMatchBorderingLines), 1,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },

				                                     model.GetVectorDataset(),
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     model.GetVectorDataset(),

				                                     2
			                                     },
			                                     optionalValues));
		}

		private static void AddQaEdgeMatchBorderingPointsCases(TestDataModel model,
		                                                       ICollection<TestDefinitionCase>
			                                                       testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("PointClass1BorderMatchCondition",
			                   "POINT.STATE_ID = BORDER.STATE_ID");
			optionalValues.Add("PointClass2BorderMatchCondition",
			                   "POINT.STATE_ID = BORDER.STATE_ID");
			optionalValues.Add("BorderingPointMatchCondition",
			                   "POINT1.STATE_ID <> POINT2.STATE_ID");
			optionalValues.Add("BorderingPointAttributeConstraint", "POINT1.TYPE = POINT2.TYPE");
			optionalValues.Add("IsBorderingPointAttributeConstraintSymmetric", false);
			optionalValues.Add("BorderingPointEqualAttributes", "FIELD1,FIELD2:#,FIELD3");
			optionalValues.Add("BorderingPointEqualAttributeOptions",
			                   new[] { "FIELD_NAME:OPTION1", "FIELD_NAME:OPTION2" });
			optionalValues.Add("ReportIndividualAttributeConstraintViolations", false);
			optionalValues.Add("CoincidenceTolerance", 1);
			optionalValues.Add("AllowDisjointCandidateFeatureIfBordersAreNotCoincident", false);
			optionalValues.Add("AllowNoFeatureWithinSearchDistance", false);
			optionalValues.Add("AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled",
			                   false);

			testCases.Add(new TestDefinitionCase(typeof(QaEdgeMatchBorderingPoints), 0,
			                                     new object[]
			                                     {
				                                     model.GetPointDataset(),
				                                     model.GetVectorDataset(),
				                                     model.GetPointDataset(),
				                                     model.GetVectorDataset(),
				                                     1
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaEdgeMatchBorderingPoints), 1,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetPointDataset(),
					                                     model.GetPointDataset()
				                                     },

				                                     model.GetVectorDataset(),
				                                     new[]
				                                     {
					                                     model.GetPointDataset(),
					                                     model.GetPointDataset()
				                                     },
				                                     model.GetVectorDataset(),

				                                     1
			                                     },
			                                     optionalValues));
		}

		private static void AddQaEdgeMatchCrossingAreasCases(TestDataModel model,
		                                                     ICollection<TestDefinitionCase>
			                                                     testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("AreaClass1BorderMatchCondition", "AREA.STATE_ID = BORDER.STATE_ID");
			optionalValues.Add("AreaClass1BoundingFeatureMatchCondition",
			                   "AREA.STATE_ID = BOUNDINGFEATURE.STATE_ID");
			optionalValues.Add("AreaClass2BoundingFeatureMatchCondition",
			                   "AREA.STATE_ID = BOUNDINGFEATURE.STATE_ID");
			optionalValues.Add("AreaClass2BorderMatchCondition", "AREA.STATE_ID = BORDER.STATE_ID");
			optionalValues.Add("CrossingAreaMatchCondition", "AREA1.STATE_ID<> AREA2.STATE_ID");
			optionalValues.Add("CrossingAreaAttributeConstraint", "AREA1.TYPE =AREA2.TYPE");
			optionalValues.Add("IsCrossingAreaAttributeConstraintSymmetric", false);
			optionalValues.Add("CrossingAreaEqualAttributes", "FIELD1,FIELD2:#,FIELD3");
			optionalValues.Add("CrossingAreaEqualAttributeOptions",
			                   new[] { "FIELD_NAME:OPTION1", "FIELD_NAME:OPTION2" });
			optionalValues.Add("ReportIndividualAttributeConstraintViolations", false);
			optionalValues.Add("AllowNoFeatureWithinSearchDistance", false);
			optionalValues.Add("AllowDisjointCandidateFeatureIfBordersAreNotCoincident", false);
			optionalValues.Add("AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled",
			                   false);

			testCases.Add(new TestDefinitionCase(typeof(QaEdgeMatchCrossingAreas), 0,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     model.GetVectorDataset(),
				                                     model.GetPolygonDataset(),
				                                     model.GetVectorDataset(),
				                                     1,
				                                     new[]
				                                     {
					                                     model.GetPolygonDataset(),
					                                     model.GetPolygonDataset()
				                                     },
				                                     new[]
				                                     {
					                                     model.GetPolygonDataset(),
					                                     model.GetPolygonDataset()
				                                     }
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaEdgeMatchCrossingAreas), 1,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetPolygonDataset(),
					                                     model.GetPolygonDataset()
				                                     },

				                                     model.GetVectorDataset(),
				                                     new[]
				                                     {
					                                     model.GetPolygonDataset(),
					                                     model.GetPolygonDataset()
				                                     },
				                                     model.GetVectorDataset(),
				                                     1,
				                                     new[]
				                                     {
					                                     model.GetPolygonDataset(),
					                                     model.GetPolygonDataset()
				                                     },
				                                     new[]
				                                     {
					                                     model.GetPolygonDataset(),
					                                     model.GetPolygonDataset()
				                                     },
			                                     },
			                                     optionalValues));
		}

		private static void AddQaEdgeMatchCrossingLinesCases(TestDataModel model,
		                                                     ICollection<TestDefinitionCase>
			                                                     testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("MinimumErrorConnectionLineLength", 0);
			optionalValues.Add("MaximumEndPointConnectionDistance", 0);
			optionalValues.Add("LineClass1BorderMatchCondition", "LINE.STATE_ID = BORDER.STATE_ID");
			optionalValues.Add("LineClass2BorderMatchCondition", "LINE.STATE_ID = BORDER.STATE_ID");
			optionalValues.Add("CrossingLineMatchCondition", "LINE1.STATE_ID <> LINE2.STATE_ID");
			optionalValues.Add("CrossingLineAttributeConstraint",
			                   "LINE1.WIDTH_CLASS = LINE2.WIDTH_CLASS");
			optionalValues.Add("IsCrossingLineAttributeConstraintSymmetric", false);
			optionalValues.Add("CrossingLineEqualAttributes", "FIELD1,FIELD2:#,FIELD3");
			optionalValues.Add("CrossingLineEqualAttributeOptions",
			                   new[] { "FIELD_NAME:OPTION1", "FIELD_NAME:OPTION2" });
			optionalValues.Add("ReportIndividualAttributeConstraintViolations", false);
			optionalValues.Add("CoincidenceTolerance", 0);
			optionalValues.Add("AllowNoFeatureWithinSearchDistance", false);
			optionalValues.Add("IgnoreAttributeConstraintsIfThreeOrMoreConnected", false);
			optionalValues.Add("AllowNoFeatureWithinSearchDistanceIfConnectedOnSameSide", false);
			optionalValues.Add("AllowDisjointCandidateFeatureIfBordersAreNotCoincident", false);
			optionalValues.Add("IgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance",
			                   false);
			optionalValues.Add("AllowEndPointsConnectingToInteriorOfValidNeighborLine", false);
			optionalValues.Add("IgnoreEndPointsOfBorderingLines", false);
			optionalValues.Add("AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled",
			                   false);

			testCases.Add(new TestDefinitionCase(typeof(QaEdgeMatchCrossingLines), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     1
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaEdgeMatchCrossingLines), 1,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },

				                                     model.GetVectorDataset(),
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     model.GetVectorDataset(),
				                                     1
			                                     },
			                                     optionalValues));
		}

		private static void AddQaExportTablesCases(
			TestDataModel model, ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("ExportTileIds", false);
			optionalValues.Add("ExportTiles", false);

			testCases.Add(new TestDefinitionCase(typeof(QaExportTables), 0,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset(),
				                                     },
				                                     "C:\\git\\Swisstopo.GoTop"
			                                     }, optionalValues));
		}

		private static void AddQaGdbTopologyCases(TestDataModel model,
		                                          List<TestDefinitionCase> testCases)
		{
			testCases.Add(new TestDefinitionCase(typeof(QaGdbTopology), 0,
			                                     new object[]
			                                     {
				                                     model.GetTopologyDataset()
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaGdbTopology), 1,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDatasetInTopology()
				                                     }
			                                     }));
		}

		private static void AddQaFullCoincidenceCases(TestDataModel model,
		                                              ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("IgnoreNeighborConditions", "FIELD_NAME:OPTION1");

			testCases.Add(new TestDefinitionCase(typeof(QaFullCoincidence), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     0,
				                                     false
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaFullCoincidence), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     0,
				                                     false,
				                                     0
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaFullCoincidence), 2,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),

				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     0,
				                                     false
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaFullCoincidence), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),

				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     0,
				                                     false,
				                                     0,
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaFullCoincidence), 4,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),

				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     0
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaFullCoincidence), 5,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),

				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     0,
				                                     0
			                                     },
			                                     optionalValues));
		}

		private static void AddQaGdbReleaseCases(
			TestDataModel model, ICollection<TestDefinitionCase> testCases)
		{
			testCases.Add(new TestDefinitionCase(typeof(QaGdbRelease), 0,
			                                     new object[]
			                                     { model.GetVectorDataset(), "10.2" }));
			testCases.Add(new TestDefinitionCase(typeof(QaGdbRelease), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "10.1", "10.2"
			                                     }));
		}

		private static void AddQaGeometryConstraintCases(
			TestDataModel model, ICollection<TestDefinitionCase> testCases)
		{
			testCases.Add(new TestDefinitionCase(typeof(QaGeometryConstraint), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "$SliverRatio < 50 OR $Area > 10", false
			                                     }));
		}

		private static void AddQaGroupConnectedCases(
			TestDataModel model, ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("ReportIndividualGaps", true);
			optionalValues.Add("IgnoreGapsLongerThan", 1.1);
			optionalValues.Add("CompleteGroupsOutsideTestArea", false);

			testCases.Add(new TestDefinitionCase(typeof(QaGroupConnected), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     new[]
				                                     {
					                                     "MY_STRING_FIELD1",
					                                     "MY_STRING_FIELD2"
				                                     },
				                                     2
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaGroupConnected), 1,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     new[]
				                                     {
					                                     "MY_STRING_FIELD1",
					                                     "MY_STRING_FIELD2"
				                                     },
				                                     ";", 2, 1, 15.3
			                                     },
			                                     optionalValues));
		}

		private static void AddQaGroupConstraintsCases(
			TestDataModel model, ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("ExistsRowGroupFilters", new[] { "String1" });

			testCases.Add(new TestDefinitionCase(typeof(QaGroupConstraints), 0,
			                                     new object[]
			                                     {
				                                     model.GetObjectDataset(),
				                                     "FIELD1 + '#' +FIELD2",
				                                     "FIELD1 + '#' +FIELD2",
				                                     1,
				                                     false
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaGroupConstraints), 1,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetObjectDataset(),
					                                     model.GetObjectDataset()
				                                     },
				                                     new[]
				                                     {
					                                     "FIELD1 + '#' +FIELD2",
					                                     "FIELD1 + '#' +FIELD2"
				                                     },
				                                     new[]
				                                     {
					                                     "FIELD1 + '#' +FIELD2",
					                                     "FIELD1 + '#' +FIELD2"
				                                     },
				                                     1,
				                                     false
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaGroupConstraints), 2,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetObjectDataset(),
					                                     model.GetObjectDataset()
				                                     },
				                                     new[]
				                                     {
					                                     "FIELD1 + '#' +FIELD2",
					                                     "FIELD1 + '#' +FIELD2"
				                                     },
				                                     new[]
				                                     {
					                                     "FIELD1 + '#' +FIELD2",
					                                     "FIELD1 + '#' +FIELD2"
				                                     },
				                                     1,
				                                     2,
				                                     false
			                                     },
			                                     optionalValues));
		}

		private static void AddQaInteriorIntersectsOtherCases(TestDataModel model,
		                                                      ICollection<TestDefinitionCase>
			                                                      testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("ValidIntersectionGeometryConstraint", "$SliverRatio < 50");

			testCases.Add(new TestDefinitionCase(typeof(QaInteriorIntersectsOther), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset()
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaInteriorIntersectsOther), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     "G1.Level <> G2.Level"
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaInteriorIntersectsOther), 2,
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
				                                     }
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaInteriorIntersectsOther), 3,
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
				                                     "G1.Level <> G2.Level"
			                                     },
			                                     optionalValues));
		}

		private static void AddQaInteriorIntersectsSelfCases(TestDataModel model,
		                                                     ICollection<TestDefinitionCase>
			                                                     testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("ValidIntersectionGeometryConstraint", "$SliverRatio < 50");

			testCases.Add(new TestDefinitionCase(typeof(QaInteriorIntersectsSelf), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaInteriorIntersectsSelf), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "G1.Level <> G2.Level"
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaInteriorIntersectsSelf), 2,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaInteriorIntersectsSelf), 3,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     "G1.Level <> G2.Level"
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaInteriorIntersectsSelf), 4,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     "G1.Level <> G2.Level",
				                                     false
			                                     },
			                                     optionalValues));
		}

		private static void AddQaInteriorRingsCases(TestDataModel model,
		                                            ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("IgnoreInnerRingsLargerThan", 1);
			optionalValues.Add("ReportIndividualRings", false);
			optionalValues.Add("ReportOnlySmallestRingsExceedingMaximumCount", false);

			testCases.Add(new TestDefinitionCase(typeof(QaInteriorRings), 0,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     1
			                                     },
			                                     optionalValues));
		}

		private static void AddQaIntersectionMatrixOtherCases(TestDataModel model,
		                                                      ICollection<TestDefinitionCase>
			                                                      testCases)
		{
			testCases.Add(new TestDefinitionCase(typeof(QaIntersectionMatrixOther), 0,
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
				                                     "TFFFTFTFF"
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaIntersectionMatrixOther), 1,
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
				                                     "FFFTFFFTF",
				                                     "G1.Level <> G2.Level"
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaIntersectionMatrixOther), 2,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     "FFFTFFFTF"
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaIntersectionMatrixOther), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     "FFFTFFFTF",
				                                     "G1.Level <> G2.Level"
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaIntersectionMatrixOther), 4,
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
				                                     "FFFTFFFTF",
				                                     "G1.Level <> G2.Level",
				                                     "0: point intersections"
			                                     }));
		}

		private static void AddQaIntersectionMatrixSelfCases(TestDataModel model,
		                                                     ICollection<TestDefinitionCase>
			                                                     testCases)
		{
			testCases.Add(new TestDefinitionCase(typeof(QaIntersectionMatrixSelf), 0,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     "TFFFTFTFF"
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaIntersectionMatrixSelf), 1,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     "FFFTFFFTF",
				                                     "G1.Level <> G2.Level"
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaIntersectionMatrixSelf), 2,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "FFFTFFFTF"
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaIntersectionMatrixSelf), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "FFFTFFFTF",
				                                     "G1.Level <> G2.Level"
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaIntersectionMatrixSelf), 4,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     "FFFTFFFTF",
				                                     "G1.Level <> G2.Level",
				                                     "0: point intersections"
			                                     }));
		}

		private static void AddQaIntersectsOtherCases(TestDataModel model,
		                                              ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("ReportIntersectionsAsMultipart", false);
			optionalValues.Add("ValidIntersectionGeometryConstraint", "$SliverRatio < 50");

			testCases.Add(new TestDefinitionCase(typeof(QaIntersectsOther), 0,
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
				                                     }
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaIntersectsOther), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset()
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaIntersectsOther), 2,
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
				                                     "G1.Level <> G2.Level"
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaIntersectsOther), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     "G1.Level <> G2.Level"
			                                     },
			                                     optionalValues));
		}

		private static void AddQaIntersectsSelfCases(TestDataModel model,
		                                             ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("ReportIntersectionsAsMultipart", false);
			optionalValues.Add("ValidIntersectionGeometryConstraint", "$SliverRatio < 50");
			optionalValues.Add("GeometryComponents", "");

			testCases.Add(new TestDefinitionCase(typeof(QaIntersectsSelf), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaIntersectsSelf), 1,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     }
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaIntersectsSelf), 2,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     "G1.Level <> G2.Level"
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaIntersectsSelf), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "G1.Level <> G2.Level"
			                                     },
			                                     optionalValues));
		}

		private static void AddQaIsCoveredByOtherCases(TestDataModel model,
		                                               ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>
			                     {
				                     { "CoveringClassTolerances", new[] { 1 } },
				                     {
					                     "ValidUncoveredGeometryConstraint",
					                     "$Area < 5 AND $VertexCount < 10"
				                     }
			                     };

			var polygonDataset1 = model.GetPolygonDataset();
			var polygonDataset2 = model.GetPolygonDataset();

			testCases.Add(new TestDefinitionCase(typeof(QaIsCoveredByOther), 0,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     polygonDataset1, polygonDataset2
				                                     }, // Covering
				                                     new[]
				                                     {
					                                     polygonDataset1, polygonDataset2
				                                     } // Covered
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaIsCoveredByOther), 1,
			                                     new object[]
			                                     {
				                                     polygonDataset1, // Covering
				                                     polygonDataset2 // Covered
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaIsCoveredByOther), 2,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     polygonDataset1, polygonDataset2
				                                     }, // Covering
				                                     new[]
				                                     {
					                                     polygonDataset1, polygonDataset2
				                                     }, // Covered
				                                     "G1" // isCoveringCondition
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaIsCoveredByOther), 3,
			                                     new object[]
			                                     {
				                                     polygonDataset1, // Covering
				                                     polygonDataset2, // Covered
				                                     "G1" // isCoveringCondition
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaIsCoveredByOther), 4,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     polygonDataset1, polygonDataset2
				                                     }, // Covering
				                                     new GeometryComponent[]
				                                     {
					                                     GeometryComponent.EntireGeometry
				                                     }, // CoveringGeometryComponents
				                                     new[]
				                                     {
					                                     polygonDataset1, polygonDataset2
				                                     }, // Covered
				                                     new GeometryComponent[]
				                                     {
					                                     GeometryComponent.EntireGeometry
				                                     }, // CoveredGeometryComponents
				                                     "G1" // Single isCoveringCondition
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaIsCoveredByOther), 5,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     polygonDataset1, polygonDataset2
				                                     }, // Covering
				                                     new GeometryComponent[]
				                                     {
					                                     GeometryComponent.EntireGeometry
				                                     }, // CoveringGeometryComponents
				                                     new[]
				                                     {
					                                     polygonDataset1, polygonDataset2
				                                     }, // Covered
				                                     new GeometryComponent[]
				                                     {
					                                     GeometryComponent.EntireGeometry
				                                     }, // CoveredGeometryComponents
				                                     "G1", // isCoveringCondition
				                                     10.0 // AllowedUncoveredPercentage
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaIsCoveredByOther), 6,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     polygonDataset1, polygonDataset2
				                                     }, // Covering
				                                     new GeometryComponent[]
				                                     {
					                                     GeometryComponent.EntireGeometry
				                                     }, // CoveringGeometryComponents
				                                     new[]
				                                     {
					                                     polygonDataset1, polygonDataset2
				                                     }, // Covered
				                                     new GeometryComponent[]
				                                     {
					                                     GeometryComponent.EntireGeometry
				                                     }, // CoveredGeometryComponents
				                                     new[] { "G1" }, // isCoveringCondition
				                                     0.45 // AllowedUncoveredPercentage
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaIsCoveredByOther), 7,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     polygonDataset1, polygonDataset2
				                                     }, // Covering
				                                     new GeometryComponent[]
				                                     {
					                                     GeometryComponent.EntireGeometry
				                                     }, // CoveringGeometryComponents
				                                     new[]
				                                     {
					                                     polygonDataset1, polygonDataset2
				                                     }, // Covered
				                                     new GeometryComponent[]
				                                     {
					                                     GeometryComponent.EntireGeometry
				                                     }, // CoveredGeometryComponents
				                                     new[] { "G1" }, // isCoveringCondition
				                                     0.45, // AllowedUncoveredPercentage
				                                     new[]
				                                     {
					                                     polygonDataset1, polygonDataset2
				                                     } // AreaOfInterestClasses
			                                     },
			                                     optionalValues));
		}

		private static void AddQaLineGroupConstraintsCases(TestDataModel model,
		                                                   ICollection<TestDefinitionCase>
			                                                   testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("ValueSeparator", "E100#E200");
			optionalValues.Add("GroupConditions", "GroupConditions");
			optionalValues.Add("MinGapToOtherGroupType", 0);
			optionalValues.Add("MinDangleLengthContinued", 0);
			optionalValues.Add("MinDangleLengthAtForkContinued", 0);
			optionalValues.Add("MinDangleLengthAtFork", 0);
			optionalValues.Add("MinGapToSameGroupTypeCovered", 0);
			optionalValues.Add("MinGapToSameGroupTypeAtFork", 0);
			optionalValues.Add("MinGapToSameGroupTypeAtForkCovered", 0);
			optionalValues.Add("MinGapToOtherGroupTypeAtFork", 0);
			optionalValues.Add("MinGapToSameGroup", 0);

			testCases.Add(new TestDefinitionCase(typeof(QaLineGroupConstraints), 0,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     0,
				                                     0,
				                                     0,
				                                     new[]
				                                     {
					                                     "String1",
					                                     "String2"
				                                     }
			                                     },
			                                     optionalValues));
		}

		private static void AddQaLineIntersectAngleCases(TestDataModel model,
		                                                 ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("AngularUnit", AngleUnit.Radiant);

			testCases.Add(new TestDefinitionCase(typeof(QaLineIntersectAngle), 0,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     0,
				                                     false
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaLineIntersectAngle), 2,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaLineIntersectAngle), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     1
			                                     },
			                                     optionalValues));
		}

		private static void AddQaLineIntersectCases(TestDataModel model,
		                                            ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("AllowedInteriorIntersections", "None");

			testCases.Add(new TestDefinitionCase(typeof(QaLineIntersect), 0,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     }
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaLineIntersect), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset()
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaLineIntersect), 2,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     "G1.Level <> G2.Level"
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaLineIntersect), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "G1.Level <> G2.Level"
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaLineIntersect), 4,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     "G1.Level <> G2.Level",
				                                     "None",
				                                     false
			                                     },
			                                     optionalValues));
		}

		private static void AddQaLineIntersectZCases(TestDataModel model,
		                                             ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("MinimumZDifferenceExpression", "U.ZDiff + L.ZDiff");
			optionalValues.Add("MaximumZDifferenceExpression", "U.ZDiff + L.ZDiff");

			testCases.Add(new TestDefinitionCase(typeof(QaLineIntersectZ), 0,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaLineIntersectZ), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     1
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaLineIntersectZ), 2,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     1,
				                                     "U.EdgeLevel > L.EdgeLevel"
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaLineIntersectZ), 3,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1,
				                                     "U.EdgeLevel > L.EdgeLevel"
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaLineIntersectZ), 4,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1,
				                                     2,
				                                     "U.EdgeLevel > L.EdgeLevel"
			                                     },
			                                     optionalValues));
		}

		private static void AddQaMaxSlopeCases(TestDataModel model,
		                                       ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("AngularUnit", AngleUnit.Radiant);

			testCases.Add(new TestDefinitionCase(typeof(QaMaxSlope), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     1
			                                     },
			                                     optionalValues));
		}

		private static void AddQaMinAngleCases(TestDataModel model,
		                                       ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("AngularUnit", AngleUnit.Radiant);

			testCases.Add(new TestDefinitionCase(typeof(QaMinAngle), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     1,
				                                     false
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaMinAngle), 1,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1,
				                                     false
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaMinAngle), 2,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1
			                                     },
			                                     optionalValues));
		}

		private static void AddQaMinSegAngleCases(TestDataModel model,
		                                          ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("UseTangents", false);
			optionalValues.Add("AngularUnit", AngleUnit.Radiant);

			testCases.Add(new TestDefinitionCase(typeof(QaMinSegAngle), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     1,
				                                     false
			                                     },
			                                     optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaMinSegAngle), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     1
			                                     },
			                                     optionalValues));
		}

		private static void AddQaMonotonicZCases(TestDataModel model,
		                                         ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("AllowConstantValues", false);
			optionalValues.Add("ExpectedMonotonicity", MonotonicityDirection.Any);
			optionalValues.Add("FlipExpression", "String");

			testCases.Add(new TestDefinitionCase(typeof(QaMonotonicZ), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
			                                     },
			                                     optionalValues));
		}

		private static void AddQaMpFootprintHolesCases(TestDataModel model,
		                                               ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("HorizontalZTolerance", 1);
			optionalValues.Add("ResolutionFactor", 1.1);
			optionalValues.Add("MinimumArea", 123);
			optionalValues.Add("ReportVerticalPatchesNotCompletelyWithinFootprint", false);

			testCases.Add(new TestDefinitionCase(typeof(QaMpFootprintHoles), 0,
			                                     new object[]
			                                     {
				                                     model.GetMultipatchDataset(),
				                                     InnerRingHandling.None
			                                     },
			                                     optionalValues));
		}

		private static void AddQaMpNonIntersectingRingFootprintsCases(TestDataModel model,
			ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("ResolutionFactor", 1);

			testCases.Add(new TestDefinitionCase(typeof(QaMpNonIntersectingRingFootprints), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     false
			                                     },
			                                     optionalValues));
		}

		private static void AddQaMpSinglePartFootprintCases(TestDataModel model,
		                                                    ICollection<TestDefinitionCase>
			                                                    testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("ResolutionFactor", 1);

			testCases.Add(new TestDefinitionCase(typeof(QaMpSinglePartFootprint), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset()
			                                     },
			                                     optionalValues));
		}

		private static void AddQaMpVertexNotNearFaceCases(TestDataModel model,
		                                                  ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("CoplanarityTolerance", 1);
			optionalValues.Add("ReportNonCoplanarity", false);
			optionalValues.Add("IgnoreNonCoplanarFaces", false);
			optionalValues.Add("VerifyWithinFeature", false);
			optionalValues.Add("PointCoincidence", 1);
			optionalValues.Add("EdgeCoincidence", 1);
			optionalValues.Add("PlaneCoincidence", 1);
			optionalValues.Add("MinimumSlopeDegrees", 1);

			testCases.Add(new TestDefinitionCase(typeof(QaMpVertexNotNearFace), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1,
				                                     1
			                                     },
			                                     optionalValues));
		}

		private static void AddQaMustBeNearOtherCases(TestDataModel model,
		                                              ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("ErrorDistanceFormat", "{0:N2} m");

			testCases.Add(new TestDefinitionCase(typeof(QaMustBeNearOther), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1,
				                                     "G1"
			                                     },
			                                     optionalValues));
		}

		private static void AddQaMustIntersectMatrixOtherCases(TestDataModel model,
		                                                       ICollection<TestDefinitionCase>
			                                                       testCases)
		{
			testCases.Add(new TestDefinitionCase(typeof(QaMustIntersectMatrixOther), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     "****T****",
				                                     "G1"
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaMustIntersectMatrixOther), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     "****T****",
				                                     "G1",
				                                     "1",
				                                     "1"
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaMustIntersectMatrixOther), 2,
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
				                                     "****T****",
				                                     "G1"
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaMustIntersectMatrixOther), 3,
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
				                                     "****T****",
				                                     "G1",
				                                     "1",
				                                     "1"
			                                     }));
		}

		private static void AddQaNeighbourAreasCases(TestDataModel model,
		                                             ICollection<TestDefinitionCase> testCases)
		{
			testCases.Add(new TestDefinitionCase(typeof(QaNeighbourAreas), 0,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     "L.ObjektArt <> R.ObjektArt"
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaNeighbourAreas), 1,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     "L.ObjektArt <> R.ObjektArt", true
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaNeighbourAreas), 2,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     true
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaNeighbourAreas), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     true, "MY_STRING_FIELD1, MY_STRING_FIELD2", 1
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaNeighbourAreas), 4,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     true,
				                                     new[]
				                                     {
					                                     "MY_STRING_FIELD1",
					                                     "MY_STRING_FIELD2"
				                                     },
				                                     0
			                                     }));
		}

		private static void AddQaNoBoundaryLoopsCases(TestDataModel model,
		                                              ICollection<TestDefinitionCase> testCases)
		{
			testCases.Add(new TestDefinitionCase(typeof(QaNoBoundaryLoops), 0,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaNoBoundaryLoops), 1,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     0
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaNoBoundaryLoops), 2,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     0, 0, 8.3
			                                     }));
		}

		private static void AddQaNodeLineCoincidenceCases(TestDataModel model,
		                                                  ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("CoincidenceTolerance", 1);

			testCases.Add(new TestDefinitionCase(typeof(QaNodeLineCoincidence), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaNodeLineCoincidence), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1,
				                                     false
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaNodeLineCoincidence), 2,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1,
				                                     false,
				                                     false
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaNodeLineCoincidence), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     new[]
				                                     {
					                                     1, 1
				                                     },
				                                     1,
				                                     false,
				                                     false
			                                     }, optionalValues));
		}

		private static void AddQaNoGapsCases(TestDataModel model,
		                                     ICollection<TestDefinitionCase> testCases)
		{
			testCases.Add(new TestDefinitionCase(typeof(QaNoGaps), 0,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     1,
				                                     1
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaNoGaps), 1,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetPolygonDataset(),
					                                     model.GetPolygonDataset()
				                                     },
				                                     1,
				                                     1
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaNoGaps), 2,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     1,
				                                     1,
				                                     1,
				                                     false
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaNoGaps), 3,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetPolygonDataset(),
					                                     model.GetPolygonDataset()
				                                     },
				                                     1,
				                                     1,
				                                     1,
				                                     false
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaNoGaps), 4,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetPolygonDataset(),
					                                     model.GetPolygonDataset()
				                                     },
				                                     1,
				                                     1,
				                                     new[]
				                                     {
					                                     model.GetPolygonDataset(),
					                                     model.GetPolygonDataset()
				                                     },
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaNoGaps), 5,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetPolygonDataset(),
					                                     model.GetPolygonDataset()
				                                     },
				                                     1,
				                                     1,
				                                     1,
				                                     false,
				                                     new[]
				                                     {
					                                     model.GetPolygonDataset(),
					                                     model.GetPolygonDataset()
				                                     },
			                                     }));
		}

		private static void AddQaNotNearCases(TestDataModel model,
		                                      ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("IgnoreNeighborCondition", "G1.CountryCode <> G2.CountryCode");

			testCases.Add(new TestDefinitionCase(typeof(QaNotNear), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     1.1, 2.2, true
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaNotNear), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     1.1, 2.2, true, 3.3
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaNotNear), 2,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     1.1, 2.2, true,
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaNotNear), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     1.1, 2.2, true, 3.3
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaNotNear), 4,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     1.1, 2.2
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaNotNear), 5,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     1.1, 2.2, 3.3
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaNotNear), 6,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     1.1, 2.2
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaNotNear), 7,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     1.1, 2.2, 3.3
			                                     }, optionalValues));
		}

		private static void AddQaPartCoincidenceOtherCases(TestDataModel model,
		                                                   ICollection<TestDefinitionCase>
			                                                   testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("IgnoreNeighborCondition", "G1.CountryCode <> G2.CountryCode");

			testCases.Add(new TestDefinitionCase(typeof(QaPartCoincidenceOther), 0,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     model.GetVectorDataset(),
				                                     1.1, 2.2, true
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaPartCoincidenceOther), 1,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     model.GetVectorDataset(),
				                                     1.1, 2.2, true, 200000.0
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaPartCoincidenceOther), 2,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     model.GetVectorDataset(),
				                                     1.1, 2.2
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaPartCoincidenceOther), 3,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     model.GetVectorDataset(),
				                                     1.1, 2.2, 200000.0
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaPartCoincidenceOther), 4,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     model.GetVectorDataset(),
				                                     1.1, 2.2, 3.3, true, 200000.0, 0
			                                     }, optionalValues));
		}

		private static void AddQaPartCoincidenceSelfCases(TestDataModel model,
		                                                  ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("IgnoreNeighborConditions",
			                   new[] { "G1.CountryCode <> G2.CountryCode" });

			testCases.Add(new TestDefinitionCase(typeof(QaPartCoincidenceSelf), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     1.1, 2.2, true
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaPartCoincidenceSelf), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     1.1, 2.2, true, 200000.0
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaPartCoincidenceSelf), 2,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1.1, 2.2, true
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaPartCoincidenceSelf), 3,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1.1, 2.2, true, 200000.0
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaPartCoincidenceSelf), 4,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1.1, 2.2
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaPartCoincidenceSelf), 5,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1.1, 2.2, 200000.0
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaPartCoincidenceSelf), 6,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1.1, 2.2, 3.3, true, 200000.0, 0
			                                     }, optionalValues));
		}

		private static void AddQaPointNotNearCases(TestDataModel model,
		                                           ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("AllowCoincidentPoints", true);
			optionalValues.Add("GeometryComponents", 0);
			optionalValues.Add("ValidRelationConstraints", "G1.Level<> G2.Level");
			optionalValues.Add("MinimumErrorLineLength", 1.1);

			testCases.Add(new TestDefinitionCase(typeof(QaPointNotNear), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     1.1
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaPointNotNear), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1.1
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaPointNotNear), 2,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1.1, "pDE_SQL_Expression",
				                                     new[]
				                                     {
					                                     "rDE_SQL_Expression_1",
					                                     "rDE_SQL_Expression_2"
				                                     }
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaPointNotNear), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     1.1, "pDE_SQL_Expression",
				                                     new[]
				                                     {
					                                     "rDE_SQL_Expression_1",
					                                     "rDE_SQL_Expression_2"
				                                     },
				                                     new[]
				                                     {
					                                     "rRSD_SQL_Expression_1",
					                                     "rRSD_SQL_Expression_2"
				                                     },
				                                     new[]
				                                     {
					                                     "rFE_SQL_Expression_1",
					                                     "rFE_SQL_Expression_2"
				                                     },
			                                     }, optionalValues));
		}

		private static void AddQaPseudoNodesCases(TestDataModel model,
		                                          ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("IgnoreLoopEndpoints", true);

			//NOTE: Constructor 0 is internally used and can not be configured properly.

			testCases.Add(new TestDefinitionCase(typeof(QaPseudoNodes), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     new[]
				                                     {
					                                     "Ignore_Fields_String_1",
					                                     "Ignore_Fields_String_2"
				                                     },
				                                     model.GetVectorDataset()
			                                     }, optionalValues));

			//NOTE: Constructor 2 is internally used and can not be configured properly.

			testCases.Add(new TestDefinitionCase(typeof(QaPseudoNodes), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     new[]
				                                     {
					                                     "Ignore_Fields_String_1",
					                                     "Ignore_Fields_String_2"
				                                     }
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaPseudoNodes), 4,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     new[]
				                                     {
					                                     "Ignore_Fields_String_1",
					                                     "Ignore_Fields_String_2"
				                                     },
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     }
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaPseudoNodes), 5,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     new[]
				                                     {
					                                     "Ignore_Fields_String_1",
					                                     "Ignore_Fields_String_2"
				                                     }
			                                     }, optionalValues));
		}

		private static void AddQaRegularExpressionCases(TestDataModel model,
		                                                ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("FieldListType", 1);

			testCases.Add(new TestDefinitionCase(typeof(QaRegularExpression), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "PATTERN_STRING", "MY_STRING_FIELD1"
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaRegularExpression), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "Pattern_String",
				                                     new[]
				                                     {
					                                     "MY_STRING_FIELD1",
					                                     "MY_STRING_FIELD2"
				                                     }
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaRegularExpression), 2,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "PATTERN_STRING", "MY_STRING_FIELD1", false
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaRegularExpression), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "PATTERN_STRING",
				                                     new[]
				                                     {
					                                     "MY_STRING_FIELD1",
					                                     "MY_STRING_FIELD2"
				                                     },
				                                     false
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaRegularExpression), 4,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "PATTERN_STRING", "MY_STRING_FIELD1", false,
				                                     "PATTERN_DESC_STRING"
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaRegularExpression), 5,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "PATTERN_STRING",
				                                     new[]
				                                     {
					                                     "MY_STRING_FIELD1",
					                                     "MY_STRING_FIELD2"
				                                     },
				                                     false, "PATTERN_DESC_STRING"
			                                     }, optionalValues));
		}

		private static void AddQaRowCountCases(TestDataModel model,
		                                       ICollection<TestDefinitionCase>
			                                       testCases)
		{
			testCases.Add(new TestDefinitionCase(typeof(QaRowCount), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(), 10, 1000
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaRowCount), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     new[]
				                                     {
					                                     model.GetPointDataset(),
					                                     model.GetPolygonDataset()
				                                     },
				                                     "200", "-100"
			                                     }));
		}

		private static void AddQaSchemaFieldDomainDescriptionsCases(TestDataModel model,
			ICollection<TestDefinitionCase>
				testCases)
		{
			testCases.Add(new TestDefinitionCase(typeof(QaSchemaFieldDomainDescriptions), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(), 1, false,
				                                     model.GetPolygonDataset()
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaSchemaFieldDomainDescriptions), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(), 1, true
			                                     }));
		}

		private static void AddQaSchemaSpatialReferenceCases(TestDataModel model,
		                                                     ICollection<TestDefinitionCase>
			                                                     testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("CompareXYDomainOrigin", false);
			optionalValues.Add("CompareZDomainOrigin", true);
			optionalValues.Add("CompareMDomainOrigin", false);
			optionalValues.Add("CompareXYResolution", true);
			optionalValues.Add("CompareZResolution", false);
			optionalValues.Add("CompareMResolution", true);

			testCases.Add(new TestDefinitionCase(typeof(QaSchemaSpatialReference), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     true, false, true, false, true
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaSchemaSpatialReference), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "<ProjectedCoordinateSystem xsi:type='typens:ProjectedCoordinateSystem' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.8'><WKT>PROJCS[&quot;CH1903+_LV95&quot;,GEOGCS[&quot;GCS_CH1903+&quot;,DATUM[&quot;D_CH1903+&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,2600000.0],PARAMETER[&quot;False_Northing&quot;,1200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,2056]]</WKT><XOrigin>-27386400</XOrigin><YOrigin>-32067900</YOrigin><XYScale>10000</XYScale><ZOrigin>-100000</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>2056</WKID><LatestWKID>2056</LatestWKID></ProjectedCoordinateSystem>\r\n",
				                                     false, true, false, true, false
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaSchemaSpatialReference), 2,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     true, false, false
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaSchemaSpatialReference), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "<ProjectedCoordinateSystem xsi:type='typens:ProjectedCoordinateSystem' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.8'><WKT>PROJCS[&quot;CH1903+_LV95&quot;,GEOGCS[&quot;GCS_CH1903+&quot;,DATUM[&quot;D_CH1903+&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,2600000.0],PARAMETER[&quot;False_Northing&quot;,1200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,2056]]</WKT><XOrigin>-27386400</XOrigin><YOrigin>-32067900</YOrigin><XYScale>10000</XYScale><ZOrigin>-100000</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>2056</WKID><LatestWKID>2056</LatestWKID></ProjectedCoordinateSystem>\r\n",
				                                     false, true, true
			                                     }, optionalValues));
		}

		private static void AddQaSliverPolygonCases(TestDataModel model,
		                                            ICollection<TestDefinitionCase> testCases)
		{
			testCases.Add(new TestDefinitionCase(typeof(QaSliverPolygon), 0,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     50
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaSliverPolygon), 1,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     50, 10
			                                     }));
		}

		private static void AddQaSmoothCases(TestDataModel model,
		                                     ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("AngularUnit", AngleUnit.Radiant);

			testCases.Add(new TestDefinitionCase(typeof(QaSmooth), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     0.5
			                                     }, optionalValues));
		}

		private static void AddQaTopoNotNearCases(TestDataModel model,
		                                         List<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("CrossingMinLengthFactor", 0.3);
			optionalValues.Add("NotReportedCondition", "1 = 1");
			optionalValues.Add("IgnoreNeighborCondition", "2 = 2");
			optionalValues.Add("JunctionCoincidenceTolerance", 0.333);
			optionalValues.Add("ConnectionMode", ConnectionMode.EndpointOnVertex);
			optionalValues.Add("UnconnectedLineCapStyle", LineCapStyle.Butt);
			optionalValues.Add("IgnoreLoopsWithinNearDistance", true);
			optionalValues.Add("IgnoreInconsistentLineSymbolEnds", true);
			optionalValues.Add("AllowCoincidentSections", true);
			optionalValues.Add("RightSideNears", new List<string> {"one"});
			optionalValues.Add("EndCapStyle", LineCapStyle.Butt);
			optionalValues.Add("JunctionIsEndExpression", "true");

			testCases.Add(new TestDefinitionCase(typeof(QaTopoNotNear), 0,
												 new object[]
												 {
													 model.GetVectorDataset(),
													 1.1,
													 5.2,
													 true
												 }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaTopoNotNear), 1,
												 new object[]
												 {
													 model.GetVectorDataset(),
													 1.1,
													 5.2,
													 true,
													 1234.5
												 }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaTopoNotNear), 2,
												 new object[]
												 {
													 model.GetVectorDataset(),
													 model.GetPolygonDataset(),
													 1.1,
													 5.2,
													 true
												 }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaTopoNotNear), 3,
												 new object[]
												 {
													 model.GetVectorDataset(),
													 model.GetPolygonDataset(),
													 1.1,
													 5.2,
													 true,
													 1234.5
												 }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaTopoNotNear), 4,
												 new object[]
												 {
													 model.GetVectorDataset(),
													 1.1,
													 5.2
												 }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaTopoNotNear), 5,
												 new object[]
												 {
													 model.GetVectorDataset(),
													 1.1,
													 5.2,
													 1234.5
												 }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaTopoNotNear), 6,
												 new object[]
												 {
													 model.GetVectorDataset(),
													 model.GetPolygonDataset(),
													 1.1,
													 5.2
												 }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaTopoNotNear), 7,
												 new object[]
												 {
													 model.GetVectorDataset(),
													 model.GetPolygonDataset(),
													 1.1,
													 5.2,
													 1234.5
												 }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaTopoNotNear), 8,
												 new object[]
												 {
													 model.GetVectorDataset(),
													 1.1,
													 "SHAPE_LEN",
													 3.0,
													 6.6,
													 true
												 }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaTopoNotNear), 9,
												 new object[]
												 {
													 model.GetVectorDataset(),
													 model.GetPolygonDataset(),
													 1.1,
													 3.0,
													 6.6,
													 true
												 }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaTopoNotNear), 10,
												 new object[]
												 {
													 model.GetVectorDataset(),
													 model.GetPolygonDataset(),
													 1.1,
													 "fc1",
													 "fc2",
													 3.0,
													 6.6,
													 true
												 }, optionalValues));

		}

		private static void AddQaTouchesOtherCases(TestDataModel model,
		                                           ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("ValidTouchGeometryConstraint", "$Length>10");

			testCases.Add(new TestDefinitionCase(typeof(QaTouchesOther), 0,
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
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaTouchesOther), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaTouchesOther), 2,
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
				                                     "G1.Level <> G2.Level"
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaTouchesOther), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
				                                     "G1.Level <> G2.Level"
			                                     }, optionalValues));
		}

		private static void AddQaTouchesSelfCases(TestDataModel model,
		                                          ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("ValidTouchGeometryConstraint", "$Length>10");

			testCases.Add(new TestDefinitionCase(typeof(QaTouchesSelf), 0,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaTouchesSelf), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaTouchesSelf), 2,
			                                     new object[]
			                                     {
				                                     new[]
				                                     {
					                                     model.GetVectorDataset(),
					                                     model.GetVectorDataset()
				                                     },
				                                     "G1.Level <> G2.Level"
			                                     }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaTouchesSelf), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "G1.Level <> G2.Level"
			                                     }, optionalValues));
		}

		private static void AddQaTrimmedTextFieldsCases(TestDataModel model,
		                                                ICollection<TestDefinitionCase> testCases)
		{
			testCases.Add(new TestDefinitionCase(typeof(QaTrimmedTextFields), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset()
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaTrimmedTextFields), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     9
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaTrimmedTextFields), 2,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "MY_STRING_FIELD1"
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaTrimmedTextFields), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     9,
				                                     "MY_STRING_FIELD1"
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaTrimmedTextFields), 4,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     9,
				                                     new[]
				                                     {
					                                     "MY_STRING_FIELD1",
					                                     "MY_STRING_FIELD2"
				                                     }
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaTrimmedTextFields), 5,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     9,
				                                     "MY_STRING_FIELD1",
				                                     0
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaTrimmedTextFields), 6,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     9,
				                                     new[]
				                                     {
					                                     "MY_STRING_FIELD1",
					                                     "MY_STRING_FIELD2"
				                                     },
				                                     1
			                                     }));
		}

		private static void AddQaValidCoordinateFieldsCases(TestDataModel model,
		                                                    ICollection<TestDefinitionCase>
			                                                    testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("AllowXYFieldValuesForUndefinedShape", false);
			optionalValues.Add("AllowZFieldValueForUndefinedShape", false);
			optionalValues.Add("AllowMissingZFieldValueForDefinedShape", false);
			optionalValues.Add("AllowMissingXYFieldValueForDefinedShape", false);

			testCases.Add(new TestDefinitionCase(typeof(QaValidCoordinateFields), 0,
			                                     new object[]
			                                     {
				                                     model.GetPointDataset(),
				                                     "XCoordinate", "YCoordinate", "ZCoordinate", 1,
				                                     1, "de-CH"
			                                     }, optionalValues));
		}

		private static void AddQaValidDateValuesCases(TestDataModel model,
		                                              ICollection<TestDefinitionCase> testCases)
		{
			testCases.Add(new TestDefinitionCase(typeof(QaValidDateValues), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "06.02.2006", "27.08.2007"
			                                     }));

			testCases.Add(new TestDefinitionCase(typeof(QaValidDateValues), 1,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "06.02.2006", "27.08.2007",
				                                     new[] { "MY_DATE_FIELD1", "MY_DATE_FIELD2" }
			                                     }));

			testCases.Add(new TestDefinitionCase(typeof(QaValidDateValues), 2,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "06.02.2006", "27.08.2007",
				                                     "MY_DATE_FIELD1, MY_DATE_FIELD2"
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaValidDateValues), 3,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "28.04.2013", 0
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaValidDateValues), 4,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "06.02.2006", 0,
				                                     "MY_DATE_FIELD1, MY_DATE_FIELD2"
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaValidDateValues), 5,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     0, "27.08.2007",
				                                     "MY_DATE_FIELD1, MY_DATE_FIELD2"
			                                     }));
			testCases.Add(new TestDefinitionCase(typeof(QaValidDateValues), 6,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     0, 0,
				                                     "MY_DATE_FIELD1, MY_DATE_FIELD2"
			                                     }));
		}

		private static void AddQaValidUrlsCases(TestDataModel model,
		                                        ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("MaximumParallelTasks", 1);

			testCases.Add(new TestDefinitionCase(typeof(QaValidUrls), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     "MY_STRING_FIELD1"
			                                     }, optionalValues));
		}

		private static void AddQaVertexCoincidenceCases(TestDataModel model,
		                                                ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("PointTolerance", -1);
			optionalValues.Add("EdgeTolerance", -1);
			optionalValues.Add("RequireVertexOnNearbyEdge", true);
			optionalValues.Add("CoincidenceTolerance", 0);
			optionalValues.Add("Is3D", false);
			optionalValues.Add("ZTolerance", 0);
			optionalValues.Add("ZCoincidenceTolerance", 0);
			optionalValues.Add("ReportCoordinates", true);

			testCases.Add(new TestDefinitionCase(typeof(QaVertexCoincidence), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
			                                     },
			                                     optionalValues));
		}

		private static void AddQaVertexCoincidenceOtherCases(TestDataModel model,
		                                                     ICollection<TestDefinitionCase>
			                                                     testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("PointTolerance", -1);
			optionalValues.Add("EdgeTolerance", -1);
			optionalValues.Add("RequireVertexOnNearbyEdge", true);
			optionalValues.Add("CoincidenceTolerance", 0);
			optionalValues.Add("ZTolerance", 0);
			optionalValues.Add("ZCoincidenceTolerance", 0);
			optionalValues.Add("Is3D", false);
			optionalValues.Add("Bidirectional", true);
			optionalValues.Add("ReportCoordinates", true);

			testCases.Add(new TestDefinitionCase(typeof(QaVertexCoincidenceOther), 0,
			                                     new object[]
			                                     {
				                                     model.GetVectorDataset(),
				                                     model.GetVectorDataset(),
			                                     },
			                                     optionalValues));

			testCases.Add(new TestDefinitionCase(typeof(QaVertexCoincidenceOther), 1,
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
			                                     },
			                                     optionalValues));

			testCases.Add(new TestDefinitionCase(typeof(QaVertexCoincidenceOther), 2,
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
				                                     "G1.CountryCode <> G2.CountryCode"
			                                     },
			                                     optionalValues));
		}

		private static void AddQaVertexCoincidenceSelfCases(TestDataModel model,
		                                                    ICollection<TestDefinitionCase>
			                                                    testCases)
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

		private static void AddQaWithinBoxCases(TestDataModel model,
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

		private static void AddQaZDifferenceOtherCases(TestDataModel model,
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

		private static void AddQaZDifferenceSelfCases(TestDataModel model,
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

		#endregion

		private static List<Type> IfRefactoredTypes
		{
			get
			{
				List<Type> ifRefactoredTypes = new List<Type>
				                               {
					                               typeof(IfAll),
					                               typeof(IfIntersecting),
					                               typeof(IfInvolvedRows),
					                               typeof(IfNear),
					                               typeof(IfWithin)
				                               };
				return ifRefactoredTypes;
			}
		}

		[Test]
		public void CanCreateIssueFilters()
		{
			foreach (Type issueFilterType in IfRefactoredTypes)
			{
				Assert.IsFalse(InstanceUtils.HasInternallyUsedAttribute(issueFilterType),
				               "Internally used issue filter");

				// One is used internally to create using the definition.
				int constructorCount = issueFilterType.GetConstructors().Length - 1;

				bool lastConstructorIsInternallyUsed =
					InstanceUtils.IsInternallyUsed(issueFilterType, constructorCount);

				Assert.IsTrue(lastConstructorIsInternallyUsed,
				              $"Last constructor not internally used in {issueFilterType.Name}");

				for (int constructorIdx = 0;
				     constructorIdx < constructorCount;
				     constructorIdx++)
				{
					Console.WriteLine("Checking {0}({1})", issueFilterType.Name, constructorIdx);

					if (! InstanceUtils.IsObsolete(issueFilterType, constructorIdx))
					{
						CompareIssueFilterMetadata(issueFilterType, constructorIdx, true);
					}

					IssueFilterDescriptor ifDescriptor =
						CreateIssueFilterDescriptor(issueFilterType, constructorIdx);

					ClassDescriptor classDescriptor = ifDescriptor.Class;
					Assert.NotNull(classDescriptor);

					bool hasAlgorithmDefinition =
						InstanceDescriptorUtils.TryGetAlgorithmDefinitionType(
							classDescriptor, out Type definitionType);

					Assert.IsTrue(hasAlgorithmDefinition);
					Assert.NotNull(definitionType);

					IssueFilterDescriptor ifDefinitionDescriptor =
						CreateIssueFilterDescriptor(definitionType, constructorIdx);

					// The factory of the definition:
					IssueFilterFactory ifDefinitionFactory =
						InstanceFactoryUtils.GetIssueFilterDefinitionFactory(ifDescriptor);
					Assert.NotNull(ifDefinitionFactory);

					// Initialize the configurations for both the definition and the implementation:
					IssueFilterConfiguration ifDefinitionConfiguration =
						new IssueFilterConfiguration("if", ifDefinitionDescriptor);
					IssueFilterConfiguration ifConfinguration =
						new IssueFilterConfiguration("if", ifDescriptor);

					// Setup parameters (could go into the factory in the future):
					//ifDefinitionFactory.Condition = ifDefinitionConfiguration;
					InstanceConfigurationUtils.InitializeParameterValues(
						ifDefinitionFactory, ifDefinitionConfiguration);

					// The factory of the implementation:
					IssueFilterFactory ifFactory =
						InstanceFactoryUtils.CreateIssueFilterFactory(ifConfinguration);

					Assert.NotNull(ifFactory);

					// NOTE: The instantiation of the issue filters and the comparisons of the values are
					// performed in the AreParametersEqual test.
				}
			}
		}

		private record IfDefinitionCase(
			Type IssueFilterType,
			int ConstructorIndex = -1,
			object[] ConstructorValues = null,
			Dictionary<string, object> OptionalParamValues = null);

		private static List<IfDefinitionCase> DefineIfCases(TestDataModel model)
		{
			var ifCases = new List<IfDefinitionCase>();
			// Issue Filter cases with automatic parameter value generation:
			ifCases.AddRange(CreateDefaultValueIssueFilterCases(typeof(IfIntersecting)));
			ifCases.AddRange(CreateDefaultValueIssueFilterCases(typeof(IfNear)));
			ifCases.AddRange(CreateDefaultValueIssueFilterCases(typeof(IfWithin)));

			//
			// Special Cases
			//
			// Manually create values for special cases, such as optional parameters or
			// difficult assertions:
			AddIfAllCases(model, ifCases);
			//ToDo: Find correct special case for IfInvolvedRows ...
			//AddIfInvolvedRowsCases(model, ifCases);

			return ifCases;
		}

		[Test]
		public void AreIssueFilterParametersEqual()
		{
			var model = new TestDataModel("simple_model");

			List<IfDefinitionCase> ifCases = DefineIfCases(model);

			foreach (IfDefinitionCase ifCase in ifCases)
			{
				Type issueFilterType = ifCase.IssueFilterType;
				int constructorIdx = ifCase.ConstructorIndex;

				Console.WriteLine("Checking constructor index {0} for issue filter {1}",
				                  constructorIdx,
				                  issueFilterType.Name);

				object[] constructorValues = ifCase.ConstructorValues;

				// Optional parameters Name-Value pairs, null if not specified (or no optional parameters):
				Dictionary<string, object>
					optionalParamValues = ifCase.OptionalParamValues;

				IssueFilterDescriptor ifDescriptor =
					CreateIssueFilterDescriptor(issueFilterType, constructorIdx);

				ClassDescriptor classDescriptor = ifDescriptor.Class;
				Assert.NotNull(classDescriptor);

				IssueFilterFactory ifDefinitionFactory =
					InstanceFactoryUtils.GetIssueFilterDefinitionFactory(ifDescriptor);

				Assert.NotNull(ifDefinitionFactory);

				bool hasAlgorithmDefinition =
					InstanceDescriptorUtils.TryGetAlgorithmDefinitionType(
						classDescriptor, out Type definitionType);

				Assert.IsTrue(hasAlgorithmDefinition,
				              $"{ifCase.IssueFilterType.Name} has no IssueFilterDefinition class");

				IssueFilterDescriptor ifDefinitionDescriptor =
					CreateIssueFilterDescriptor(definitionType, constructorIdx);

				// Initialize the configurations for both the definition and the implementation:
				IssueFilterConfiguration ifDefinitionConfiguration =
					new IssueFilterConfiguration("if", ifDefinitionDescriptor);
				IssueFilterConfiguration ifConfinguration =
					new IssueFilterConfiguration("if", ifDescriptor);

				// Setup parameters (could go into the factory in the future):
				//ifDefinitionFactory.Condition = ifDefinitionConfiguration;
				InstanceConfigurationUtils.InitializeParameterValues(
					ifDefinitionFactory, ifDefinitionConfiguration);

				// The factory of the implementations
				IssueFilterFactory ifFactory =
					InstanceFactoryUtils.CreateIssueFilterFactory(ifConfinguration);

				Assert.NotNull(ifFactory);

				ConstructorInfo constructorInfo = issueFilterType.GetConstructors()[constructorIdx];

				Assert.AreEqual(constructorInfo.GetParameters().Length,
				                constructorValues.Length,
				                $"Wrong numbers of constructor parameters for constructor index {constructorIdx}");

				// Create the test parameter values:

				int constructorParameterCount =
					ifFactory.Parameters.Count(p => p.IsConstructorParameter);
				for (var i = 0; i < constructorParameterCount; i++)
				{
					TestParameter parameter = ifFactory.Parameters[i];
					object value = constructorValues[i];

					string parameterName = parameter.Name;

					AddParameterValueIssueFilter(parameterName, value, ifConfinguration,
					                             ifDefinitionConfiguration);
				}

				if (optionalParamValues != null)
				{
					foreach (KeyValuePair<string, object> optionalParam in
					         optionalParamValues)
					{
						AddParameterValueIssueFilter(optionalParam.Key, optionalParam.Value,
						                             ifConfinguration,
						                             ifDefinitionConfiguration);
					}
				}

				IIssueFilter testsOrig =
					InstanceFactoryUtils.CreateIssueFilter(ifConfinguration,
					                                       new SimpleDatasetOpener(model));

				IIssueFilter testsNew =
					InstanceFactoryUtils.CreateIssueFilter(ifDefinitionConfiguration,
					                                       new SimpleDatasetOpener(model));

				List<KeyValuePair<Type, MemberInfo>> differences =
					ReflectionCompare.RecursiveReflectionCompare(testsOrig, testsNew, true);

				foreach (KeyValuePair<Type, MemberInfo> difference in differences)
				{
					Console.WriteLine("Difference: {0} {1}", difference.Key.Name,
					                  difference.Value.Name);
				}

				Assert.AreEqual(0, differences.Count,
				                $"Differences found for {issueFilterType.Name} constructor index {constructorIdx}:");
			}
		}

		#region Methods to add special issue filter definition cases

		private static void AddIfAllCases(TestDataModel model,
		                                  ICollection<IfDefinitionCase> ifCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("Filter", "true");

			ifCases.Add(new IfDefinitionCase(typeof(IfAll), 0,
			                                 new object[]
			                                 { },
			                                 optionalValues));
		}

		private static void AddIfInvolvedRowsCases(TestDataModel model,
		                                           ICollection<IfDefinitionCase> ifCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add(
				"Tables", new object[] { model.GetVectorDataset(), model.GetVectorDataset() });

			ifCases.Add(new IfDefinitionCase(typeof(IfInvolvedRows), 0,
											  new object[]
											  {
												  //new[]
												  //{
													  //model.GetVectorDataset(),
													  //model.GetVectorDataset(),
												  //},
												  "Constraint"
											  },
											  optionalValues));
		}

		#endregion

		[Test]
		public void CanCreateTransformers()
		{
			List<Type> refactoredTypes = new List<Type>
			                             {
				                             typeof(TrDissolve),
											 typeof(TrGeometryToPoints),
				                             typeof(TrGetNodes),
				                             typeof(TrIntersect),
				                             typeof(TrMultilineToLine),
				                             typeof(TrMultipolygonToPolygon),
				                             typeof(TrPolygonToLine)
			                             };

			foreach (Type transformerType in refactoredTypes)
			{
				Assert.IsFalse(InstanceUtils.HasInternallyUsedAttribute(transformerType),
				               "Internally used transformer");

				// One is used internally to create using the definition.
				int constructorCount = transformerType.GetConstructors().Length - 1;

				bool lastConstructorIsInternallyUsed =
					InstanceUtils.IsInternallyUsed(transformerType, constructorCount);

				Assert.IsTrue(lastConstructorIsInternallyUsed,
				              $"Last constructor not internally used in {transformerType.Name}");

				for (int constructorIdx = 0;
				     constructorIdx < constructorCount;
				     constructorIdx++)
				{
					Console.WriteLine("Checking {0}({1})", transformerType.Name, constructorIdx);

					if (! InstanceUtils.IsObsolete(transformerType, constructorIdx))
					{
						CompareTransformerMetadata(transformerType, constructorIdx, true);
					}

					TransformerDescriptor trDescriptor =
						CreateTransformerDescriptor(transformerType, constructorIdx);

					ClassDescriptor classDescriptor = trDescriptor.Class;
					Assert.NotNull(classDescriptor);

					bool hasAlgorithmDefinition =
						InstanceDescriptorUtils.TryGetAlgorithmDefinitionType(
							classDescriptor, out Type definitionType);

					Assert.IsTrue(hasAlgorithmDefinition);
					Assert.NotNull(definitionType);

					TransformerDescriptor trDefinitionDescriptor =
						CreateTransformerDescriptor(definitionType, constructorIdx);

					// The factory of the definition:
					TransformerFactory trDefinitionFactory =
						InstanceFactoryUtils.GetTransformerDefinitionFactory(trDescriptor);
					Assert.NotNull(trDefinitionFactory);

					// Initialize the configurations for both the definition and the implementation:
					TransformerConfiguration trDefinitionConfiguration =
						new TransformerConfiguration("tr", trDefinitionDescriptor);
					TransformerConfiguration trConfinguration =
						new TransformerConfiguration("tr", trDescriptor);

					// Setup parameters (could go into the factory in the future):
					InstanceConfigurationUtils.InitializeParameterValues(
						trDefinitionFactory, trDefinitionConfiguration);

					// The factory of the implementation:
					TransformerFactory trFactory =
						InstanceFactoryUtils.CreateTransformerFactory(trConfinguration);

					Assert.NotNull(trFactory);

					// NOTE: The instantiation of the issue filters and the comparisons of the values are
					// performed in the AreParametersEqual test.
				}
			}
		}

		private record TrDefinitionCase(
			Type TransformerType,
			int ConstructorIndex = -1,
			object[] ConstructorValues = null,
			Dictionary<string, object> OptionalParamValues = null);

		[Test]
		public void AreTransformerParametersEqual()
		{
			var model = new TestDataModel("simple model");

			var trCases = new List<TrDefinitionCase>();

			// Transformer cases with automatic parameter value generation:
			//trCases.AddRange(CreateDefaultValueTransformerCases(typeof(TrDissolve)));
			trCases.AddRange(CreateDefaultValueTransformerCases(typeof(TrIntersect)));

			//
			// Special Cases
			//
			// Manually create values for special cases, such as optional parameters or
			// difficult assertions:
			AddTrDissolveCases(model, trCases);
			AddTrGeometryToPointsCases(model, trCases);
			AddTrGetNodesCases(model, trCases);
			AddTrMultilineToLineCases(model, trCases);
			AddTrMultipolygonToPolygonCases(model, trCases);
			AddTrPolygonToLineCases(model, trCases);

			foreach (TrDefinitionCase trCase in trCases)
			{
				Type transformerType = trCase.TransformerType;
				int constructorIdx = trCase.ConstructorIndex;

				Console.WriteLine("Checking constructor index {0} for transformer {1}",
				                  constructorIdx,
				                  transformerType.Name);

				object[] constructorValues = trCase.ConstructorValues;

				// Optional parameters Name-Value pairs, null if not specified (or no optional parameters):
				Dictionary<string, object>
					optionalParamValues = trCase.OptionalParamValues;

				TransformerDescriptor trDescriptor =
					CreateTransformerDescriptor(transformerType, constructorIdx);

				ClassDescriptor classDescriptor = trDescriptor.Class;
				Assert.NotNull(classDescriptor);

				TransformerFactory trDefinitionFactory =
					InstanceFactoryUtils.GetTransformerDefinitionFactory(trDescriptor);

				Assert.NotNull(trDefinitionFactory);

				bool hasAlgorithmDefinition =
					InstanceDescriptorUtils.TryGetAlgorithmDefinitionType(
						classDescriptor, out Type definitionType);

				Assert.IsTrue(hasAlgorithmDefinition,
				              $"{trCase.TransformerType.Name} has no TransformerDefinition class");

				TransformerDescriptor trDefinitionDescriptor =
					CreateTransformerDescriptor(definitionType, constructorIdx);

				// Initialize the configurations for both the definition and the implementation:
				TransformerConfiguration trDefinitionConfiguration =
					new TransformerConfiguration("tr", trDefinitionDescriptor);
				TransformerConfiguration trConfinguration =
					new TransformerConfiguration("tr", trDescriptor);

				// Setup parameters (could go into the factory in the future):
				InstanceConfigurationUtils.InitializeParameterValues(
					trDefinitionFactory, trDefinitionConfiguration);

				// The factory of the implementations
				TransformerFactory trFactory =
					InstanceFactoryUtils.CreateTransformerFactory(trConfinguration);

				Assert.NotNull(trFactory);

				ConstructorInfo constructorInfo = transformerType.GetConstructors()[constructorIdx];

				Assert.AreEqual(constructorInfo.GetParameters().Length,
				                constructorValues.Length,
				                $"Wrong numbers of constructor parameters for constructor index {constructorIdx}");

				// Create the test parameter values:

				int constructorParameterCount =
					trFactory.Parameters.Count(p => p.IsConstructorParameter);
				for (var i = 0; i < constructorParameterCount; i++)
				{
					TestParameter parameter = trFactory.Parameters[i];
					object value = constructorValues[i];

					string parameterName = parameter.Name;

					AddParameterValueTransformer(parameterName, value, trConfinguration,
					                             trDefinitionConfiguration);
				}

				if (optionalParamValues != null)
				{
					foreach (KeyValuePair<string, object> optionalParam in
					         optionalParamValues)
					{
						AddParameterValueTransformer(optionalParam.Key, optionalParam.Value,
						                             trConfinguration,
						                             trDefinitionConfiguration);
					}
				}

				ITableTransformer testsOrig =
					InstanceFactoryUtils.CreateTransformer(trConfinguration,
					                                       new SimpleDatasetOpener(model));

				ITableTransformer testsNew =
					InstanceFactoryUtils.CreateTransformer(trConfinguration,
					                                       new SimpleDatasetOpener(model));

				List<KeyValuePair<Type, MemberInfo>> differences =
					ReflectionCompare.RecursiveReflectionCompare(testsOrig, testsNew, true);

				foreach (KeyValuePair<Type, MemberInfo> difference in differences)
				{
					Console.WriteLine("Difference: {0} {1}", difference.Key.Name,
					                  difference.Value.Name);
				}

				Assert.AreEqual(0, differences.Count,
				                $"Differences found for {transformerType.Name} constructor index {constructorIdx}:");
			}
		}

		private static void AddTrDissolveCases(TestDataModel model,
		                                       ICollection<TrDefinitionCase>
			                                       trCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("Search", 1.0);
			optionalValues.Add("NeighborSearchOption", "Tile");
			optionalValues.Add("Attributes", "MAX(LAUF_NR) AS MAX_LAUF_NR");
			optionalValues.Add("GroupBy", "");
			optionalValues.Add("Constraint", "");
			optionalValues.Add("CreateMultipartFeatures", false);

			trCases.Add(new TrDefinitionCase(typeof(TrDissolve), 0,
			                                 new object[]
			                                 {
				                                 model.GetVectorDataset(),
			                                 },
			                                 optionalValues));
		}


		private static void AddTrGeometryToPointsCases(TestDataModel model,
												ICollection<TrDefinitionCase>
													trCases)
		{
			var optionalValues = new Dictionary<string, object>();

			trCases.Add(new TrDefinitionCase(typeof(TrGeometryToPoints), 0,
											 new object[]
											 {
												 model.GetPolygonDataset(),
												 "EntireGeometry"
											 },
												optionalValues));
		}


		private static void AddTrGetNodesCases(TestDataModel model,
		                                       ICollection<TrDefinitionCase>
			                                       trCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("Attributes", "MAX(LAUF_NR) AS MAX_LAUF_NR");

			trCases.Add(new TrDefinitionCase(typeof(TrGetNodes), 0,
			                                 new object[]
			                                 {
				                                 model.GetVectorDataset(),
			                                 },
			                                 optionalValues));
		}

		private static void AddTrMultilineToLineCases(TestDataModel model,
		                                            ICollection<TrDefinitionCase>
			                                            trCases)
		{
			trCases.Add(new TrDefinitionCase(typeof(TrMultilineToLine), 0,
			                                 new object[]
			                                 {
				                                 model.GetVectorDataset(),
			                                 }));
		}

		private static void AddTrMultipolygonToPolygonCases(TestDataModel model,
		                                              ICollection<TrDefinitionCase>
			                                              trCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("TransformedParts", "SinglePolygons");

			trCases.Add(new TrDefinitionCase(typeof(TrMultipolygonToPolygon), 0,
			                                 new object[]
			                                 {
				                                 model.GetVectorDataset(),
			                                 },
			                                 optionalValues));
		}

		private static void AddTrPolygonToLineCases(TestDataModel model,
		                                                    ICollection<TrDefinitionCase>
			                                                    trCases)
		{
			trCases.Add(new TrDefinitionCase(typeof(TrPolygonToLine), 0,
			                                 new object[]
			                                 {
				                                 model.GetVectorDataset(),
			                                 }));
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

		private static void AddParameterValueIssueFilter(string parameterName, object value,
		                                                 IssueFilterConfiguration issuefilter,
		                                                 IssueFilterConfiguration
			                                                 issueFilterDef)
		{
			// NOTE: For lists, multiple IssueFilterParameterValues can be added for the same IssueFilterParameter.
			if (value is IEnumerable enumerable and not string)
			{
				foreach (object singleValue in enumerable)
				{
					AddSingleParameterValueIssueFilter(parameterName, singleValue, issuefilter,
					                                   issueFilterDef);
				}
			}
			else
			{
				AddSingleParameterValueIssueFilter(parameterName, value, issuefilter,
				                                   issueFilterDef);
			}
		}

		private static void AddSingleParameterValueIssueFilter(string parameterName, object value,
		                                                       IssueFilterConfiguration issuefilter,
		                                                       IssueFilterConfiguration
			                                                       issueFilterDef)
		{
			if (value is Dataset datasetVal)
			{
				TestParameterValueUtils.AddParameterValue(
					issuefilter, parameterName, datasetVal);
				TestParameterValueUtils.AddParameterValue(
					issueFilterDef, parameterName, datasetVal);
			}
			else
			{
				string stringVal = Convert.ToString(value);
				TestParameterValueUtils.AddParameterValue(
					issuefilter, parameterName, stringVal);
				TestParameterValueUtils.AddParameterValue(
					issueFilterDef, parameterName, stringVal);
			}
		}

		private static void AddParameterValueTransformer(string parameterName, object value,
		                                                 TransformerConfiguration transformer, //**
		                                                 TransformerConfiguration
			                                                 transformerDef) //**
		{
			// NOTE: For lists, multiple TranformerValues can be added for the same Transformer.
			if (value is IEnumerable enumerable and not string)
			{
				foreach (object singleValue in enumerable)
				{
					AddSingleParameterValueTransformer(parameterName, singleValue, transformer,
					                                   transformerDef);
				}
			}
			else
			{
				AddSingleParameterValueTransformer(parameterName, value, transformer,
				                                   transformerDef);
			}
		}

		private static void AddSingleParameterValueTransformer(string parameterName, object value,
		                                                       TransformerConfiguration transformer,
		                                                       TransformerConfiguration
			                                                       transformerDef)
		{
			if (value is Dataset datasetVal)
			{
				TestParameterValueUtils.AddParameterValue(
					transformer, parameterName, datasetVal);
				TestParameterValueUtils.AddParameterValue(
					transformerDef, parameterName, datasetVal);
			}
			else
			{
				string stringVal = Convert.ToString(value);
				TestParameterValueUtils.AddParameterValue(
					transformer, parameterName, stringVal);
				TestParameterValueUtils.AddParameterValue(
					transformerDef, parameterName, stringVal);
			}
		}

		private static IEnumerable<TestDefinitionCase> CreateDefaultValueTestCases(
			Type testType, TestDataModel model)
		{
			// One is used internally to create using the definition.
			int constructorCount = testType.GetConstructors().Length - 1;

			for (int constructorIdx = 0;
			     constructorIdx < constructorCount;
			     constructorIdx++)
			{
				Console.WriteLine("Creating default test case for {0}({1})", testType.Name,
				                  constructorIdx);

				bool hasNoCategory = TestTypeHasNoCategory(testType);

				CompareTestMetadata(testType, constructorIdx, hasNoCategory);

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

		private static bool TestTypeHasNoCategory(Type testType)
		{
			bool hasNoCategory = testType == typeof(QaExportTables) ||
			                     testType == typeof(QaRowCount);

			return hasNoCategory;
		}

		private static void CompareTestMetadata(Type testType,
		                                        int constructorIdx,
		                                        bool allowNoCategory = false)
		{
			TestDescriptor testDescriptor = CreateTestDescriptor(testType, constructorIdx);
			Assert.NotNull(testDescriptor.Class);

			IInstanceInfo instanceInfo = CheckInstanceInfo(testDescriptor, allowNoCategory);

			// Algorithm Definition:
			bool hasAlgorithmDefinition =
				InstanceDescriptorUtils.TryGetAlgorithmDefinitionType(
					testDescriptor.Class, out Type definitionType);

			Assert.IsTrue(hasAlgorithmDefinition);

			// Using instance definition:
			InstanceDescriptor instanceDefDescriptor =
				CreateTestDescriptor(definitionType, constructorIdx);

			IInstanceInfo instanceDefInfo =
				CheckInstanceInfo(instanceDefDescriptor, allowNoCategory, true);

			Assert.IsTrue(AssertEqual(instanceInfo, instanceDefInfo));

			// Use case 2: Load actual factory:
			TestFactory testFactory = TestFactoryUtils.GetTestFactory(testDescriptor);
			Assert.NotNull(testFactory);

			// and definition factory
			TestFactory testDefinitionFactory =
				TestFactoryUtils.GetTestDefinitionFactory(testDescriptor);
			Assert.NotNull(testDefinitionFactory);

			// and compare:
			Assert.IsTrue(AssertEqual(testFactory, testDefinitionFactory));

			Assert.Greater(instanceInfo.Parameters.Count, 0);
			Assert.Greater(testDefinitionFactory.Parameters.Count, 0);
		}

		private static IEnumerable<IfDefinitionCase> CreateDefaultValueIssueFilterCases(
			Type issueFilterType)
		{
			// One is used internally to create using the definition.
			int constructorCount = issueFilterType.GetConstructors().Length - 1;

			for (int constructorIdx = 0;
			     constructorIdx < constructorCount;
			     constructorIdx++)
			{
				Console.WriteLine("Checking {0}({1})", issueFilterType.Name, constructorIdx);

				const bool hasNoCategory = true;

				CompareIssueFilterMetadata(issueFilterType, constructorIdx, hasNoCategory);

				IssueFilterDescriptor filterImplDescriptor =
					CreateIssueFilterDescriptor(issueFilterType, constructorIdx);

				ClassDescriptor classDescriptor = filterImplDescriptor.Class;
				Assert.NotNull(classDescriptor);

				bool hasAlgorithmDefinition =
					InstanceDescriptorUtils.TryGetAlgorithmDefinitionType(
						classDescriptor, out Type definitionType);

				Assert.IsTrue(hasAlgorithmDefinition);
				Assert.NotNull(definitionType);

				IssueFilterConfiguration issueFilter =
					new IssueFilterConfiguration("if", filterImplDescriptor);

				// The factory of the implementations
				IssueFilterFactory issueFilterFactory =
					InstanceFactoryUtils.CreateIssueFilterFactory(issueFilter);

				Assert.NotNull(issueFilterFactory);

				var model = new TestDataModel("simple model");

				var parameterList = new List<object>();
				foreach (TestParameter parameter in issueFilterFactory.Parameters)
				{
					object defaultVal =
						CreateDefaultValue(
							TestParameterTypeUtils.GetParameterType(parameter.Type), model);

					parameterList.Add(defaultVal);
				}

				IfDefinitionCase result = new IfDefinitionCase(issueFilterType,
				                                               constructorIdx,
				                                               parameterList.ToArray());

				bool hasOptionalParameters =
					issueFilterFactory.Parameters.Any(p => ! p.IsConstructorParameter);

				Assert.False(hasOptionalParameters,
				             $"The type {issueFilterType.Name} has optional parameters. Use manual configuration!");

				yield return result;
			}
		}

		private static void CompareIssueFilterMetadata(Type testType,
		                                               int constructorIdx,
		                                               bool allowNoCategory = false)
		{
			IssueFilterDescriptor descriptor =
				CreateIssueFilterDescriptor(testType, constructorIdx);
			Assert.NotNull(descriptor.Class);

			IInstanceInfo ifInstanceInfo = CheckInstanceInfo(descriptor, allowNoCategory);

			// Algorithm Definition:
			bool hasAlgorithmDefinition =
				InstanceDescriptorUtils.TryGetAlgorithmDefinitionType(
					descriptor.Class, out Type definitionType);

			Assert.IsTrue(hasAlgorithmDefinition);

			// Using instance definition:
			InstanceDescriptor instanceDefDescriptor =
				CreateTestDescriptor(definitionType, constructorIdx);

			IInstanceInfo instanceDefinitionInfo =
				CheckInstanceInfo(instanceDefDescriptor, allowNoCategory, true);

			Assert.IsTrue(AssertEqual(ifInstanceInfo, instanceDefinitionInfo));

			// Use case 2: Load actual factory:
			IssueFilterFactory ifFactory =
				InstanceFactoryUtils.CreateIssueFilterFactory(descriptor);
			Assert.NotNull(ifFactory);

			// and definition factory
			var ifDefinitionFactory =
				InstanceFactoryUtils.GetIssueFilterDefinitionFactory(descriptor);
			Assert.NotNull(ifDefinitionFactory);

			// and compare:
			Assert.IsTrue(AssertEqual(ifFactory, ifDefinitionFactory));

			Assert.Greater(ifInstanceInfo.Parameters.Count, 0);
			Assert.Greater(ifDefinitionFactory.Parameters.Count, 0);
		}

		private static IEnumerable<TrDefinitionCase> CreateDefaultValueTransformerCases(
			Type transformerType)
		{
			// One is used internally to create using the definition.
			int constructorCount = transformerType.GetConstructors().Length - 1;

			for (int constructorIdx = 0;
			     constructorIdx < constructorCount;
			     constructorIdx++)
			{
				Console.WriteLine("Checking {0}({1})", transformerType.Name, constructorIdx);

				bool hasNoCategory = TestTypeHasNoCategory(transformerType);

				CompareTestMetadata(transformerType, constructorIdx, hasNoCategory);

				CompareTransformerMetadata(transformerType, constructorIdx, hasNoCategory);

				TransformerDescriptor filterImplDescriptor =
					CreateTransformerDescriptor(transformerType, constructorIdx);

				ClassDescriptor classDescriptor = filterImplDescriptor.Class;
				Assert.NotNull(classDescriptor);

				bool hasAlgorithmDefinition =
					InstanceDescriptorUtils.TryGetAlgorithmDefinitionType(
						classDescriptor, out Type definitionType);

				Assert.IsTrue(hasAlgorithmDefinition);
				Assert.NotNull(definitionType);

				TransformerConfiguration transformer =
					new TransformerConfiguration("tr", filterImplDescriptor);

				// The factory of the implementations
				TransformerFactory transformerFactory =
					InstanceFactoryUtils.CreateTransformerFactory(transformer);

				Assert.NotNull(transformerFactory);

				var model = new TestDataModel("simple model");

				var parameterList = new List<object>();
				foreach (TestParameter parameter in transformerFactory.Parameters)
				{
					object defaultVal =
						CreateDefaultValue(
							TestParameterTypeUtils.GetParameterType(parameter.Type), model);

					parameterList.Add(defaultVal);
				}

				TrDefinitionCase result = new TrDefinitionCase(transformerType,
				                                               constructorIdx,
				                                               parameterList.ToArray());

				bool hasOptionalParameters =
					transformerFactory.Parameters.Any(p => ! p.IsConstructorParameter);

				Assert.False(hasOptionalParameters,
				             $"The type {transformerType.Name} has optional parameters. Use manual configuration!");

				yield return result;
			}
		}

		private static void CompareTransformerMetadata(Type testType,
		                                               int constructorIdx,
		                                               bool allowNoCategory = false)
		{
			TransformerDescriptor descriptor =
				CreateTransformerDescriptor(testType, constructorIdx);
			Assert.NotNull(descriptor.Class);

			IInstanceInfo trInstanceInfo = CheckInstanceInfo(descriptor, allowNoCategory);

			// Algorithm Definition:
			bool hasAlgorithmDefinition =
				InstanceDescriptorUtils.TryGetAlgorithmDefinitionType(
					descriptor.Class, out Type definitionType);

			Assert.IsTrue(hasAlgorithmDefinition);

			// Using instance definition:
			InstanceDescriptor instanceDefDescriptor =
				CreateTestDescriptor(definitionType, constructorIdx);

			IInstanceInfo instanceDefinitionInfo =
				CheckInstanceInfo(instanceDefDescriptor, allowNoCategory, true);

			Assert.IsTrue(AssertEqual(trInstanceInfo, instanceDefinitionInfo));

			// Use case 2: Load actual factory:
			TransformerFactory trFactory =
				InstanceFactoryUtils.CreateTransformerFactory(descriptor);
			Assert.NotNull(trFactory);

			// and definition factory
			var trDefinitionFactory =
				InstanceFactoryUtils.GetTransformerDefinitionFactory(descriptor);
			Assert.NotNull(trDefinitionFactory);

			// and compare:
			Assert.IsTrue(AssertEqual(trFactory, trDefinitionFactory));

			Assert.Greater(trInstanceInfo.Parameters.Count, 0);
			Assert.Greater(trDefinitionFactory.Parameters.Count, 0);
		}

		private static IInstanceInfo CheckInstanceInfo(InstanceDescriptor descriptor,
		                                               bool allowNoCategory,
		                                               bool tryAlgorithmDefinition = false)
		{
			Assert.NotNull(descriptor.Class);

			// Using instance info in DDX:
			IInstanceInfo instanceInfo =
				InstanceDescriptorUtils.GetInstanceInfo(descriptor, tryAlgorithmDefinition);

			Assert.NotNull(instanceInfo);
			Assert.NotNull(instanceInfo.TestDescription);

			if (! allowNoCategory)
			{
				Assert.Greater(instanceInfo.TestCategories.Length, 0);
			}

			return instanceInfo;
		}

		private static object CreateDefaultValue(TestParameterType testParameterType,
		                                         TestDataModel model)
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

		private static IssueFilterDescriptor CreateIssueFilterDescriptor(Type type,
			int constructorIndex)
		{
			string typeName = $"{type.Name}";

			return new IssueFilterDescriptor(typeName, new ClassDescriptor(type),
			                                 constructorIndex);
		}

		private static TransformerDescriptor CreateTransformerDescriptor(Type type,
			int constructorIndex)
		{
			string typeName = $"{type.Name}";

			return new TransformerDescriptor(typeName, new ClassDescriptor(type),
			                                 constructorIndex);
		}

		[Test]
		public static void CheckCompletenessCanCreateTest()
		{
			Type typeFromTestAssembly = typeof(Qa3dConstantZ);
			Assembly testAssembly = typeFromTestAssembly.Assembly;

			List<Assembly> assemblies = new List<Assembly> { testAssembly };

			List<Type> testTypes = CollectTestTypes(assemblies);

			List<Type> missingCanCreateTypes =
				FindMissingCanCreateTypes(testTypes, RefactoredTypes);

			if (missingCanCreateTypes.Any())
			{
				foreach (var missingCanCreateType in missingCanCreateTypes)
				{
					Console.WriteLine("Not implemented in method CanCreateTest: " +
					                  missingCanCreateType.Name);
				}

				throw new AssertionException(
					"One or more types missing in method CanCreateTest()");
			}
			else
			{
				Console.WriteLine(
					"All types are present in the RefactoredTypes in method CanCreateTest().");
			}
		}

		[Test]
		public static void CheckCompletenessAreParametersEqual()
		{
			List<Type> missingAreParametersEqualTypes =
				FindMissingAreParametersEqualTypes(RefactoredTypes);

			if (missingAreParametersEqualTypes.Any())
			{
				foreach (var missingAreParametersEqualType in missingAreParametersEqualTypes)
				{
					Console.WriteLine("Not implemented in method AreParametersEqual: " +
					                  missingAreParametersEqualType.Name);
				}

				throw new AssertionException(
					"One or more types missing in method AreParametersEqual()");
			}

			else
			{
				Console.WriteLine(
					"All types are present in testCases in method AreParametersEqual().");
			}
		}

		[Test]
		public static void CheckCompletenessCanCreateIssueFilter()
		{
			Type typeFromTestAssembly = typeof(IfAll);

			Assembly testAssembly = typeFromTestAssembly.Assembly;
			string assemblyName = testAssembly.GetName().Name;
			string desiredNamespace = $"{assemblyName}.IssueFilters";
			var issueFilterTypes = testAssembly.GetTypes()
			                                   .Where(t => t.Namespace == desiredNamespace &&
			                                               t.IsPublic == true)
			                                   .ToList();

			List<Type> missingCanCreateTypes =
				FindMissingCanCreateTypes(issueFilterTypes, IfRefactoredTypes);

			if (missingCanCreateTypes.Any())
			{
				foreach (var missingCanCreateType in missingCanCreateTypes)
				{
					Console.WriteLine("Not implemented in method CanCreateIssueFilters: " +
					                  missingCanCreateType.Name);
				}

				throw new AssertionException(
					"One or more types missing in method CanCreateIssueFilters()");
			}
			else
			{
				Console.WriteLine(
					"All types are present in the IfRefactoredTypes in method CanCreateIssueFilters().");
			}
		}

		[Test]
		public static void CheckCompletenessAreIssueFilterParametersEqual()
		{
			List<Type> missingAreParametersEqualTypes =
				FindMissingAreIssueFilterParametersEqualTypes(IfRefactoredTypes);

			if (missingAreParametersEqualTypes.Any())
			{
				foreach (var missingAreParametersEqualType in missingAreParametersEqualTypes)
				{
					Console.WriteLine("Not implemented in method AreIssueFilterParametersEqual: " +
					                  missingAreParametersEqualType.Name);
				}

				throw new AssertionException(
					"One or more types missing in method AreIssueFilterParametersEqual()");
			}

			else
			{
				Console.WriteLine(
					"All types are present in ifCases in method AreIssueFilterParametersEqual().");
			}
		}

		private static List<Type> CollectTestTypes(IEnumerable<Assembly> assemblies)
		{
			const bool includeObsolete = false;
			const bool includeInternallyUsed = false;

			var testTypes = new List<Type>();

			foreach (Assembly assembly in assemblies)
			{
				foreach (Type testType in TestFactoryUtils.GetTestClasses(
					         assembly, includeObsolete, includeInternallyUsed))
				{
					// Add the testType to the list
					testTypes.Add(testType);
				}
			}

			return testTypes;
		}

		static List<Type> FindMissingCanCreateTypes(List<Type> testassemblyTypes,
		                                            List<Type> canCreateTypes)
		{
			return testassemblyTypes.Except(canCreateTypes).ToList();
		}

		static List<Type> FindMissingAreParametersEqualTypes(List<Type> testassemblyTypes)
		{
			var model = new TestDataModel("simple model");

			List<TestDefinitionCase> testCases = DefineTestCases(model);

			List<Type> testDefinitionTypes = new List<Type>();

			foreach (TestDefinitionCase testCase in testCases)
			{
				testDefinitionTypes.Add(testCase.TestType);
			}

			return testassemblyTypes.Except(testDefinitionTypes).ToList();
		}

		static List<Type> FindMissingAreIssueFilterParametersEqualTypes(List<Type> issueFilterTypes)
		{
			var model = new TestDataModel("simple model");

			List<IfDefinitionCase> ifCases = DefineIfCases(model);

			List<Type> ifDefinitionTypes = new List<Type>();

			foreach (IfDefinitionCase ifCase in ifCases)
			{
				ifDefinitionTypes.Add(ifCase.IssueFilterType);
			}

			return issueFilterTypes.Except(ifDefinitionTypes).ToList();
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

		public static List<KeyValuePair<Type, MemberInfo>> RecursiveReflectionCompare<T>(
			T first, T second, bool includeNonPublicMembers = false)
			where T : class
		{
			var differences = new List<KeyValuePair<Type, MemberInfo>>();

			var parentType = first.GetType();

			BindingFlags bindingFlags = includeNonPublicMembers
				                            ? BindingFlags.NonPublic | BindingFlags.Instance |
				                              BindingFlags.Static
				                            : BindingFlags.Public | BindingFlags.Instance |
				                              BindingFlags.Static;

			//bindingFlags = BindingFlags.Public | BindingFlags.Instance |
			//               BindingFlags.Static;
			foreach (PropertyInfo property in parentType.GetProperties(bindingFlags))
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

				if (property.DeclaringType == typeof(Delegate))
				{
					continue;
				}

				object value1 = property.GetValue(first, null);
				object value2 = property.GetValue(second, null);

				if (value1 is IGeometry geom1 && value2 is IGeometry geom2)
				{
					Assert.IsTrue(GeometryUtils.AreEqual(geom1, geom2));
					continue;
				}

				if (property.PropertyType == typeof(string))
				{
					if (string.IsNullOrEmpty(value1 as string) !=
					    string.IsNullOrEmpty(value2 as string))
					{
						CompareObject(value1, value2, property, differences, parentType);
					}
				}
				else if (property.PropertyType.IsPrimitive)
				{
					CompareObject(value1, value2, property, differences, parentType);
				}
				else
				{
					if (value1 == null && value2 == null)
					{
						continue;
					}

					List<KeyValuePair<Type, MemberInfo>> subDifferences =
						RecursiveReflectionCompare(value1, value2);

					if (subDifferences.Count > 0)
					{
						Console.WriteLine($"Found differences in {property.Name}:");
						foreach (KeyValuePair<Type, MemberInfo> difference in subDifferences)
						{
							Console.WriteLine("Difference: {0} {1}", difference.Key.Name,
							                  difference.Value.Name);
						}

						differences.AddRange(subDifferences);
					}
				}
			}

			// Additionally, compare private fields with primitive types:
			foreach (FieldInfo fieldInfo in parentType.GetFields(bindingFlags))
			{
				object value1 = fieldInfo.GetValue(first);
				object value2 = fieldInfo.GetValue(second);

				if (fieldInfo.FieldType == typeof(string))
				{
					if (string.IsNullOrEmpty(value1 as string) !=
					    string.IsNullOrEmpty(value2 as string))
					{
						CompareObject(value1, value2, fieldInfo, differences, parentType);
					}
				}
				else if (fieldInfo.FieldType.IsPrimitive)
				{
					CompareObject(value1, value2, fieldInfo, differences, parentType);
				}
				else if (fieldInfo.FieldType.IsEnum)
				{
					if (! Equals(value1, value2))
					{
						differences.Add(new KeyValuePair<Type, MemberInfo>(parentType, fieldInfo));
					}
				}

				// TODO: Consider checking also non-primitive fields
				//CompareNonPrimitiveObjects(value1, value2, fieldInfo, differences);
			}

			return differences;
		}

		private static void CompareObject(object obj1, object obj2,
		                                  MemberInfo info,
		                                  List<KeyValuePair<Type, MemberInfo>> differences,
		                                  Type parentType)
		{
			if (! obj1.Equals(obj2))
			{
				differences.Add(new KeyValuePair<Type, MemberInfo>(parentType, info));
			}
		}
	}
}
