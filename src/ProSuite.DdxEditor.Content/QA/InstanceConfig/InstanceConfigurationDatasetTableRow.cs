using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.DataModel.ResourceLookup;
using ProSuite.UI.Core.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public class InstanceConfigurationDatasetTableRow : IEntityRow
	{
		[NotNull] private readonly InstanceConfiguration _instanceConfiguration;

		#region Constructors

		private InstanceConfigurationDatasetTableRow(
			[NotNull] InstanceConfiguration instanceConfiguration,
			int usageCount)
		{
			Assert.ArgumentNotNull(instanceConfiguration, nameof(instanceConfiguration));

			_instanceConfiguration = instanceConfiguration;
			UsageCount = usageCount;

			TestTypeImage = TestTypeImageLookup.GetImage(instanceConfiguration);
			TestTypeImage.Tag = TestTypeImageLookup.GetDefaultSortIndex(instanceConfiguration);

			Name = instanceConfiguration.Name;
			Description = instanceConfiguration.Description;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceConfigurationDatasetTableRow"/> class.
		/// </summary>
		/// <param name="instanceConfiguration">The instance configuration.</param>
		/// <param name="datasetParameterValue">The parameter value.</param>
		/// <param name="usageCount">The number of quality specifications that reference this
		/// instance configuration.</param>
		public InstanceConfigurationDatasetTableRow(
			[NotNull] InstanceConfiguration instanceConfiguration,
			[NotNull] DatasetTestParameterValue datasetParameterValue,
			int usageCount)
			: this(instanceConfiguration, usageCount)
		{
			Assert.ArgumentNotNull(instanceConfiguration, nameof(instanceConfiguration));
			Assert.ArgumentNotNull(datasetParameterValue, nameof(datasetParameterValue));

			InstanceDescriptorName = _instanceConfiguration.InstanceDescriptor.Name;

			Dataset dataset = datasetParameterValue.DatasetValue;
			FilterExpression = datasetParameterValue.FilterExpression;
			UsedAsReferenceData = datasetParameterValue.UsedAsReferenceData
				                      ? "Yes"
				                      : "No";

			DatasetName = datasetParameterValue.GetName();
			ModelName = InstanceConfigurationUtils.GetDatasetModelName(datasetParameterValue);
			DatasetCategory =
				InstanceConfigurationUtils.GetDatasetCategoryName(datasetParameterValue);

			if (dataset != null)
			{
				DatasetImage = DatasetTypeImageLookup.GetImage(dataset);
				DatasetImage.Tag = DatasetTypeImageLookup.GetDefaultSortIndex(dataset);
			}
			else if (datasetParameterValue.ValueSource != null)
			{
				TransformerConfiguration transformer = datasetParameterValue.ValueSource;

				DatasetImage = TestTypeImageLookup.GetImage(transformer);
				DatasetImage.Tag = TestTypeImageLookup.GetDefaultSortIndex(transformer);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceConfigurationDatasetTableRow"/> class.
		/// </summary>
		/// <param name="instanceConfiguration">The instance configuration.</param>
		/// <param name="errorText">The error text.</param>
		/// <param name="usageCount">The number of entities (e.g. quality specifications for quality
		/// conditions) that reference this instance configuration.</param>
		public InstanceConfigurationDatasetTableRow(
			[NotNull] InstanceConfiguration instanceConfiguration,
			[NotNull] string errorText,
			int usageCount)
			: this(instanceConfiguration, usageCount)
		{
			Assert.ArgumentNotNull(instanceConfiguration, nameof(instanceConfiguration));
			Assert.ArgumentNotNullOrEmpty(errorText, nameof(errorText));

			InstanceDescriptorName = $"Error: {errorText}";

			const string invalid = "<INVALID>";
			DatasetName = invalid;
			ModelName = invalid;
			DatasetCategory = invalid;
			FilterExpression = invalid;
		}

		#endregion

		[UsedImplicitly]
		public Image TestTypeImage { get; }

		[DisplayName("Name")]
		[ColumnConfiguration(Width = 400)]
		[UsedImplicitly]
		public string Name { get; }

		[ColumnConfiguration(MinimumWidth = 100,
		                     AutoSizeColumnMode = DataGridViewAutoSizeColumnMode.Fill)]
		[UsedImplicitly]
		public string Description { get; }

		[DisplayName("Usage Count")]
		[ColumnConfiguration(Width = 70)]
		[UsedImplicitly]
		public int UsageCount { get; }

		public Image DatasetImage { get; }

		[DisplayName("Dataset Name")]
		[UsedImplicitly]
		public string DatasetName { get; }

		[DisplayName("Model")]
		[UsedImplicitly]
		public string ModelName { get; }

		[DisplayName("Category")]
		[UsedImplicitly]
		public string DatasetCategory { get; }

		[DisplayName("Filter Expression")]
		[ColumnConfiguration(Width = 200)]
		[UsedImplicitly]
		public string FilterExpression { get; }

		[DisplayName("Reference Data")]
		[UsedImplicitly]
		public string UsedAsReferenceData { get; }

		[DisplayName("Algorithm")]
		[UsedImplicitly]
		public string InstanceDescriptorName { get; }

		[DisplayName("Url")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public string Url => _instanceConfiguration.Url;

		[DisplayName("Created")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? CreatedDate => _instanceConfiguration.CreatedDate;

		[DisplayName("Created By")]
		[ColumnConfiguration(Width = 80)]
		[UsedImplicitly]
		public string CreatedByUser => _instanceConfiguration.CreatedByUser;

		[DisplayName("Last Changed")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? LastChangedDate => _instanceConfiguration.LastChangedDate;

		[DisplayName("Last Changed By")]
		[ColumnConfiguration(MinimumWidth = 90)]
		[UsedImplicitly]
		public string LastChangedByUser => _instanceConfiguration.LastChangedByUser;

		#region IEntityRow Members

		[Browsable(false)]
		public Entity Entity => _instanceConfiguration;

		#endregion
	}
}
