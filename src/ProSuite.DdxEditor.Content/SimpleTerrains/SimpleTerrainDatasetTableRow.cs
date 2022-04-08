using System;
using System.ComponentModel;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.SimpleTerrains
{
	public class SimpleTerrainDatasetTableRow : SelectableTableRow, IEntityRow
	{
		private readonly SimpleTerrainDataset _simpleTerrainDataset;

		public SimpleTerrainDatasetTableRow([NotNull] SimpleTerrainDataset simpleTerrainDataset)
		{
			Assert.ArgumentNotNull(simpleTerrainDataset, nameof(simpleTerrainDataset));

			_simpleTerrainDataset = simpleTerrainDataset;
		}

		Entity IEntityRow.Entity => _simpleTerrainDataset;

		[DisplayName("Simple Terrain Name")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public string Name => _simpleTerrainDataset.Name;

		[DisplayName("Created")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? CreatedDate => _simpleTerrainDataset.CreatedDate;

		[DisplayName("Created By")]
		[ColumnConfiguration(Width = 80)]
		[UsedImplicitly]
		public string CreatedByUser => _simpleTerrainDataset.CreatedByUser;

		[DisplayName("Last Changed")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? LastChangedDate => _simpleTerrainDataset.LastChangedDate;

		[DisplayName("Last Changed By")]
		[ColumnConfiguration(MinimumWidth = 90)]
		[UsedImplicitly]
		public string LastChangedByUser => _simpleTerrainDataset.LastChangedByUser;
	}
}