using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class QualitySpecificationInCategoryTableRow : IEntityRow
	{
		[NotNull] private readonly QualitySpecification _entity;
		[NotNull] private readonly string _hiddenText;
		[NotNull] private readonly Image _image;

		/// <summary>
		/// Initializes a new instance of the <see cref="QualitySpecificationInCategoryTableRow"/> class.
		/// </summary>
		/// <param name="entity">The quality specification.</param>
		public QualitySpecificationInCategoryTableRow([NotNull] QualitySpecification entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;
			_hiddenText = entity.Hidden
				              ? "Yes"
				              : "No";
			_image = QualitySpecificationImageLookup.GetImage(entity);
		}

		[DisplayName("")]
		[NotNull]
		[UsedImplicitly]
		public Image Image => _image;

		[UsedImplicitly]
		public string Name => _entity.Name;

		[CanBeNull]
		[ColumnConfiguration(MinimumWidth = 100,
		                     AutoSizeColumnMode = DataGridViewAutoSizeColumnMode.Fill)]
		[UsedImplicitly]
		public string Description => _entity.Description;

		[DisplayName("Tile Size")]
		[UsedImplicitly]
		public double? TileSize => _entity.TileSize;

		[DisplayName("Display List Order")]
		[UsedImplicitly]
		public int ListOrder => _entity.ListOrder;

		[UsedImplicitly]
		public string Hidden => _hiddenText;

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
		public QualitySpecification QualitySpecification => _entity;

		Entity IEntityRow.Entity => _entity;
	}
}
