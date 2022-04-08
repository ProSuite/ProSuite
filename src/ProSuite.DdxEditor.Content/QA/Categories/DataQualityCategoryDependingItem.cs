using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.QA.Categories
{
	public class DataQualityCategoryDependingItem : DependingItem
	{
		[NotNull] private readonly DataQualityCategory _category;
		[NotNull] private readonly IDataQualityCategoryRepository _categoryRepository;
		[NotNull] private readonly IUnitOfWork _unitOfWork;

		public DataQualityCategoryDependingItem(
			[NotNull] DataQualityCategory category,
			[NotNull] IDataQualityCategoryRepository categoryRepository,
			[NotNull] IUnitOfWork unitOfWork)
			: base(category, category.GetQualifiedName())
		{
			Assert.ArgumentNotNull(category, nameof(category));
			Assert.ArgumentNotNull(categoryRepository, nameof(categoryRepository));
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));

			_category = category;
			_categoryRepository = categoryRepository;
			_unitOfWork = unitOfWork;
		}

		public override bool CanRemove => true;

		public override bool RemovedByCascadingDeletion => true;

		protected override void RemoveDependencyCore()
		{
			_unitOfWork.Reattach(_category);

			DeleteCategory(_category);
		}

		private void DeleteCategory([NotNull] DataQualityCategory category)
		{
			foreach (DataQualityCategory subCategory in category.SubCategories)
			{
				DeleteCategory(subCategory);
			}

			_categoryRepository.Delete(category);
		}
	}
}