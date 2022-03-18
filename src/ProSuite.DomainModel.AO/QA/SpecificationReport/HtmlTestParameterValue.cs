using System;
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
			TestParameter = testParameter;

			Name = testParameterValue.TestParameterName;

			if (testParameterValue is ScalarTestParameterValue scalarParameterValue)
			{
				Value = scalarParameterValue.GetDisplayValue();
				return;
			}

			if (testParameterValue is DatasetTestParameterValue datasetParameterValue)
			{
				IsDatasetParameter = true;
				FilterExpression = datasetParameterValue.FilterExpression;
				UsedAsReferenceData = datasetParameterValue.UsedAsReferenceData;

				Dataset dataset = datasetParameterValue.DatasetValue;
				if (dataset != null)
				{
					Dataset = dataset.Name;
					DataModel = dataset.Model?.Name;
					DatasetGeometryType = HtmlDataset.GetGeometryTypeName(dataset);
					Value = dataset.Name;
				}

				DatasetValue = dataset;

				return;
			}

			throw new ArgumentException($"Unhandled type {testParameterValue.GetType()}");
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
