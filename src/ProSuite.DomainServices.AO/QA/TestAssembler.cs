using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA
{
	public class TestAssembler
	{
		[NotNull] private readonly IVerificationContext _verificationContext;
		[NotNull] private readonly IQualityConditionObjectDatasetResolver _datasetResolver;
		[NotNull] private readonly Func<ITest, QualityCondition> _getQualityCondition;
		[CanBeNull] private readonly Predicate<VectorDataset> _isRelevantVectorDataset;

		public TestAssembler(
			[NotNull] IVerificationContext verificationContext,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
			[NotNull] Func<ITest, QualityCondition> getQualityCondition,
			[CanBeNull] Predicate<VectorDataset> isRelevantVectorDataset)
		{
			_verificationContext = verificationContext;
			_datasetResolver = datasetResolver;
			_getQualityCondition = getQualityCondition;
			_isRelevantVectorDataset = isRelevantVectorDataset;
		}

		internal IList<ITest> AssembleTests(
			[NotNull] IEnumerable<ITest> tests,
			[CanBeNull] AreaOfInterest areaOfInterest,
			bool filterTableRowsUsingRelatedGeometry,
			[NotNull] out IList<TestsWithRelatedGeometry> testsWithRelatedGeometry)
		{
			IList<ITest> containerTests = AssembleTests(
				tests, areaOfInterest,
				filterTableRowsUsingRelatedGeometry,
				out List<ITest> testsToVerifyByRelatedGeometry);

			testsWithRelatedGeometry = FindTestsWithRelatedGeometry(
				testsToVerifyByRelatedGeometry, _datasetResolver,
				out IList<ITest> testsWithoutGeometry);

			foreach (ITest test in testsWithoutGeometry)
			{
				containerTests.Add(test);
			}

			return containerTests;
		}

		[NotNull]
		private IList<ITest> AssembleTests([NotNull] IEnumerable<ITest> tests,
		                                   [CanBeNull] AreaOfInterest areaOfInterest,
		                                   bool filterTableRowsUsingRelatedGeometry,
		                                   [NotNull] out List<ITest> testsToVerifyByRelatedGeometry)
		{
			Assert.ArgumentNotNull(tests, nameof(tests));

			List<ITest> containerTests = new List<ITest>();

			testsToVerifyByRelatedGeometry = new List<ITest>();

			foreach (ITest test in tests)
			{
				// only if none of the involved datasets is a feature class or a terrain
				bool filterByRelatedGeometry =
					filterTableRowsUsingRelatedGeometry &&
					areaOfInterest != null &&
					! _getQualityCondition(test).NeverFilterTableRowsUsingRelatedGeometry &&
					! TestUtils.UsesSpatialDataset(test);

				if (filterByRelatedGeometry)
				{
					// only if none of the involved datasets is a feature class or a terrain
					testsToVerifyByRelatedGeometry.Add(test);
				}
				else
				{
					containerTests.Add(test);
				}
			}

			return containerTests;
		}

		public List<IList<ITest>> BuildTestGroups(
			[NotNull] IList<ITest> tests, int maxProcesses)
		{
			List<IList<ITest>> testGroups = new List<IList<ITest>>();
			if (maxProcesses <= 1)
			{
				testGroups.Add(tests);
				return testGroups;
			}

			TestUtils.ClassifyTests(tests, allowEditing: false, // remark :  is set again later
			                        out IList<ContainerTest> containerTests,
			                        out IList<ITest> nonContainerTests);

			testGroups.Add(nonContainerTests); // TODO: split up?
			int nGroups = maxProcesses - 1;
			for (int iGroup = 0; iGroup < nGroups; iGroup++)
			{
				testGroups.Add(new List<ITest>());
			}

			int group = 0;
			foreach (var test in containerTests)
			{
				testGroups[group + 1].Add(test);
				group++;
				if (group >= maxProcesses - 1)
				{
					group = 0;
				}
			}

			return testGroups;
		}

		public List<QualityConditionGroup> BuildQualityConditionGroups(
			[NotNull] IList<ITest> tests,
			[CanBeNull] AreaOfInterest areaOfInterest,
			bool filterTableRowsUsingRelatedGeometry,
			int maxProcesses)
		{
			IList<ITest> containerTests = AssembleTests(
				tests, areaOfInterest, filterTableRowsUsingRelatedGeometry,
				out IList<TestsWithRelatedGeometry> testsWithRelatedGeometry);

			return BuildQcGroups(containerTests, testsWithRelatedGeometry, maxProcesses);
		}

		public static bool CanBeExecutedWithTileThreads([NotNull]ITest test)
		{
			return CanBeExecutedWithTileThreads(test.GetType());
		}

		public static bool CanBeExecutedWithTileThreads([NotNull] Type testType)
		{
			Type ct = typeof(ContainerTest);
			if (! ct.IsAssignableFrom(testType))
			{
				return false;
			}

			if (Overrides(testType, ct, "CompleteTileCore")
			    || Overrides(testType, ct, "BeginTileCore"))
			{
				return false;
			}

			return true;
		}

		private static bool Overrides(Type type, Type baseType, string methodName)
		{
			MethodInfo method = type.GetMethod(
				methodName,
				BindingFlags.Instance | BindingFlags.NonPublic);

			return method?.DeclaringType != baseType;
		}

		internal List<QualityConditionGroup> BuildQcGroups(
			[NotNull] IList<ITest> tests,
			[NotNull] IList<TestsWithRelatedGeometry> testsWithRelatedGeom,
			int maxProcesses)
		{
			Dictionary<QualityCondition, IList<ITest>> qcTests =
				new Dictionary<QualityCondition, IList<ITest>>();
			Dictionary<ITest, QualityCondition> testQc = new Dictionary<ITest, QualityCondition>();

			List<IList<ITest>> testEnums = new List<IList<ITest>>();
			testEnums.Add(tests);
			testEnums.AddRange(testsWithRelatedGeom.Select(x => x.Tests));

			foreach (IEnumerable<ITest> testEnum in testEnums)
			{
				foreach (ITest test in testEnum)
				{
					QualityCondition qc = _getQualityCondition(test);
					if (! qcTests.TryGetValue(qc, out IList<ITest> qcList))
					{
						qcList = new List<ITest>();
						qcTests.Add(qc, qcList);
					}

					qcList.Add(test);
					testQc[test] = qc;
				}
			}

			TestUtils.ClassifyTests(tests, allowEditing: false, // remark :  is set again later
			                        out IList<ContainerTest> containerTests,
			                        out IList<ITest> nonContainerTests);

			HashSet<QualityCondition> nonContainerQcs = new HashSet<QualityCondition>();
			AddQcs(nonContainerQcs, nonContainerTests, testQc); // TODO: split up?
			// Add geom-related tests to nonContainer tests
			foreach (var relTests in testsWithRelatedGeom)
			{
				AddQcs(nonContainerQcs, relTests.Tests, testQc);
			}

			HashSet<QualityCondition> containerQcs = new HashSet<QualityCondition>();
			foreach (ContainerTest test in containerTests)
			{
				QualityCondition qc = testQc[test];
				if (! nonContainerQcs.Contains(qc))
				{
					containerQcs.Add(qc);
				}
			}

			List<QualityConditionGroup> testGroups = new List<QualityConditionGroup>();
			testGroups.Add(new QualityConditionGroup(QualityConditionExecType.NonContainer, nonContainerQcs));

			int nGroups = maxProcesses - 1;
			for (int iGroup = 0; iGroup < nGroups; iGroup++)
			{
				testGroups.Add(new QualityConditionGroup(QualityConditionExecType.Container));
			}

			int group = 0;
			int offset = maxProcesses > 1 ? 1 : 0;
			// TODO: do something with nonTileParallelTest and tileParallelTest
			QualityConditionGroup nonTileParallelTest =
				new QualityConditionGroup(QualityConditionExecType.Container);
			QualityConditionGroup tileParallelTest =
				new QualityConditionGroup(QualityConditionExecType.TileParallel);
			foreach (var qc in containerQcs)
			{
				QualityConditionExecType execType = QualityConditionExecType.TileParallel;
				foreach (ITest test in qcTests[qc])
				{
					if (! CanBeExecutedWithTileThreads(test))
					{
						execType = QualityConditionExecType.Container;
					}
				}

				if (execType == QualityConditionExecType.Container)
				{
					nonTileParallelTest.QualityConditions.Add(qc);
				}
				else
				{
					tileParallelTest.QualityConditions.Add(qc);
				}

				testGroups[group + offset].QualityConditions.Add(qc);
				group++;
				if (group >= maxProcesses - 1)
				{
					group = 0;
				}
			}

			return testGroups;
		}

		private void AddQcs(HashSet<QualityCondition> qualityConditions, IEnumerable<ITest> tests,
		                    Dictionary<ITest, QualityCondition> testQc)
		{
			foreach (var test in tests)
			{
				qualityConditions.Add(testQc[test]);
			}
		}

		private IList<TestsWithRelatedGeometry> FindTestsWithRelatedGeometry(
			[NotNull] ICollection<ITest> tests,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
			[NotNull] out IList<ITest> testsWithoutGeometry)
		{
			Assert.ArgumentNotNull(tests, nameof(tests));

			testsWithoutGeometry = new List<ITest>();

			if (tests.Count == 0)
			{
				return new List<TestsWithRelatedGeometry>();
			}

			Dictionary<IReadOnlyTable, IList<ITest>> testsByTable =
				TestUtils.GetTestsByTable(tests);

			var testsWithRelatedGeometry = new List<TestsWithRelatedGeometry>();
			foreach (KeyValuePair<IReadOnlyTable, IList<ITest>> pair in testsByTable)
			{
				IReadOnlyTable table = pair.Key;
				IList<ITest> tableTests = pair.Value;
				TestsWithRelatedGeometry testsWithRelGeom =
					CreateTestsWithRelatedGeometry(table, tableTests, datasetResolver);
				if (testsWithRelGeom?.HasAnyAssociationsToFeatureClasses == true)
				{
					testsWithRelatedGeometry.Add(testsWithRelGeom);
				}
				else
				{
					foreach (ITest test in tableTests)
					{
						testsWithoutGeometry.Add(test);
					}
				}
			}

			return testsWithRelatedGeometry;
		}

		[CanBeNull]
		private TestsWithRelatedGeometry CreateTestsWithRelatedGeometry(
			[NotNull] IReadOnlyTable table, [NotNull] IList<ITest> tests,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver)
		{
			ITest testWithTable = tests[0];
			IObjectDataset objectDataset = GetInvolvedObjectDataset(
				table, testWithTable, datasetResolver);

			if (objectDataset == null)
			{
				return null;
			}

			var relClassChains = GetRelClassChains(
				table, objectDataset, testWithTable,
				out bool hasAnyAssociationsToFeatureClasses);

			var relClassChainTests =
				new TestsWithRelatedGeometry(table, tests, objectDataset, relClassChains);
			relClassChainTests.HasAnyAssociationsToFeatureClasses =
				hasAnyAssociationsToFeatureClasses;

			return relClassChainTests;
		}

		[CanBeNull]
		private IObjectDataset GetInvolvedObjectDataset(
			[NotNull] IReadOnlyTable table,
			[NotNull] ITest testWithTable,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver)
		{
			return datasetResolver.GetDatasetByGdbTableName(
				table.Name, _getQualityCondition(testWithTable));
		}

		[NotNull]
		private IEnumerable<IList<IRelationshipClass>> GetRelClassChains(
			[NotNull] IReadOnlyTable table,
			[NotNull] IObjectDataset objectDataset,
			[NotNull] ITest testWithTable,
			out bool hasAnyAssociationsToFeatureClasses)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(objectDataset, nameof(objectDataset));
			Assert.ArgumentNotNull(testWithTable, nameof(testWithTable));

			IEnumerable<IList<IRelationshipClass>> relClassChains =
				ReferenceGeometryUtils.GetRelationshipClassChainsToVerifiedFeatureClasses(
					objectDataset, _verificationContext,
					_isRelevantVectorDataset, out hasAnyAssociationsToFeatureClasses);
			return relClassChains;
		}
	}
}
