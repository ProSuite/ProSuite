using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.Core.DataModel.ResourceLookup;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public class DatasetTableRow : SelectableTableRow, IEntityRow
	{
		private readonly string _abbreviation;
		private readonly string _aliasName;
		private readonly Dataset _entity;
		private readonly string _datasetCategory;
		private readonly string _description;
		private readonly string _name;
		private readonly string _typeDescription;

		private readonly Image _image;
		private readonly string _modelName;

		/// <summary>
		/// Initializes a new instance of the <see cref="DatasetTableRow"/> class.
		/// </summary>
		/// <param name="entity">The dataset.</param>
		public DatasetTableRow([NotNull] Dataset entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;

			_typeDescription = entity.TypeDescription;
			_name = entity.Name;
			_aliasName = entity.AliasName;
			_description = entity.Description;

			_abbreviation = entity.Abbreviation;

			_modelName = entity.Model != null
				             ? entity.Model.Name
				             : string.Empty;

			_datasetCategory = entity.DatasetCategory != null
				                   ? entity.DatasetCategory.Name
				                   : string.Empty;

			_image = DatasetTypeImageLookup.GetImage(entity);
			_image.Tag = DatasetTypeImageLookup.GetDefaultSortIndex(entity);
		}

		[NotNull]
		[UsedImplicitly]
		public Image Image => _image;

		[DisplayName("Name")]
		[UsedImplicitly]
		public string Name => _name;

		[DisplayName("Alias Name")]
		[UsedImplicitly]
		public string AliasName => _aliasName;

		[DisplayName("Type")]
		[UsedImplicitly]
		public string TypeDescription => _typeDescription;

		[DisplayName("Category")]
		[UsedImplicitly]
		public string DatasetCategory => _datasetCategory;

		[UsedImplicitly]
		public string Abbreviation => _abbreviation;

		[UsedImplicitly]
		[ColumnConfiguration(
			MinimumWidth = 100,
			AutoSizeColumnMode = DataGridViewAutoSizeColumnMode.Fill)]
		public string Description => _description;

		[DisplayName("Model")]
		[UsedImplicitly]
		public string ModelName => _modelName;

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
		public Dataset Dataset => _entity;

		#region IEntityRow Members

		[Browsable(false)]
		[NotNull]
		public Entity Entity => _entity;

		#endregion
	}
}
