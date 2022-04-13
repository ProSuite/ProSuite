using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.UI.DataModel.ResourceLookup;
using ProSuite.UI.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public class QualityConditionDatasetTableRow : IEntityRow
	{
		[NotNull] private readonly QualityCondition _qualityCondition;

		private QualityConditionDatasetTableRow(
			[NotNull] QualityCondition qualityCondition,
			int qualitySpecificationRefCount)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			_qualityCondition = qualityCondition;
			QualitySpecificationRefCount = qualitySpecificationRefCount;

			TestTypeImage = TestTypeImageLookup.GetImage(qualityCondition);
			TestTypeImage.Tag = TestTypeImageLookup.GetDefaultSortIndex(qualityCondition);

			Name = qualityCondition.Name;
			Description = qualityCondition.Description;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityConditionDatasetTableRow"/> class.
		/// </summary>
		/// <param name="qualityCondition">The quality condition.</param>
		/// <param name="parameterValue">The parameter value.</param>
		/// <param name="qualitySpecificationRefCount">The number of quality specifications that reference this
		/// quality condition.</param>
		public QualityConditionDatasetTableRow(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] DatasetTestParameterValue parameterValue,
			int qualitySpecificationRefCount)
			: this(qualityCondition, qualitySpecificationRefCount)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(parameterValue, nameof(parameterValue));

			TestName = _qualityCondition.TestDescriptor.Name;

			Dataset dataset = parameterValue.DatasetValue;
			FilterExpression = parameterValue.FilterExpression;
			UsedAsReferenceData = parameterValue.UsedAsReferenceData
				                      ? "Yes"
				                      : "No";

			if (dataset != null)
			{
				DatasetName = dataset.Name;
				ModelName = dataset.Model.Name;

				if (dataset.DatasetCategory != null)
				{
					DatasetCategory = dataset.DatasetCategory.Name;
				}

				DatasetImage = DatasetTypeImageLookup.GetImage(dataset);
				DatasetImage.Tag = DatasetTypeImageLookup.GetDefaultSortIndex(dataset);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityConditionDatasetTableRow"/> class.
		/// </summary>
		/// <param name="qualityCondition">The quality condition.</param>
		/// <param name="errorText">The error text.</param>
		/// <param name="qualitySpecificationRefCount">The number of quality specifications that reference this
		/// quality condition.</param>
		public QualityConditionDatasetTableRow(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] string errorText,
			int qualitySpecificationRefCount)
			: this(qualityCondition, qualitySpecificationRefCount)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNullOrEmpty(errorText, nameof(errorText));

			TestName = $"Error: {errorText}";

			const string invalid = "<INVALID>";
			DatasetName = invalid;
			ModelName = invalid;
			DatasetCategory = invalid;
			FilterExpression = invalid;
		}

		[UsedImplicitly]
		public Image TestTypeImage { get; }

		[DisplayName("Quality Condition")]
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
		public int QualitySpecificationRefCount { get; }

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

		[DisplayName("Test")]
		[UsedImplicitly]
		public string TestName { get; }

		[DisplayName("Url")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public string Url => _qualityCondition.Url;

		[DisplayName("Created")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? CreatedDate => _qualityCondition.CreatedDate;

		[DisplayName("Created By")]
		[ColumnConfiguration(Width = 80)]
		[UsedImplicitly]
		public string CreatedByUser => _qualityCondition.CreatedByUser;

		[DisplayName("Last Changed")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? LastChangedDate => _qualityCondition.LastChangedDate;

		[DisplayName("Last Changed By")]
		[ColumnConfiguration(MinimumWidth = 90)]
		[UsedImplicitly]
		public string LastChangedByUser => _qualityCondition.LastChangedByUser;

		#region IEntityRow Members

		[Browsable(false)]
		public Entity Entity => _qualityCondition;

		#endregion
	}
}
