using System;
using System.ComponentModel;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.LinearNetworks
{
	public class LinearNetworkTableRow : SelectableTableRow, IEntityRow
	{
		private readonly LinearNetwork _linearNetwork;

		public LinearNetworkTableRow([NotNull] LinearNetwork linearNetwork)
		{
			Assert.ArgumentNotNull(linearNetwork, nameof(linearNetwork));

			_linearNetwork = linearNetwork;
		}

		Entity IEntityRow.Entity => _linearNetwork;

		[DisplayName("Network Name")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public string Name => _linearNetwork.Name;

		[DisplayName("Created")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? CreatedDate => _linearNetwork.CreatedDate;

		[DisplayName("Created By")]
		[ColumnConfiguration(Width = 80)]
		[UsedImplicitly]
		public string CreatedByUser => _linearNetwork.CreatedByUser;

		[DisplayName("Last Changed")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? LastChangedDate => _linearNetwork.LastChangedDate;

		[DisplayName("Last Changed By")]
		[ColumnConfiguration(MinimumWidth = 90)]
		[UsedImplicitly]
		public string LastChangedByUser => _linearNetwork.LastChangedByUser;
	}
}