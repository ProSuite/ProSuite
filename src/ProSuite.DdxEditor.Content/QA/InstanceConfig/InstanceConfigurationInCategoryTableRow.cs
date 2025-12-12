using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public class InstanceConfigurationInCategoryTableRow : SelectableTableRow, IEntityRow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceConfigurationInCategoryTableRow"/> class.
		/// </summary>
		/// <param name="entity">The instance configuration.</param>
		/// <param name="usageCount"></param>
		public InstanceConfigurationInCategoryTableRow([NotNull] InstanceConfiguration entity,
		                                               int usageCount)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			InstanceConfiguration = entity;
			UsageCount = usageCount;

			Image = TestTypeImageLookup.GetImage(entity);
			Image.Tag = TestTypeImageLookup.GetDefaultSortIndex(entity);
		}

		[DisplayName("")]
		[NotNull]
		[UsedImplicitly]
		public Image Image { get; }

		[ColumnConfiguration(Width = 400)]
		[UsedImplicitly]
		public string Name => InstanceConfiguration.Name;

		[ColumnConfiguration(MinimumWidth = 100,
		                     AutoSizeColumnMode = DataGridViewAutoSizeColumnMode.Fill)]
		[UsedImplicitly]
		public string Description => InstanceConfiguration.Description;

		[DisplayName("Usage Count")]
		[ColumnConfiguration(Width = 70)]
		[UsedImplicitly]
		public int UsageCount { get; }

		[DisplayName("Algorithm")]
		[UsedImplicitly]
		public string AlgorithmImplementation => InstanceConfiguration.InstanceDescriptor.Name;

		[DisplayName("Url")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public string Url => InstanceConfiguration.Url;

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
