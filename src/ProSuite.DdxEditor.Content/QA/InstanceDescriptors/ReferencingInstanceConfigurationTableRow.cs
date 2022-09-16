using System;
using System.ComponentModel;
using System.Drawing;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Content.QA.TestDescriptors;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public class ReferencingInstanceConfigurationTableRow : SelectableTableRow, IEntityRow
	{
		private readonly InstanceConfiguration _entity;

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferencingQualityConditionTableRow"/> class.
		/// </summary>
		/// <param name="entity">The quality condition.</param>
		public ReferencingInstanceConfigurationTableRow([NotNull] InstanceConfiguration entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;
		}

		[DisplayName("")]
		[UsedImplicitly]
		public Image Image
		{
			get
			{
				Image image = TestTypeImageLookup.GetImage(_entity);
				if (_entity != null)
				{
					image.Tag = TestTypeImageLookup.GetDefaultSortIndex(_entity);
				}

				return image;
			}
		}

		[UsedImplicitly]
		public string Name => _entity.Name;

		[UsedImplicitly]
		[ColumnConfiguration(Width = 300)]
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
		public InstanceConfiguration InstanceConfiguration => _entity;

		#region IEntityRow Members

		Entity IEntityRow.Entity => _entity;

		#endregion
	}
}
