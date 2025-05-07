using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.Models
{
	public class ModelToQualityVerificationDependingItem : DependingItem
	{
		[NotNull] private readonly QualityVerification _qualityVerification;
		[NotNull] private readonly DdxModel _model;
		[NotNull] private readonly IUnitOfWork _unitOfWork;

		public ModelToQualityVerificationDependingItem(
			[NotNull] QualityVerification qualityVerification,
			[NotNull] DdxModel model,
			[NotNull] IUnitOfWork unitOfWork)
			: base(qualityVerification, GetName(qualityVerification))
		{
			Assert.ArgumentNotNull(qualityVerification, nameof(qualityVerification));
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));

			_qualityVerification = qualityVerification;
			_model = model;
			_unitOfWork = unitOfWork;
		}

		public override bool CanRemove => true;

		public override bool RequiresConfirmation => false;

		public override bool RemovedByCascadingDeletion => false;

		protected override void RemoveDependencyCore()
		{
			_unitOfWork.Reattach(_model);
			_unitOfWork.Reattach(_qualityVerification);

			_qualityVerification.RemoveVerificationDatasets(_model.Datasets);
		}

		[NotNull]
		private static string GetName([NotNull] QualityVerification qualityVerification)
		{
			Assert.ArgumentNotNull(qualityVerification, nameof(qualityVerification));

			// TODO remove duplication with other DependingItems -> quality verification
			return string.Format("Verification of {0} from {1}",
			                     qualityVerification.SpecificationName,
			                     qualityVerification.StartDate);
		}
	}
}
