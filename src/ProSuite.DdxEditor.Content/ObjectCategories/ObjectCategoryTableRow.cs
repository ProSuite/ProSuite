using System;
using System.ComponentModel;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.TableRows;

namespace ProSuite.DdxEditor.Content.ObjectCategories
{
	public class ObjectCategoryTableRow : SelectableTableRow, IEntityRow
	{
		private readonly ObjectCategory _entity;

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectCategoryTableRow"/> class.
		/// </summary>
		/// <param name="entity">The object category.</param>
		public ObjectCategoryTableRow([NotNull] ObjectCategory entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;
		}

		[UsedImplicitly]
		public string Name => _entity.Name;

		[DisplayName("Subtype Code")]
		[UsedImplicitly]
		public int SubtypeCode => _entity.SubtypeCode;

		[DisplayName("Minimum segment length")]
		[UsedImplicitly]
		public string MinimumSegmentLength =>
			_entity.MinimumSegmentLengthOverride.HasValue
				? _entity.MinimumSegmentLengthOverride.ToString()
				: string.Empty;

		[ColumnConfiguration(MinimumWidth = 200)]
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

		#region IEntityRow Members

		Entity IEntityRow.Entity => _entity;

		#endregion
	}
}
