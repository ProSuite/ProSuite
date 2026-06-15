using System;
using System.ComponentModel;
using System.Drawing;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public class ReferencingQualityConditionTableRow : SelectableTableRow, IEntityRow
	{
		private readonly QualityCondition _entity;

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferencingQualityConditionTableRow"/> class.
		/// </summary>
		/// <param name="entity">The quality condition.</param>
		public ReferencingQualityConditionTableRow([NotNull] QualityCondition entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;
		}

		[DisplayName("")]
		[UsedImplicitly]
		public Image Image
		{
			get
			{
				Image image = TestTypeImageLookup.GetImage(_entity);
				image.Tag = TestTypeImageLookup.GetDefaultSortIndex(_entity);

				return image;
			}
		}

		[UsedImplicitly]
		public string Name => _entity.Name;

		[UsedImplicitly]
		[ColumnConfiguration(Width = 300)]
		public string Description => _entity.Description;

		[UsedImplicitly]
		public BooleanOverride AllowErrorsOverride
		{
			get
			{
				return NullableBooleanItems.GetBooleanOverride(
					_entity.AllowErrorsOverride);
			}
			set
			{
				_entity.AllowErrorsOverride =
					NullableBooleanItems.GetNullableBoolean(value);
			}
		}

		[UsedImplicitly]
		public BooleanOverride StopOnErrorOverride
		{
			get
			{
				return
					NullableBooleanItems.GetBooleanOverride(
						_entity.StopOnErrorOverride);
			}
			set
			{
				_entity.StopOnErrorOverride =
					NullableBooleanItems.GetNullableBoolean(value);
			}
		}

		[DisplayName("Created")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? CreatedDate => _entity.CreatedDate;

		[DisplayName("Created By")]
		[ColumnConfiguration(Width = 80)]
		[UsedImplicitly]
		public string CreatedByUser => _entity.CreatedByUser;

		[DisplayName("Last Changed")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? LastChangedDate => _entity.LastChangedDate;

		[DisplayName("Last Changed By")]
		[ColumnConfiguration(MinimumWidth = 90)]
		[UsedImplicitly]
		public string LastChangedByUser => _entity.LastChangedByUser;

		[Browsable(false)]
		public QualityCondition QualityCondition => _entity;

		#region IEntityRow Members

		Entity IEntityRow.Entity => _entity;

		#endregion
	}
}
