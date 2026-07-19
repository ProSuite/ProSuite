using System;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.Test.QA
{
	/// <summary>
	/// Regression coverage for the legacy ArcObjects test-parameter type mapping that
	/// <see cref="TestParameterTypeUtils"/> registers with the AO-free core
	/// (<see cref="TestParameterTypes"/>). Guards the GetEmptyParameterValue regression where
	/// legacy dataset parameters were being turned into scalar values.
	/// </summary>
	[TestFixture]
	public class TestParameterTypeUtilsTest
	{
		private enum SampleEnum
		{
			A,
			B
		}

		[OneTimeSetUp]
		public void EnsureRegistered()
		{
			// Explicit, idempotent registration (mirrors the composition roots); the static
			// constructor would do it too on first access.
			TestParameterTypeUtils.EnsureRegistered();
		}

		[Test]
		public void GetParameterType_ResolvesLegacyDatasetTypes()
		{
			Assert.AreEqual(TestParameterType.VectorDataset,
			                TestParameterTypeUtils.GetParameterType(
				                typeof(IReadOnlyFeatureClass)));
			Assert.AreEqual(TestParameterType.VectorDataset,
			                TestParameterTypeUtils.GetParameterType(typeof(IFeatureClass)));
			Assert.AreEqual(TestParameterType.ObjectDataset,
			                TestParameterTypeUtils.GetParameterType(typeof(IReadOnlyTable)));
			Assert.AreEqual(TestParameterType.ObjectDataset,
			                TestParameterTypeUtils.GetParameterType(typeof(ITable)));
		}

		[Test]
		public void GetParameterType_ScalarsAndEnumUnchanged()
		{
			Assert.AreEqual(TestParameterType.Double,
			                TestParameterTypeUtils.GetParameterType(typeof(double)));
			Assert.AreEqual(TestParameterType.Integer,
			                TestParameterTypeUtils.GetParameterType(typeof(int)));
			Assert.AreEqual(TestParameterType.Boolean,
			                TestParameterTypeUtils.GetParameterType(typeof(bool)));
			Assert.AreEqual(TestParameterType.String,
			                TestParameterTypeUtils.GetParameterType(typeof(string)));
			Assert.AreEqual(TestParameterType.DateTime,
			                TestParameterTypeUtils.GetParameterType(typeof(DateTime)));
			Assert.AreEqual(TestParameterType.Integer,
			                TestParameterTypeUtils.GetParameterType(typeof(SampleEnum)));
		}

		[Test]
		public void IsDatasetType_TrueForLegacyDatasetTypes()
		{
			Assert.IsTrue(TestParameterTypeUtils.IsDatasetType(typeof(IReadOnlyFeatureClass)));
			Assert.IsTrue(TestParameterTypeUtils.IsDatasetType(typeof(IFeatureClass)));
			Assert.IsTrue(TestParameterTypeUtils.IsDatasetType(typeof(IReadOnlyTable)));
			Assert.IsTrue(TestParameterTypeUtils.IsDatasetType(typeof(ITable)));
		}

		[Test]
		public void IsDatasetType_FalseForScalars()
		{
			Assert.IsFalse(TestParameterTypeUtils.IsDatasetType(typeof(int)));
			Assert.IsFalse(TestParameterTypeUtils.IsDatasetType(typeof(string)));
		}

		[Test]
		public void GetEmptyParameterValue_ReturnsDatasetValueForLegacyDatasetParams()
		{
			AssertDatasetEmptyValue(typeof(IReadOnlyFeatureClass));
			AssertDatasetEmptyValue(typeof(IFeatureClass));
			AssertDatasetEmptyValue(typeof(ITable));
			AssertDatasetEmptyValue(typeof(IReadOnlyTable));
		}

		[Test]
		public void GetEmptyParameterValue_ReturnsScalarValueForScalarParams()
		{
			var parameter = new TestParameter("count", typeof(int)) { DefaultValue = 42 };

			TestParameterValue value = TestParameterTypeUtils.GetEmptyParameterValue(parameter);

			Assert.IsInstanceOf<ScalarTestParameterValue>(value);
			Assert.AreEqual(42, ((ScalarTestParameterValue) value).GetValue(typeof(int)));
		}

		[Test]
		public void GetEmptyParameterValue_ReturnsScalarValueForEnumParam()
		{
			var parameter = new TestParameter("kind", typeof(SampleEnum));

			TestParameterValue value = TestParameterTypeUtils.GetEmptyParameterValue(parameter);

			Assert.IsInstanceOf<ScalarTestParameterValue>(value);
		}

		private static void AssertDatasetEmptyValue(Type datasetParameterType)
		{
			var parameter = new TestParameter("ds", datasetParameterType);

			TestParameterValue value = TestParameterTypeUtils.GetEmptyParameterValue(parameter);

			Assert.IsInstanceOf<DatasetTestParameterValue>(
				value, "Expected a DatasetTestParameterValue for {0}", datasetParameterType.Name);
		}
	}
}
