using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.DataModel
{
	public abstract class ErrorDatasetSchema
	{
		[NotNull]
		public abstract ICollection<IssueDatasetName> IssueDatasetNames { get; }

		[NotNull]
		public abstract string StatusFieldName { get; }

		[NotNull]
		public abstract string StatusFieldAlias { get; }

		[NotNull]
		public abstract string QualityConditionIDFieldName { get; }

		[NotNull]
		public abstract string QualityConditionIDFieldAlias { get; }

		[NotNull]
		public abstract string OperatorFieldName { get; }

		[NotNull]
		public abstract string OperatorFieldAlias { get; }

		[NotNull]
		public abstract string DateOfCreationFieldName { get; }

		[NotNull]
		public abstract string DateOfCreationFieldAlias { get; }

		[NotNull]
		public abstract string DateOfChangeFieldName { get; }

		[NotNull]
		public abstract string DateOfChangeFieldAlias { get; }

		[NotNull]
		public abstract string QualityConditionParametersFieldName { get; }

		[NotNull]
		public abstract string QualityConditionParametersFieldAlias { get; }

		[NotNull]
		public abstract string QualityConditionNameFieldName { get; }

		[NotNull]
		public abstract string QualityConditionNameFieldAlias { get; }

		[NotNull]
		public abstract string ErrorDescriptionFieldName { get; }

		[NotNull]
		public abstract string ErrorDescriptionFieldAlias { get; }

		[NotNull]
		public abstract string ErrorObjectsFieldName { get; }

		[NotNull]
		public abstract string ErrorObjectsFieldAlias { get; }

		[NotNull]
		public abstract string ErrorTypeFieldName { get; }

		[NotNull]
		public abstract string ErrorTypeFieldAlias { get; }

		[NotNull]
		public abstract string QualityConditionVersionFieldName { get; }

		[NotNull]
		public abstract string QualityConditionVersionFieldAlias { get; }

		[CanBeNull]
		public virtual string ErrorAffectedComponentFieldName => null;

		[CanBeNull]
		public virtual string ErrorAffectedComponentFieldAlias => null;
	}
}
