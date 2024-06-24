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
using ProSuite.QA.Core.ParameterTypes;
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
				                             typeof(QaCentroids),
											 typeof(QaCoplanarRings),
											 typeof(QaConstraint),
											 typeof(QaContainedPointsCount),
											 typeof(QaContainsOther),
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
											 //typeof(QaExportTables),
				                             typeof(QaExtent),
				                             typeof(QaFlowLogic),
				                             typeof(QaForeignKey),
				                             typeof(QaFullCoincidence),
				                             typeof(QaGdbConstraint),
											 typeof(QaGdbRelease),
				                             typeof(QaGeometryConstraint),
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
											 typeof(QaMinSegAngle),
											 typeof(QaMonotonicMeasures),
											 typeof(QaMonotonicZ),
											 typeof(QaMpAllowedPartTypes),
											 typeof(QaMpConstantPointIdsPerRing),
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
											 typeof(QaNoBoundaryLoops),
											 typeof(QaNoClosedPaths),
											 typeof(QaNodeLineCoincidence),
											 typeof(QaNoGaps),
											 typeof(QaNonEmptyGeometry),
											 typeof(QaOrphanNode),
											 typeof(QaOverlapsSelf),
											 typeof(QaOverlapsOther),
											 typeof(QaPartCoincidenceOther),
											 typeof(QaPartCoincidenceSelf),
											 typeof(QaPointOnLine),
											 typeof(QaRegularExpression),
											 typeof(QaRequiredFields),
											 typeof(QaSchemaFieldAliases),
											 typeof(QaSchemaFieldDomainCodedValues),
											 typeof(QaSchemaFieldDomainNameRegex),
											 typeof(QaSchemaFieldDomainNames),
											 typeof(QaSchemaFieldDomains),
											 typeof(QaSchemaFieldNameRegex),
											 typeof(QaSchemaFieldNames),
											 typeof(QaSchemaFieldProperties),
											 typeof(QaSchemaFieldPropertiesFromTable),
											 typeof(QaSchemaReservedFieldNames),
											 typeof(QaSchemaReservedFieldNameProperties),
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
											 typeof(QaValidNonLinearSegments),
											 typeof(QaValidUrls),
											 typeof(QaValue),
											 typeof(QaValidDateValues),
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

					if (! InstanceUtils.IsObsolete(testType, constructorIdx))
					{
						CompareMetadata(testType, constructorIdx);
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

		[Test]
		public void AreParametersEqual()
		{
			var model = new InMemoryTestDataModel("simple model");

			var testCases = new List<TestDefinitionCase>();

			//// Test cases with automatic parameter value generation:
			testCases.AddRange(CreateDefaultValueTestCases(typeof(Qa3dConstantZ)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaBorderSense)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaCentroids)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaCoplanarRings)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaConstraint)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaContainsOther)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaCrossesOther)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaCrossesSelf)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaDangleCount)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaDuplicateGeometrySelf)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaForeignKey)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaGdbConstraint)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaLineConnectionFieldValues)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMaxArea)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMaxLength)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMaxVertexCount)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMeasures)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMeasuresAtPoints)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMinArea)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMinIntersect)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMinLength)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMinMeanSegmentLength)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMonotonicMeasures)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMpAllowedPartTypes)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMpConstantPointIdsPerRing)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMpHorizontalAzimuths)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMpHorizontalHeights)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMpHorizontalPerpendicular)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMpVerticalFaces)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMultipart)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMustIntersectOther)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMustTouchOther)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaNoClosedPaths)));

			// TODO: Implement Definition
			//testCases.AddRange(CreateDefaultValueTestCases(typeof(QaEmptyNotNullTextFields)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaExtent)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaFlowLogic)));

			// TODO: Add special case 
			//testCases.AddRange(CreateDefaultValueTestCases(typeof(QaGeometryConstraint)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaHorizontalSegments)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaMustTouchSelf)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaNonEmptyGeometry)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaOrphanNode)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaOverlapsSelf)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaOverlapsOther)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaPointOnLine)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaRequiredFields)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaFieldAliases)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaFieldDomainCodedValues)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaFieldDomainNameRegex)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaFieldDomainNames)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaFieldDomains)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaFieldNameRegex)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaFieldNames)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaFieldProperties)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaFieldPropertiesFromTable)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaReservedFieldNames)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSchemaReservedFieldNameProperties)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSegmentLength)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSimpleGeometry)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSurfacePipe)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSurfaceSpikes)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaSurfaceVertex)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaUnique)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaUnreferencedRows)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaValidNonLinearSegments)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaValue)));
			testCases.AddRange(CreateDefaultValueTestCases(typeof(QaWithinZRange)));

			//
			// Special Cases
			//
			// Manually create values for special cases, such as optional parameters or
			// difficult assertions:
			AddQaContainedPointsCountCases(model, testCases);
			AddQaCurveTestCases(model, testCases); //example optional parameters
			AddQaDateFieldsWithoutTimeCases(model, testCases); //example for assertions requiring special parameter values
			AddQaEdgeMatchBorderingLinesCases(model, testCases);
			AddQaEdgeMatchBorderingPointsCases(model, testCases);
			AddQaEdgeMatchCrossingAreasCases(model, testCases);
			AddQaEdgeMatchCrossingLinesCases(model, testCases);
			//AddQaExportTablesCases(model, testCases);
			AddQaFullCoincidenceCases(model, testCases);
			AddQaGdbReleaseCases(model, testCases);
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
			AddQaLineIntersectCases(model, testCases);
			AddQaLineIntersectAngleCases(model, testCases);
			AddQaLineIntersectZCases(model, testCases);
			AddQaMaxSlopeCases(model, testCases);
			AddQaMinAngleCases(model, testCases);
			AddQaMinSegAngleCases(model, testCases);
			AddQaMonotonicZCases(model, testCases);
			AddQaMpNonIntersectingRingFootprintsCases(model, testCases);
			AddQaMpSinglePartFootprintCases(model, testCases);
			AddQaMpVertexNotNearFaceCases(model, testCases);
			AddQaMustBeNearOtherCases(model, testCases);
			AddQaMustIntersectMatrixOtherCases(model, testCases);
			AddQaNoBoundaryLoopsCases(model, testCases);
			AddQaNodeLineCoincidenceCases(model, testCases);
			AddQaNoGapsCases(model, testCases);
			AddQaPartCoincidenceOtherCases(model, testCases);
			AddQaPartCoincidenceSelfCases(model, testCases);
			AddQaRegularExpressionCases(model, testCases);
			AddQaSliverPolygonCases(model, testCases);
			AddQaSmoothCases(model, testCases);
			AddQaTouchesSelfCases(model, testCases);
			AddQaTouchesOtherCases(model, testCases);
			AddQaTrimmedTextFieldsCases(model, testCases);
			AddQaValidDateValuesCases(model, testCases);
			AddQaValidCoordinateFieldsCases(model, testCases);
			AddQaValidUrlsCases(model, testCases);
			AddQaVertexCoincidenceCases(model, testCases);
			AddQaVertexCoincidenceOtherCases(model, testCases);
			AddQaVertexCoincidenceSelfCases(model, testCases);
			AddQaWithinBoxCases(model, testCases);
			AddQaZDifferenceOtherCases(model, testCases);
			AddQaZDifferenceSelfCases(model, testCases);

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
					ReflectionCompare.RecursiveReflectionCompare(testsOrig[0], testsNew[0]);

				foreach (KeyValuePair<Type, MemberInfo> difference in differences)
				{
					Console.WriteLine("Difference: {0} {1}", difference.Key.Name,
					                  difference.Value.Name);
				}

				Assert.AreEqual(0, differences.Count,
				                $"Differences found for {testType.Name} constructor index {constructorIdx}:");
			}
		}

		private static void AddQaContainedPointsCountCases(InMemoryTestDataModel model,
		                                        ICollection<TestDefinitionCase> testCases)
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

		//private static void AddQaCentroidsCases(InMemoryTestDataModel model,
		//                                        ICollection<TestDefinitionCase> testCases)
		//{
		//	testCases.Add(new TestDefinitionCase(typeof(QaCentroids), 0,
		//	                                     new object[]
		//	                                     {
		//		                                     model.GetVectorDataset(),
		//		                                     model.GetVectorDataset()
		//										 }));
		//	testCases.Add(new TestDefinitionCase(typeof(QaCentroids), 1,
		//	                                     new object[]
		//	                                     {
		//		                                     model.GetVectorDataset(),
		//		                                     model.GetVectorDataset(),
		//											 "B.ObjektArt = x AND L.ObjektArt = R.ObjektArt"
		//										 }));
		//	testCases.Add(new TestDefinitionCase(typeof(QaCentroids), 2,
		//	                                     new object[]
		//	                                     {
		//											 new[]
		//											 {
		//											 model.GetVectorDataset(),
		//		                                     model.GetVectorDataset(),
		//											 },
		//											 new[]
		//											 {
		//											 model.GetVectorDataset(),
		//											 model.GetVectorDataset(),
		//											 }
		//										 }));
		//	testCases.Add(new TestDefinitionCase(typeof(QaCentroids), 3,
		//	                                     new object[]
		//	                                     {
		//		                                     new[]
		//		                                     {
		//			                                     model.GetVectorDataset(),
		//			                                     model.GetVectorDataset(),
		//		                                     },
		//		                                     new[]
		//		                                     {
		//			                                     model.GetVectorDataset(),
		//			                                     model.GetVectorDataset(),
		//		                                     },
		//											 "B.ObjektArt = x AND L.ObjektArt = R.ObjektArt"
		//										 }));
		//}

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

		private static void AddQaEdgeMatchBorderingLinesCases(InMemoryTestDataModel model,
		                                                      ICollection<TestDefinitionCase> testCases)
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
			optionalValues.Add("AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled", false);

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

		private static void AddQaEdgeMatchBorderingPointsCases(InMemoryTestDataModel model,
															  ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("PointClass1BorderMatchCondition", "POINT.STATE_ID = BORDER.STATE_ID");
			optionalValues.Add("PointClass2BorderMatchCondition", "POINT.STATE_ID = BORDER.STATE_ID");
			optionalValues.Add("BorderingPointMatchCondition", "POINT1.STATE_ID <> POINT2.STATE_ID");
			optionalValues.Add("BorderingPointAttributeConstraint", "POINT1.TYPE = POINT2.TYPE");
			optionalValues.Add("IsBorderingPointAttributeConstraintSymmetric", false);
			optionalValues.Add("BorderingPointEqualAttributes", "FIELD1,FIELD2:#,FIELD3");
			optionalValues.Add("BorderingPointEqualAttributeOptions",
							   new[] { "FIELD_NAME:OPTION1", "FIELD_NAME:OPTION2" });
			optionalValues.Add("ReportIndividualAttributeConstraintViolations", false);
			optionalValues.Add("CoincidenceTolerance", 1);
			optionalValues.Add("AllowDisjointCandidateFeatureIfBordersAreNotCoincident", false);
			optionalValues.Add("AllowNoFeatureWithinSearchDistance", false);
			optionalValues.Add("AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled", false);

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

		private static void AddQaEdgeMatchCrossingAreasCases(InMemoryTestDataModel model,
															  ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("AreaClass1BorderMatchCondition", "AREA.STATE_ID = BORDER.STATE_ID");
			optionalValues.Add("AreaClass1BoundingFeatureMatchCondition", "AREA.STATE_ID = BOUNDINGFEATURE.STATE_ID");
			optionalValues.Add("AreaClass2BoundingFeatureMatchCondition", "AREA.STATE_ID = BOUNDINGFEATURE.STATE_ID");
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
			optionalValues.Add("AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled", false);

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

		private static void AddQaEdgeMatchCrossingLinesCases(InMemoryTestDataModel model,
															  ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("MinimumErrorConnectionLineLength", 0);
			optionalValues.Add("MaximumEndPointConnectionDistance", 0);
			optionalValues.Add("LineClass1BorderMatchCondition", "LINE.STATE_ID = BORDER.STATE_ID");
			optionalValues.Add("LineClass2BorderMatchCondition", "LINE.STATE_ID = BORDER.STATE_ID");
			optionalValues.Add("CrossingLineMatchCondition", "LINE1.STATE_ID <> LINE2.STATE_ID");
			optionalValues.Add("CrossingLineAttributeConstraint", "LINE1.WIDTH_CLASS = LINE2.WIDTH_CLASS");
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
			optionalValues.Add("IgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance", false);
			optionalValues.Add("AllowEndPointsConnectingToInteriorOfValidNeighborLine", false);
			optionalValues.Add("IgnoreEndPointsOfBorderingLines", false);
			optionalValues.Add("AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled", false);

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
			InMemoryTestDataModel model, ICollection<TestDefinitionCase> testCases)
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

		private static void AddQaFullCoincidenceCases(InMemoryTestDataModel model,
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
			InMemoryTestDataModel model, ICollection<TestDefinitionCase> testCases)
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

		private static void AddQaGroupConstraintsCases(
			InMemoryTestDataModel model, ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("ExistsRowGroupFilters", new[] { "String1" });

			testCases.Add(new TestDefinitionCase(typeof(QaGroupConstraints), 0,
			                                     new object[]
			                                     { model.GetObjectDataset(),
													 "FIELD1 + '#' +FIELD2",
													 "FIELD1 + '#' +FIELD2",
													 1,
													 false},
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
													 false},
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
				                                     false},
			                                     optionalValues));
		}

		private static void AddQaInteriorIntersectsOtherCases(InMemoryTestDataModel model,
		                                        ICollection<TestDefinitionCase> testCases)
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

		private static void AddQaInteriorIntersectsSelfCases(InMemoryTestDataModel model,
												ICollection<TestDefinitionCase> testCases)
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

		private static void AddQaInteriorRingsCases(InMemoryTestDataModel model,
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

		private static void AddQaIntersectionMatrixOtherCases(InMemoryTestDataModel model,
		                                                     ICollection<TestDefinitionCase> testCases)
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

		private static void AddQaIntersectionMatrixSelfCases(InMemoryTestDataModel model,
															 ICollection<TestDefinitionCase> testCases)
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

		private static void AddQaIntersectsOtherCases(InMemoryTestDataModel model,
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

		private static void AddQaIntersectsSelfCases(InMemoryTestDataModel model,
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

		private static void AddQaIsCoveredByOtherCases(InMemoryTestDataModel model,
											   ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>
		{
			{ "CoveringClassTolerances", new[] { 1 } },
			{ "ValidUncoveredGeometryConstraint", "$Area < 5 AND $VertexCount < 10" }
		};

			var polygonDataset1 = model.GetPolygonDataset();
			var polygonDataset2 = model.GetPolygonDataset();

			testCases.Add(new TestDefinitionCase(typeof(QaIsCoveredByOther), 0,
												 new object[]
												 {
													new[] { polygonDataset1, polygonDataset2 }, // Covering
													new[] { polygonDataset1, polygonDataset2 } // Covered
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
													new[] { polygonDataset1, polygonDataset2 }, // Covering
													new[] { polygonDataset1, polygonDataset2 }, // Covered
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
													new[] { polygonDataset1, polygonDataset2 }, // Covering
													new GeometryComponent[] { GeometryComponent.EntireGeometry }, // CoveringGeometryComponents
													new[] { polygonDataset1, polygonDataset2 }, // Covered
													new GeometryComponent[] { GeometryComponent.EntireGeometry }, // CoveredGeometryComponents
													"G1" // Single isCoveringCondition
												 },
												 optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaIsCoveredByOther), 5,
												 new object[]
												 {
													new[] { polygonDataset1, polygonDataset2 }, // Covering
													new GeometryComponent[] { GeometryComponent.EntireGeometry }, // CoveringGeometryComponents
													new[] { polygonDataset1, polygonDataset2 }, // Covered
													new GeometryComponent[] { GeometryComponent.EntireGeometry }, // CoveredGeometryComponents
													"G1",  // isCoveringCondition
													10.0 // AllowedUncoveredPercentage
												 },
												 optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaIsCoveredByOther), 6,
												 new object[]
												 {
													new[] { polygonDataset1, polygonDataset2 }, // Covering
													new GeometryComponent[] { GeometryComponent.EntireGeometry }, // CoveringGeometryComponents
													new[] { polygonDataset1, polygonDataset2 }, // Covered
													new GeometryComponent[] { GeometryComponent.EntireGeometry }, // CoveredGeometryComponents
													new[] { "G1" }, // isCoveringCondition
													0.45 // AllowedUncoveredPercentage
												 },
												 optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaIsCoveredByOther), 7,
												 new object[]
												 {
													new[] { polygonDataset1, polygonDataset2 }, // Covering
													new GeometryComponent[] { GeometryComponent.EntireGeometry }, // CoveringGeometryComponents
													new[] { polygonDataset1, polygonDataset2 }, // Covered
													new GeometryComponent[] { GeometryComponent.EntireGeometry }, // CoveredGeometryComponents
													new[] { "G1" }, // isCoveringCondition
													0.45, // AllowedUncoveredPercentage
													new[] { polygonDataset1, polygonDataset2 } // AreaOfInterestClasses
												 },
												 optionalValues));
		}

		private static void AddQaLineGroupConstraintsCases(InMemoryTestDataModel model,
		                                                     ICollection<TestDefinitionCase> testCases)
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

		private static void AddQaLineIntersectCases(InMemoryTestDataModel model,
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

		private static void AddQaLineIntersectAngleCases(InMemoryTestDataModel model,
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

		private static void AddQaLineIntersectZCases(InMemoryTestDataModel model,
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

		private static void AddQaMaxSlopeCases(InMemoryTestDataModel model,
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


		private static void AddQaMinAngleCases(InMemoryTestDataModel model,
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

		private static void AddQaMinSegAngleCases(InMemoryTestDataModel model,
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

		private static void AddQaMonotonicZCases(InMemoryTestDataModel model,
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

		private static void AddQaMpNonIntersectingRingFootprintsCases(InMemoryTestDataModel model,
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

		private static void AddQaMpSinglePartFootprintCases(InMemoryTestDataModel model,
			ICollection<TestDefinitionCase> testCases)
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

		private static void AddQaMpVertexNotNearFaceCases(InMemoryTestDataModel model,
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

		private static void AddQaMustBeNearOtherCases(InMemoryTestDataModel model,
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

		private static void AddQaMustIntersectMatrixOtherCases(InMemoryTestDataModel model,
		                                              ICollection<TestDefinitionCase> testCases)
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

		private static void AddQaNoBoundaryLoopsCases(InMemoryTestDataModel model,
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

		private static void AddQaNodeLineCoincidenceCases(InMemoryTestDataModel model,
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
														 1,1
				                                     },
													 1,
				                                     false,
				                                     false
			                                     }, optionalValues));
		}

		private static void AddQaNoGapsCases(InMemoryTestDataModel model,
														ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("CoincidenceTolerance", 1);

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
		private static void AddQaPartCoincidenceOtherCases(InMemoryTestDataModel model,
		                                              ICollection<TestDefinitionCase> testCases)
		{
			var optionalValues = new Dictionary<string, object>();
			optionalValues.Add("IgnoreNeighborCondition", "G1.CountryCode <> G2.CountryCode");

			testCases.Add(new TestDefinitionCase(typeof(QaPartCoincidenceOther), 0,
												 new object[]
												 {
													 model.GetPolygonDataset(),
													 model.GetVectorDataset(),
													 1.1, 2.2,true
												 }, optionalValues));
			testCases.Add(new TestDefinitionCase(typeof(QaPartCoincidenceOther), 1,
			                                     new object[]
			                                     {
				                                     model.GetPolygonDataset(),
				                                     model.GetVectorDataset(),
				                                     1.1, 2.2,true,200000.0
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
				                                     1.1, 2.2,3.3,true, 200000.0,0
			                                     }, optionalValues));
		}

		private static void AddQaPartCoincidenceSelfCases(InMemoryTestDataModel model,
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

		private static void AddQaRegularExpressionCases(InMemoryTestDataModel model,
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

		private static void AddQaSliverPolygonCases(InMemoryTestDataModel model,
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

		private static void AddQaSmoothCases(InMemoryTestDataModel model,
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

		private static void AddQaTouchesOtherCases(InMemoryTestDataModel model,
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

		private static void AddQaTouchesSelfCases(InMemoryTestDataModel model,
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

		private static void AddQaTrimmedTextFieldsCases(InMemoryTestDataModel model,
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

		private static void AddQaValidDateValuesCases(InMemoryTestDataModel model,
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

		private static void AddQaValidCoordinateFieldsCases(InMemoryTestDataModel model,
		                                                    ICollection<TestDefinitionCase> testCases)
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
													 "XCoordinate", "YCoordinate", "ZCoordinate", 1, 1, "de-CH"
			                                     }, optionalValues));
		}

		private static void AddQaValidUrlsCases(InMemoryTestDataModel model,
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

		private static void AddQaVertexCoincidenceCases(InMemoryTestDataModel model,
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

		private static void AddQaVertexCoincidenceOtherCases(InMemoryTestDataModel model,
		                                                     ICollection<TestDefinitionCase> testCases)
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

		private static void AddQaVertexCoincidenceSelfCases(InMemoryTestDataModel model,
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

		private static void AddQaZDifferenceOtherCases(InMemoryTestDataModel model,
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

		private static void AddQaWithinBoxCases(InMemoryTestDataModel model,
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

		private static void AddQaZDifferenceSelfCases(InMemoryTestDataModel model,
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
			if (testInstanceInfo.InstanceType.Name != "QaExportTables")
			{
				Assert.Greater(testInstanceInfo.TestCategories.Length, 0);
			}

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
			if (testInstanceInfo.InstanceType.Name != "QaExportTables")
			{
				Assert.Greater(instanceDefInfo.TestCategories.Length, 0);
			}

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

		public static List<KeyValuePair<Type, MemberInfo>> RecursiveReflectionCompare<T>(
			T first, T second)
			where T : class
		{
			var differences = new List<KeyValuePair<Type, MemberInfo>>();

			var parentType = first.GetType();

			void CompareObject(object obj1, object obj2, MemberInfo info)
			{
				if (! obj1.Equals(obj2))
				{
					differences.Add(new KeyValuePair<Type, MemberInfo>(parentType, info));
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

					differences.Concat(RecursiveReflectionCompare(value1, value2));
				}
			}

			// Additionally, compare private fields with primitive types:
			foreach (FieldInfo fieldInfo in parentType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
			{
				object value1 = fieldInfo.GetValue(first);
				object value2 = fieldInfo.GetValue(second);

				if (fieldInfo.FieldType == typeof(string))
				{
					if (string.IsNullOrEmpty(value1 as string) !=
					    string.IsNullOrEmpty(value2 as string))
					{
						CompareObject(value1, value2, fieldInfo);
					}
				}
				else if (fieldInfo.FieldType.IsPrimitive)
				{
					CompareObject(value1, value2, fieldInfo);
				}

				// TODO: Consider checking also non-primitive fields

				//CompareObject(value1, value2, fieldInfo);
			}

			return differences;
		}
	}
}
