using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AO.QA.SpecificationReport
{
	public class HtmlTestParameterValue
	{
		internal HtmlTestParameterValue([NotNull] TestParameterValue testParameterValue,
		                                [NotNull] HtmlTestParameter testParameter)
		{
			Name = testParameterValue.TestParameterName;
			Value = testParameterValue.StringValue;

			TestParameter = testParameter;
			var datasetParameterValue = testParameterValue as DatasetTestParameterValue;
			if (datasetParameterValue != null)
			{
				Dataset dataset = Assert.NotNull(datasetParameterValue.DatasetValue);
				DatasetValue = dataset;
				Dataset = dataset.Name;
				IsDatasetParameter = true;
				FilterExpression = datasetParameterValue.FilterExpression;
				DataModel = dataset.Model.Name;
				UsedAsReferenceData = datasetParameterValue.UsedAsReferenceData;
				DatasetGeometryType = HtmlDataset.GetGeometryTypeName(dataset);
			}
		}

		[NotNull]
		[UsedImplicitly]
		public string Name { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public string Value { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public HtmlTestParameter TestParameter { get; private set; }

		[UsedImplicitly]
		public bool IsDatasetParameter { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public string Dataset { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public string DatasetGeometryType { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public string DataModel { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public string FilterExpression { get; private set; }

		[UsedImplicitly]
		public bool UsedAsReferenceData { get; private set; }

		[CanBeNull]
		internal Dataset DatasetValue { get; private set; }
	}
}
