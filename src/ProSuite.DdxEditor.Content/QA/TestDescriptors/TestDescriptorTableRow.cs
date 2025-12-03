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

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public class TestDescriptorTableRow : IEntityRow, IEntityRow<TestDescriptor>
	{
		private readonly TestDescriptor _entity;
		private readonly int _referencingQualityConditionCount;
		private readonly string _testDescription;
		private readonly Image _image;
		private readonly string _parameters;
		private readonly string _testImplementation;
		private readonly string _categories;

		/// <summary>
		/// Initializes a new instance of the <see cref="TestDescriptorTableRow"/> class.
		/// </summary>
		/// <param name="entity">The test descriptor.</param>
		/// <param name="referencingQualityConditionCount">The number of quality conditions that reference this
		/// test descriptor.</param>
		public TestDescriptorTableRow([NotNull] TestDescriptor entity,
		                              int referencingQualityConditionCount)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_entity = entity;
			_referencingQualityConditionCount = referencingQualityConditionCount;

			_image = TestTypeImageLookup.GetImage(entity);
			_image.Tag = TestTypeImageLookup.GetDefaultSortIndex(entity);

			try
			{
				IInstanceInfo instanceInfo =
					InstanceDescriptorUtils.GetInstanceInfo(entity);

				if (instanceInfo == null)
				{
					_parameters = "No TestDescriptor";
					_testDescription = "<INVALID>";
				}
				else
				{
					_testDescription = instanceInfo.TestDescription ?? string.Empty;

					_categories = StringUtils.ConcatenateSorted(instanceInfo.TestCategories, ", ");

					_parameters = InstanceUtils.GetTestSignature(instanceInfo);
				}
			}
			catch (Exception e)
			{
				_parameters = string.Format("Error: {0}", e.Message);
				_testDescription = "<INVALID>";
			}

			if (entity.TestClass != null)
			{
				_testImplementation = entity.TestClass.TypeName;
			}
			else if (entity.TestFactoryDescriptor != null)
			{
				_testImplementation = entity.TestFactoryDescriptor.TypeName;
			}
			else
			{
				_testImplementation = "<not defined>";
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
		public int ReferencingQualityConditionCount => _referencingQualityConditionCount;

		[UsedImplicitly]
		[ColumnConfiguration(Width = 300)]
		public string Parameters => _parameters;

		[DisplayName("Test Documentation")]
		[ColumnConfiguration(Width = 300)]
		[UsedImplicitly]
		public string TestDescription => _testDescription;

		[DisplayName("Test Implementation")]
		[UsedImplicitly]
		public string TestImplementation => _testImplementation;

		[ColumnConfiguration(Width = 300)]
		[UsedImplicitly]
		public string Description => _entity.Description;

		[DisplayName("Execution Priority")]
		[ColumnConfiguration(MinimumWidth = 100)]
		[UsedImplicitly]
		public int? ExecutionPriority => _entity.ExecutionPriority;

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
		public TestDescriptor TestDescriptor => _entity;

		Entity IEntityRow.Entity => _entity;

		TestDescriptor IEntityRow<TestDescriptor>.Entity => _entity;
	}
}
