using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Dependencies;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public class QualityConditionToVerificationDependingItem : DependingItem
	{
		[NotNull] private readonly QualityVerification _qualityVerification;
		[NotNull] private readonly QualityCondition _qualityCondition;
		[NotNull] private readonly IUnitOfWork _unitOfWork;

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityConditionToVerificationDependingItem"/> class.
		/// </summary>
		/// <param name="qualityVerification">The quality specification.</param>
		/// <param name="qualityCondition">The quality condition.</param>
		/// <param name="unitOfWork">The unit of work.</param>
		public QualityConditionToVerificationDependingItem(
			[NotNull] QualityVerification qualityVerification,
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IUnitOfWork unitOfWork)
			: base(qualityVerification, GetName(qualityVerification))
		{
			Assert.ArgumentNotNull(qualityVerification, nameof(qualityVerification));
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));

			_qualityVerification = qualityVerification;
			_qualityCondition = qualityCondition;
			_unitOfWork = unitOfWork;
		}

		public override bool CanRemove => true;

		public override bool RequiresConfirmation => false;

		public override bool RemovedByCascadingDeletion => false;

		protected override void RemoveDependencyCore()
		{
			_unitOfWork.Reattach(_qualityVerification);
			_unitOfWork.Reattach(_qualityCondition);

			_qualityVerification.UnlinkQualityCondition(_qualityCondition);
		}

		[NotNull]
		private static string GetName([NotNull] QualityVerification qualityVerification)
		{
			Assert.ArgumentNotNull(qualityVerification, nameof(qualityVerification));

			return string.Format("Verification of {0} from {1}",
			                     qualityVerification.SpecificationName,
			                     qualityVerification.StartDate);
		}
	}
}
