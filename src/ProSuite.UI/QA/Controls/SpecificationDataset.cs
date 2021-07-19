using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.DataModel.ResourceLookup;
using ProSuite.UI.Properties;

namespace ProSuite.UI.QA.Controls
{
	public class SpecificationDataset
	{
		private static readonly Image _allowImage =
			(Bitmap) TestTypeImages.TestTypeWarning.Clone();

		private static readonly Image _continueImage =
			(Bitmap) TestTypeImages.TestTypeProhibition.Clone();

		private static readonly Image _errorsImage = (Bitmap) Resources.Error.Clone();
		private static readonly Image _noErrorsImage = (Bitmap) Resources.OK.Clone();

		private static readonly Image _stopImage =
			(Bitmap) TestTypeImages.TestTypeStop.Clone();

		private static readonly Image _warningImage =
			(Bitmap) TestTypeImages.TestTypeWarning.Clone();

		private static readonly SortedList<string, Image> _datasetImageList;

		private readonly DatasetTestParameterValue _datasetTestParameterValue;

		#region Constructors

		static SpecificationDataset()
		{
			ImageList list = DatasetTypeImageLookup.CreateImageList();
			_datasetImageList = new SortedList<string, Image>();

			foreach (string key in list.Images.Keys)
			{
				Image image = Assert.NotNull(list.Images[key], "image");

				var clonedImage = (Image) image.Clone();

				clonedImage.Tag = DatasetTypeImageLookup.GetDefaultSortIndex(key);

				_datasetImageList.Add(key, clonedImage);
			}
		}

		public SpecificationDataset(
			[NotNull] QualitySpecificationElement qualitySpecificationElement)
		{
			QualitySpecificationElement = qualitySpecificationElement;
		}

		public SpecificationDataset(
			[NotNull] QualityConditionVerification qualityConditionVerification)
		{
			QualityConditionVerification = qualityConditionVerification;
		}

		private SpecificationDataset(
			[NotNull] QualitySpecificationElement qualitySpecificationElement,
			[NotNull] DatasetTestParameterValue datasetTestParameterValue)
		{
			QualitySpecificationElement = qualitySpecificationElement;
			_datasetTestParameterValue = datasetTestParameterValue;

			_allowImage.Tag = QualityConditionType.Allowed;
			_continueImage.Tag = QualityConditionType.ContinueOnError;
			_stopImage.Tag = QualityConditionType.StopOnError;

			Dataset dataset = datasetTestParameterValue.DatasetValue;

			if (dataset != null)
			{
				string datasetImageKey = DatasetTypeImageLookup.GetImageKey(dataset);
				DatasetType = _datasetImageList[datasetImageKey];
			}
		}

		[NotNull]
		public static List<SpecificationDataset> CreateList(
			[NotNull] QualitySpecificationElement qualitySpecificationElement)
		{
			Assert.ArgumentNotNull(qualitySpecificationElement,
			                       nameof(qualitySpecificationElement));

			var datasetNames = new List<string>();
			var result = new List<SpecificationDataset>();

			foreach (
				TestParameterValue param in
				qualitySpecificationElement.QualityCondition.ParameterValues)
			{
				if (param is DatasetTestParameterValue == false)
				{
					continue;
				}

				var dsParam = (DatasetTestParameterValue) param;
				if (dsParam.DatasetValue == null)
				{
					continue;
				}

				if (datasetNames.Contains(dsParam.StringValue))
				{
					continue;
				}

				datasetNames.Add(dsParam.StringValue);

				result.Add(new SpecificationDataset(qualitySpecificationElement, dsParam));
			}

			return result;
		}

		#endregion

		[CanBeNull]
		public QualityCondition QualityCondition
			=> QualitySpecificationElement != null
				   ? QualitySpecificationElement.QualityCondition
				   : QualityConditionVerification?.DisplayableCondition;

		public bool Enabled
		{
			get
			{
				return QualitySpecificationElement != null &&
				       QualitySpecificationElement.Enabled;
			}
			set
			{
				QualitySpecificationElement element = Assert.NotNull(QualitySpecificationElement,
				                                                     "quality specification element is null");

				element.Enabled = value;
			}
		}

		[UsedImplicitly]
		public Image Type
		{
			get
			{
				if (QualitySpecificationElement != null)
				{
					if (QualitySpecificationElement.AllowErrors)
					{
						return _allowImage;
					}

					return ! QualitySpecificationElement.StopOnError
						       ? _continueImage
						       : _stopImage;
				}

				QualityConditionVerification verification =
					Assert.NotNull(QualityConditionVerification,
					               "quality condition verification is null");

				if (verification.AllowErrors)
				{
					return _allowImage;
				}

				return ! verification.StopOnError
					       ? _continueImage
					       : _stopImage;
			}
		}

		[UsedImplicitly]
		public Image Status
		{
			get
			{
				if (QualityConditionVerification == null)
				{
					return null;
				}

				if (QualityConditionVerification.ErrorCount == 0)
				{
					return _noErrorsImage;
				}

				return QualityConditionVerification.AllowErrors
					       ? _warningImage
					       : _errorsImage;
			}
		}

		[NotNull]
		[UsedImplicitly]
		public string TestName => QualityCondition == null
			                          ? string.Empty
			                          : QualityCondition.Name;

		[UsedImplicitly]
		public string TestType => QualityCondition == null
			                          ? string.Empty
			                          : QualityCondition.TestDescriptor == null
				                          ? QualityCondition.Description
				                          : QualityCondition.TestDescriptor.Name;

		public string DatasetName
		{
			get
			{
				if (_datasetTestParameterValue == null)
				{
					return null;
				}

				return UseAliasDatasetName
					       ? _datasetTestParameterValue.Alias
					       : _datasetTestParameterValue.StringValue;
			}
		}

		public bool UseAliasDatasetName { get; set; } = true;

		[UsedImplicitly]
		public Image DatasetType { get; }

		[CanBeNull]
		public QualitySpecificationElement QualitySpecificationElement { get; }

		[CanBeNull]
		public QualityConditionVerification QualityConditionVerification { get; }
	}
}
