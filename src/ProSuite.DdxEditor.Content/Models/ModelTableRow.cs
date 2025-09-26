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
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
	public class ModelTableRow : SelectableTableRow, IEntityRow
	{
		private readonly DdxModel _entity;
		private readonly Image _image;
		private readonly string _type;

		/// <summary>
		/// Initializes a new instance of the <see cref="ModelTableRow"/> class.
		/// </summary>
		/// <param name="entity">The model.</param>
		public ModelTableRow([NotNull] DdxModel entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;
			_image = Resources.ModelItem;
			_type = _entity.GetType().Name;
		}

		[NotNull]
		[UsedImplicitly]
		public Image Image => _image;

		[UsedImplicitly]
		public string Name => _entity.Name;

		[UsedImplicitly]
		public string SpatialReference =>
			_entity.SpatialReferenceDescriptor == null
				? string.Empty
				: _entity.SpatialReferenceDescriptor.Name;

		[DisplayName("Master Database")]
		public string MasterDatabase =>
			_entity.UserConnectionProvider == null
				? string.Empty
				: _entity.UserConnectionProvider.Name;

		[DisplayName("Master Database Used For")]
		public string MasterDatabaseUsage =>
			_entity.UseDefaultDatabaseOnlyForSchema
				? "Schema only"
				: "Schema and data";

		[ColumnConfiguration(
			MinimumWidth = 100,
			AutoSizeColumnMode = DataGridViewAutoSizeColumnMode.Fill)]
		[UsedImplicitly]
		public string Description => _entity.Description ?? string.Empty;

		[UsedImplicitly]
		public string Type => _type;

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
		[UsedImplicitly]
		public DdxModel Model => _entity;

		#region IEntityRow Members

		[Browsable(false)]
		public Entity Entity => _entity;

		#endregion
	}
}
