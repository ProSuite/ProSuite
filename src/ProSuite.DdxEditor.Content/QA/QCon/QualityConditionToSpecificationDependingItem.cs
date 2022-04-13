using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public class QualityConditionToSpecificationDependingItem : DependingItem
	{
		private readonly QualitySpecification _qualitySpecification;
		private readonly QualityCondition _qualityCondition;
		private readonly IUnitOfWork _unitOfWork;

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityConditionToSpecificationDependingItem"/> class.
		/// </summary>
		/// <param name="qualitySpecification">The quality specification.</param>
		/// <param name="qualityCondition">The quality condition.</param>
		/// <param name="unitOfWork">The unit of work.</param>
		public QualityConditionToSpecificationDependingItem(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IUnitOfWork unitOfWork)
			: base(qualitySpecification, qualitySpecification.Name)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));

			_qualitySpecification = qualitySpecification;
			_qualityCondition = qualityCondition;
			_unitOfWork = unitOfWork;
		}

		public override bool RemovedByCascadingDeletion => false;

		protected override void RemoveDependencyCore()
		{
			_unitOfWork.Reattach(_qualitySpecification, _qualityCondition);

			_qualitySpecification.RemoveElement(_qualityCondition);
		}
	}
}