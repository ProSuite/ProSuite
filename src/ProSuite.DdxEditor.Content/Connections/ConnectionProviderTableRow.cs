using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DdxEditor.Content.Connections
{
	public class ConnectionProviderTableRow : IEntityRow
	{
		[NotNull] private readonly ConnectionProvider _entity;
		[NotNull] private static readonly Image _image = Resources.ConnectionProviderItem;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionProviderTableRow"/> class.
		/// </summary>
		/// <param name="entity">The connection provider.</param>
		public ConnectionProviderTableRow([NotNull] ConnectionProvider entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;
		}

		[DisplayName("")]
		[UsedImplicitly]
		public Image Image => _image;

		[UsedImplicitly]
		public string Name => _entity.Name;

		[DisplayName("Type")]
		[UsedImplicitly]
		public string TypeDescription => _entity.TypeDescription;

		[ColumnConfiguration(
			MinimumWidth = 100,
			AutoSizeColumnMode = DataGridViewAutoSizeColumnMode.Fill)]
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
