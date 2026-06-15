using System;
using System.ComponentModel;
using System.Drawing;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public class ReferencingInstanceConfigurationTableRow : SelectableTableRow, IEntityRow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ReferencingInstanceConfigurationTableRow"/> class.
		/// </summary>
		/// <param name="entity">The instance configuration.</param>
		public ReferencingInstanceConfigurationTableRow([NotNull] InstanceConfiguration entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			InstanceConfiguration = entity;
		}

		[DisplayName("")]
		[UsedImplicitly]
		public Image Image
		{
			get
			{
				Image image = TestTypeImageLookup.GetImage(InstanceConfiguration);
				image.Tag = TestTypeImageLookup.GetDefaultSortIndex(InstanceConfiguration);

				return image;
			}
		}

		[UsedImplicitly]
		public string Name => InstanceConfiguration.Name;

		[CanBeNull]
		[ColumnConfiguration(Width = 200)]
		[UsedImplicitly]
		public string Category => InstanceConfiguration.Category?.GetQualifiedName();

		[UsedImplicitly]
		[ColumnConfiguration(Width = 300)]
		public string Description => InstanceConfiguration.Description;

		[DisplayName("Created")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? CreatedDate => InstanceConfiguration.CreatedDate;

		[DisplayName("Created By")]
		[ColumnConfiguration(Width = 80)]
		[UsedImplicitly]
		public string CreatedByUser => InstanceConfiguration.CreatedByUser;

		[DisplayName("Last Changed")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? LastChangedDate => InstanceConfiguration.LastChangedDate;

		[DisplayName("Last Changed By")]
		[ColumnConfiguration(MinimumWidth = 90)]
		[UsedImplicitly]
		public string LastChangedByUser => InstanceConfiguration.LastChangedByUser;

		[Browsable(false)]
		[NotNull]
		public InstanceConfiguration InstanceConfiguration { get; }

		#region IEntityRow Members

		Entity IEntityRow.Entity => InstanceConfiguration;

		#endregion
	}
}
