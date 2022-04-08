using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.DatasetCategories
{
	public class DatasetCategoryTableRow : SelectableTableRow, IEntityRow
	{
		[NotNull] private readonly DatasetCategory _entity;
		[NotNull] private static readonly Image _image = Resources.DatasetCategoryItem;

		/// <summary>
		/// Initializes a new instance of the <see cref="DatasetCategoryTableRow"/> class.
		/// </summary>
		/// <param name="entity">The dataset category.</param>
		public DatasetCategoryTableRow([NotNull] DatasetCategory entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;
		}

		[DisplayName("")]
		[UsedImplicitly]
		public Image Image => _image;

		[UsedImplicitly]
		public string Name => _entity.Name;

		[UsedImplicitly]
		public string Abbreviation => _entity.Abbreviation;

		[UsedImplicitly]
		[ColumnConfiguration(
			MinimumWidth = 100,
			AutoSizeColumnMode = DataGridViewAutoSizeColumnMode.Fill,
			WrapMode = TriState.True)]
		public string Description => _entity.Description;

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
		public DatasetCategory DatasetCategory => _entity;

		Entity IEntityRow.Entity => _entity;
	}
}
