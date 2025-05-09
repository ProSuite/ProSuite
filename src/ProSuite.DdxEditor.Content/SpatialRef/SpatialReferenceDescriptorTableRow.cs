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

namespace ProSuite.DdxEditor.Content.SpatialRef
{
	public class SpatialReferenceDescriptorTableRow : SelectableTableRow, IEntityRow
	{
		[NotNull] private readonly SpatialReferenceDescriptor _entity;
		[NotNull] private static readonly Image _image = Resources.SpatialReferenceItem;

		/// <summary>
		/// Initializes a new instance of the <see cref="SpatialReferenceDescriptorTableRow"/> class.
		/// </summary>
		/// <param name="entity">The spatial reference descriptor.</param>
		public SpatialReferenceDescriptorTableRow(
			[NotNull] SpatialReferenceDescriptor entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;
		}

		[DisplayName("")]
		[UsedImplicitly]
		public Image Image => _image;

		[UsedImplicitly]
		public string Name => _entity.Name;

		[UsedImplicitly]
		[ColumnConfiguration(
			MinimumWidth = 100,
			AutoSizeColumnMode = DataGridViewAutoSizeColumnMode.Fill)]
		public string Description => _entity.Description;

		[DisplayName("Xml")]
		[UsedImplicitly]
		[ColumnConfiguration(Width = 300)]
		public string XmlString => _entity.XmlString;

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
		public SpatialReferenceDescriptor SpatialReferenceDescriptor => _entity;

		#region IEntityRow Members

		Entity IEntityRow.Entity => _entity;

		#endregion
	}
}
