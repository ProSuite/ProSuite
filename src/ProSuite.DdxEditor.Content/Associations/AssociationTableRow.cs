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

namespace ProSuite.DdxEditor.Content.Associations
{
	public class AssociationTableRow : SelectableTableRow, IEntityRow
	{
		[NotNull] private readonly Association _entity;
		private readonly AssociationCardinality _cardinality;

		private readonly Image _image;

		/// <summary>
		/// Initializes a new instance of the <see cref="AssociationTableRow"/> class.
		/// </summary>
		/// <param name="entity">The association.</param>
		public AssociationTableRow([NotNull] Association entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;
			_cardinality = entity.Cardinality;

			string imageKey;
			_image = AssociationImageLookup.GetImage(entity, out imageKey);
			_image.Tag = imageKey;
		}

		[DisplayName("")]
		[UsedImplicitly]
		public Image Image => _image;

		[UsedImplicitly]
		public string Name => _entity.Name;

		[UsedImplicitly]
		public AssociationCardinality Cardinality => _cardinality;

		[UsedImplicitly]
		public string Origin => _entity.OriginDataset.UnqualifiedName;

		[UsedImplicitly]
		public string Destination => _entity.DestinationDataset.Name;

		[ColumnConfiguration(Width = 300)]
		[UsedImplicitly]
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
		[NotNull]
		public Association Association => _entity;

		#region IEntityRow Members

		[Browsable(false)]
		[NotNull]
		public Entity Entity => _entity;

		#endregion
	}
}
