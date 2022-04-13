using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.Models
{
	public class DeletableQualityConditionDependingItem : DependingItem
	{
		[NotNull] private readonly QualityCondition _qualityCondition;
		[NotNull] private readonly IQualityConditionRepository _qualityConditionRepository;

		[NotNull] private readonly IQualityVerificationRepository
			_qualityVerificationRepository;

		[NotNull] private readonly IQualitySpecificationRepository
			_qualitySpecificationRepository;

		[NotNull] private readonly IUnitOfWork _unitOfWork;

		/// <summary>
		/// Initializes a new instance of the <see cref="DeletableQualityConditionDependingItem"/> class.
		/// </summary>
		/// <param name="qualityCondition">The quality condition.</param>
		/// <param name="qualityConditionRepository">The quality condition repository.</param>
		/// <param name="qualityVerificationRepository">The quality verification repository.</param>
		/// <param name="qualitySpecificationRepository">The quality specification repository.</param>
		/// <param name="unitOfWork">The unit of work.</param>
		public DeletableQualityConditionDependingItem(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IQualityConditionRepository qualityConditionRepository,
			[NotNull] IQualityVerificationRepository qualityVerificationRepository,
			[NotNull] IQualitySpecificationRepository qualitySpecificationRepository,
			[NotNull] IUnitOfWork unitOfWork)
			: base(qualityCondition, qualityCondition.Name)
		{
			Assert.ArgumentNotNull(qualityConditionRepository,
			                       nameof(qualityConditionRepository));
			Assert.ArgumentNotNull(qualityVerificationRepository,
			                       nameof(qualityVerificationRepository));
			Assert.ArgumentNotNull(qualitySpecificationRepository,
			                       nameof(qualitySpecificationRepository));
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));

			_qualityCondition = qualityCondition;
			_qualityConditionRepository = qualityConditionRepository;
			_qualityVerificationRepository = qualityVerificationRepository;
			_qualitySpecificationRepository = qualitySpecificationRepository;
			_unitOfWork = unitOfWork;
		}

		#region Overrides of DependingItem

		public override bool CanRemove => true;

		public override bool RemovedByCascadingDeletion => true;

		protected override void RemoveDependencyCore()
		{
			_unitOfWork.Reattach(_qualityCondition);

			foreach (
				QualityVerification qualityVerification in
				_qualityVerificationRepository.Get(_qualityCondition))
			{
				qualityVerification.UnlinkQualityCondition(_qualityCondition);
			}

			foreach (QualitySpecification qualitySpecification in
			         _qualitySpecificationRepository.Get(_qualityCondition))
			{
				qualitySpecification.RemoveElement(_qualityCondition);
			}

			_qualityConditionRepository.Delete(_qualityCondition);
		}

		#endregion
	}
}
