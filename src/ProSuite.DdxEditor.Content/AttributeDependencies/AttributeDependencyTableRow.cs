using System;
using System.ComponentModel;
using System.Drawing;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.AttributeDependencies;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.AttributeDependencies
{
	public class AttributeDependencyTableRow : SelectableTableRow, IEntityRow
	{
		private readonly AttributeDependency _entity;
		private readonly Image _image;

		public AttributeDependencyTableRow([NotNull] AttributeDependency entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;

			_image = Resources.AttributeDependencyItem;
		}

		[DisplayName("")]
		[NotNull]
		[UsedImplicitly]
		public Image Image => _image;

		[UsedImplicitly]
		public string Name => _entity.Dataset.Name;

		[DisplayName(@"Model")]
		[UsedImplicitly]
		public string ModelName
		{
			get
			{
				DdxModel model = _entity.Dataset.Model;
				return model == null ? "(null)" : model.Name;
			}
		}

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
		public AttributeDependency AttributeDependency => _entity;

		#region IEntityRow Members

		Entity IEntityRow.Entity => _entity;

		#endregion
	}
}
