using System;
using System.ComponentModel;
using System.Drawing;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.Core.DataModel.ResourceLookup;

namespace ProSuite.DdxEditor.Content.AssociationEnds
{
	public class AssociationEndTableRow : SelectableTableRow, IEntityRow
	{
		private readonly AssociationEnd _entity;
		private readonly Image _image;

		/// <summary>
		/// Initializes a new instance of the <see cref="AssociationEndTableRow"/> class.
		/// </summary>
		/// <param name="entity">The association end.</param>
		public AssociationEndTableRow([NotNull] AssociationEnd entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;

			_image = AssociationEndImageLookup.GetImage(entity);
		}

		Entity IEntityRow.Entity => _entity;

		[DisplayName("")]
		public Image Image => _image;

		public string Name => _entity.Name;

		[DisplayName("Copy Policy")]
		public string CopyPolicy => _entity.CopyPolicy.ToString();

		[DisplayName("Document Association Edit")]
		public string DocumentAssociationEdit =>
			GetBooleanAsString(_entity.DocumentAssociationEdit);

		[DisplayName("Cascade Deletion")]
		public string CascadeDeletion => GetBooleanAsString(_entity.CascadeDeletion);

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
		public AssociationEnd AssociationEnd => _entity;
	}
}
