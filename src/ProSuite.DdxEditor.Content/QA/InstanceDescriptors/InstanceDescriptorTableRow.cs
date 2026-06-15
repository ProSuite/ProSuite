using System;
using System.ComponentModel;
using System.Drawing;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.Core.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public class InstanceDescriptorTableRow : IEntityRow, IEntityRow<InstanceDescriptor>
	{
		private readonly InstanceDescriptor _entity;
		private readonly int _referencingInstanceConfigurationCount;
		private readonly string _algorithmDescription;
		private readonly Image _image;
		private readonly string _parameters;
		private readonly string _implementation;
		private readonly string _categories;

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceDescriptorTableRow"/> class.
		/// </summary>
		/// <param name="entity">The instance descriptor.</param>
		/// <param name="referencingInstanceConfigurationCount">The number of configurations that reference this
		/// instance descriptor.</param>
		public InstanceDescriptorTableRow([NotNull] InstanceDescriptor entity,
		                                  int referencingInstanceConfigurationCount)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;
			_referencingInstanceConfigurationCount = referencingInstanceConfigurationCount;

			_image = TestTypeImageLookup.GetImage(entity);
			_image.Tag = TestTypeImageLookup.GetDefaultSortIndex(entity);

			try
			{
				IInstanceInfo instanceInfo =
					InstanceDescriptorUtils.GetInstanceInfo(entity);

				_algorithmDescription = instanceInfo.TestDescription ?? string.Empty;

				_categories = StringUtils.ConcatenateSorted(instanceInfo.TestCategories, ", ");

				_parameters = InstanceUtils.GetTestSignature(instanceInfo);
			}
			catch (TypeLoadException e)
			{
				_parameters = string.Format("Error: {0}", e.Message);
				_algorithmDescription = "<INVALID>";
			}

			if (entity.Class != null)
			{
				_implementation = entity.Class.TypeName;
			}
			else
			{
				_implementation = "<not defined>";
			}
		}

		[DisplayName("")]
		[UsedImplicitly]
		public Image Image => _image;

		[UsedImplicitly]
		public string Name => _entity.Name;

		[ColumnConfiguration(Width = 150)]
		[UsedImplicitly]
		public string Categories => _categories;

		[DisplayName("Usage Count")]
		[ColumnConfiguration(Width = 70)]
		[UsedImplicitly]
		public int ReferencingInstanceConfigurationCount => _referencingInstanceConfigurationCount;

		[UsedImplicitly]
		[ColumnConfiguration(Width = 300)]
		public string Parameters => _parameters;

		[DisplayName("Algorithm Documentation")]
		[ColumnConfiguration(Width = 300)]
		[UsedImplicitly]
		public string AlgorithmDescription => _algorithmDescription;

		[DisplayName("Algorithm Implementation")]
		[UsedImplicitly]
		public string Implementation => _implementation;

		[ColumnConfiguration(Width = 300)]
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

		[Browsable(false)]
		[UsedImplicitly]
		public InstanceDescriptor InstanceDescriptor => _entity;

		Entity IEntityRow.Entity => _entity;

		InstanceDescriptor IEntityRow<InstanceDescriptor>.Entity => _entity;
	}
}
