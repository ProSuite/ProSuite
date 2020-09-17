using NUnit.Framework;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.Core.Test.QA
{
	[TestFixture]
	public class DatasetTestParameterValueTest
	{
		[Test]
		public void CanClone()
		{
			string parameterName = "p1Name";

			var dataset = new ErrorLineDataset("LineDataset");

			var testParameterValue =
				new DatasetTestParameterValue(parameterName, typeof(IDummyDatasetType));

			testParameterValue.DatasetValue = dataset;
			testParameterValue.FilterExpression = "filterExp";
			testParameterValue.UsedAsReferenceData = true;

			DatasetTestParameterValue
				clone = (DatasetTestParameterValue) testParameterValue.Clone();

			Assert.IsTrue(clone.Equals(testParameterValue));
			Assert.IsTrue(testParameterValue.DataType == clone.DataType);

			Assert.IsTrue(dataset.Equals(clone.DatasetValue));
		}

		private interface IDummyDatasetType { }
	}
}