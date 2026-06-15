using System.Drawing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.DataModel.ResourceLookup;
using ProSuite.UI.Core.QA.ResourceLookup;

namespace ProSuite.UI.Core.QA
{
	public class ParameterValueListItem
	{
		private static readonly Image _empty = new Bitmap(2, 2);

		[NotNull] private readonly TestParameterValue _paramValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParameterValueListItem"/> class.
		/// </summary>
		/// <param name="paramValue">The param value.</param>
		public ParameterValueListItem([NotNull] TestParameterValue paramValue)
		{
			Assert.ArgumentNotNull(paramValue, nameof(paramValue));

			_paramValue = paramValue;

			var datasetParameterValue = _paramValue as DatasetTestParameterValue;
			if (datasetParameterValue != null)
			{
				IsDataset = true;
				FilterExpression = datasetParameterValue.FilterExpression;
				UsedAsReferenceData = datasetParameterValue.UsedAsReferenceData
					                      ? "Yes"
					                      : "No";

				Dataset dataset = datasetParameterValue.DatasetValue;
				if (dataset != null)
				{
					DatasetType = DatasetTypeImageLookup.GetImage(dataset);

					Value = dataset.Name;
					ModelName = dataset.Model?.Name;
				}
				else if (datasetParameterValue.ValueSource != null)
				{
					TransformerConfiguration transformerConfig = datasetParameterValue.ValueSource;

					DatasetType = TestTypeImageLookup.GetImage(transformerConfig);

					Value = transformerConfig?.Name;
				}
				else
				{
					DatasetType = _empty;
					Value = "<unknown>";
				}

				return;
			}

			var scalarParameterValue = _paramValue as ScalarTestParameterValue;
			if (scalarParameterValue != null)
			{
				IsDataset = false;

				DatasetType = _empty;

				Value = scalarParameterValue.GetDisplayValue();

				return;
			}

			// Dummy scalar:
			Value = _paramValue.StringValue;
		}

		[UsedImplicitly]
		public string ParameterName => _paramValue.TestParameterName;

		[UsedImplicitly]
		public Image DatasetType { get; }

		[UsedImplicitly]
		public string Value { get; }

		[UsedImplicitly]
		public string ModelName { get; }

		[UsedImplicitly]
		public string FilterExpression { get; }

		public bool IsDataset { get; }

		[UsedImplicitly]
		public string UsedAsReferenceData { get; }
	}
}
