using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.TableRows;

namespace ProSuite.DdxEditor.Content.QA.Categories
{
	public class DataQualityCategoryTableRow : SelectableTableRow, IEntityRow
	{
		[CanBeNull] private readonly DataQualityCategory _entity;
		[NotNull] private readonly string _qualifiedName;
		[CanBeNull] private readonly string _defaultModelName;
		[NotNull] private static readonly Image _image;

		static DataQualityCategoryTableRow()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.DataQualityCategoryOverlay);
		}

		public DataQualityCategoryTableRow([NotNull] string noCategoryTitle)
		{
			Assert.ArgumentNotNullOrEmpty(noCategoryTitle, nameof(noCategoryTitle));

			_qualifiedName = noCategoryTitle;
		}

		public DataQualityCategoryTableRow([NotNull] DataQualityCategory entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;
			_qualifiedName = entity.GetQualifiedName(" / ");

			if (entity.DefaultModel != null)
			{
				_defaultModelName = entity.DefaultModel.Name;
			}
		}

		[DisplayName("")]
		[NotNull]
		[UsedImplicitly]
		public Image Image => _image;

		[DisplayName("Full Name")]
		[ColumnConfiguration(
			MinimumWidth = 100,
			AutoSizeColumnMode = DataGridViewAutoSizeColumnMode.AllCells)]
		[NotNull]
		[UsedImplicitly]
		public string QualifiedName => _qualifiedName;

		[CanBeNull]
		[UsedImplicitly]
		[ColumnConfiguration(
			MinimumWidth = 100,
			AutoSizeColumnMode = DataGridViewAutoSizeColumnMode.Fill)]
		public string Description => _entity?.Description;

		[DisplayName("Default Data Model")]
		[CanBeNull]
		[UsedImplicitly]
		public string DefaultModelName => _defaultModelName;

		[DisplayName("Created")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? CreatedDate => _entity?.CreatedDate;

		[DisplayName("Created By")]
		[ColumnConfiguration(Width = 80)]
		[UsedImplicitly]
		public string CreatedByUser => _entity?.CreatedByUser;

		[DisplayName("Last Changed")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? LastChangedDate => _entity?.LastChangedDate;

		[DisplayName("Last Changed By")]
		[ColumnConfiguration(MinimumWidth = 90)]
		[UsedImplicitly]
		public string LastChangedByUser => _entity?.LastChangedByUser;

		[CanBeNull]
		[Browsable(false)]
		public DataQualityCategory DataQualityCategory => _entity;

		Entity IEntityRow.Entity => _entity;
	}
}
