using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Finder;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA;
using ProSuite.UI.Core.QA.BoundTableRows;

namespace ProSuite.UI.QA.PropertyEditors
{
	public abstract class DatasetConfig : ParameterConfig
	{
		private Dataset _dataset;
		private bool _usedAsReferenceData;

		[Editor(typeof(DatasetEditor), typeof(UITypeEditor))]
		[DisplayName("Dataset")]
		public virtual Dataset Data
		{
			get { return _dataset; }
			set
			{
				_dataset = value;
				var dsValue = (DatasetTestParameterValue) GetTestParameterValue();

				if (dsValue == null)
				{
					//dsValue = new DatasetTestParameterValue(null, _dataset);
					//SetTestParameterValue(dsValue);
				}
				else
				{
					var parameterType =
						TestParameterTypeUtils.GetParameterType(Assert.NotNull(dsValue.DataType));

					if (_dataset != null &&
					    ! TestParameterTypeUtils.IsValidDataset(parameterType, _dataset))
					{
						throw new ArgumentException(
							string.Format(
								"Invalid dataset for test parameter type {0}: {1} ({2})",
								Enum.GetName(typeof(TestParameterType), parameterType), _dataset,
								dsValue.TestParameterName));
					}

					dsValue.DatasetValue = _dataset;
				}

				OnDataChanged(null);
			}
		}

		[Description(
			"Indicates that this dataset is used as valid reference data for the quality condition; " +
			"if only reference datasets are loaded in a work context for a given quality condition, the quality condition is not applied"
		)]
		[DisplayName("Used as Reference Data")]
		[DefaultValue(false)]
		[UsedImplicitly]
		public virtual bool UsedAsReferenceData
		{
			get { return _usedAsReferenceData; }
			set
			{
				_usedAsReferenceData = value;

				var dsValue = (DatasetTestParameterValue) GetTestParameterValue();

				if (dsValue != null)
				{
					dsValue.UsedAsReferenceData = _usedAsReferenceData;
				}

				OnDataChanged(null);
			}
		}

		public override void SetTestParameterValue(TestParameterValue parameterValue)
		{
			var dsValue = (DatasetTestParameterValue) parameterValue;

			_dataset = dsValue.DatasetValue;
			_usedAsReferenceData = dsValue.UsedAsReferenceData;

			base.SetTestParameterValue(parameterValue);
		}

		[NotNull]
		public FinderForm<DatasetFinderItem> GetFinderForm()
		{
			var finder = new Finder<DatasetFinderItem>();

			DataQualityCategory category = QualityCondition?.Category;
			DdxModel model = category?.GetDefaultModel();

			return finder.CreateForm(GetFinderQueries(model),
			                         allowMultiSelection: false,
			                         columnDescriptors: null,
			                         filterSettingsContext: FinderContextIds.GetId(category));
		}

		[NotNull]
		private IEnumerable<FinderQuery<DatasetFinderItem>> GetFinderQueries(
			[CanBeNull] DdxModel model)
		{
			if (model != null)
			{
				yield return new FinderQuery<DatasetFinderItem>(
					string.Format("Datasets in {0}", model.Name),
					string.Format("model{0}", model.Id),
					() => GetListItems(model));
			}

			yield return new FinderQuery<DatasetFinderItem>(
				"<All>", "[all]", () => GetListItems());
		}

		[NotNull]
		private IList<DatasetFinderItem> GetListItems([CanBeNull] DdxModel model = null)
		{
			if (DatasetProvider == null)
			{
				return new List<DatasetFinderItem>();
			}

			return DatasetProvider.GetDatasets(TestParameterTypes, model)
			                      .Select(dataset => new DatasetFinderItem(dataset))
			                      .ToList();
		}

		protected abstract TestParameterType TestParameterTypes { get; }
	}
}
