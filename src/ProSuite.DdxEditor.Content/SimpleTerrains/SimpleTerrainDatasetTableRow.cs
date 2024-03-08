using System;
using System.ComponentModel;
using System.Drawing;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.SimpleTerrains
{
	public class SimpleTerrainDatasetTableRow : SelectableTableRow, IEntityRow
	{
		private readonly SimpleTerrainDataset _entity;
		[NotNull] private static readonly Image _image = Resources.DatasetTypeSurface;

		public SimpleTerrainDatasetTableRow([NotNull] SimpleTerrainDataset entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;
		}

		Entity IEntityRow.Entity => _entity;

		[DisplayName("")]
		[UsedImplicitly]
		public Image Image => _image;

		[DisplayName("Simple Terrain Name")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public string Name => _entity.Name;

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
		public SimpleTerrainDataset SimpleTerrainDataset => _entity;
	}
}
