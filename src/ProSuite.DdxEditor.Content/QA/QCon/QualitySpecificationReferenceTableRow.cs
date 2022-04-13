using System.ComponentModel;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.TableRows;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public class QualitySpecificationReferenceTableRow : IEntityRow
	{
		public QualitySpecificationReferenceTableRow(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] QualitySpecificationElement qualitySpecificationElement)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));
			Assert.ArgumentNotNull(qualitySpecificationElement,
			                       nameof(qualitySpecificationElement));

			QualitySpecification = qualitySpecification;
			QualitySpecificationElement = qualitySpecificationElement;

			QualitySpecificationName = qualitySpecification.Name;

			if (qualitySpecification.Category != null)
			{
				Category = qualitySpecification.Category.GetQualifiedName();
			}
		}

		[UsedImplicitly]
		public string QualitySpecificationName { get; private set; }

		[UsedImplicitly]
		public string Category { get; private set; }

		[UsedImplicitly]
		public BooleanOverride AllowErrorsOverride
		{
			get
			{
				return
					NullableBooleanItems.GetBooleanOverride(
						QualitySpecificationElement.AllowErrorsOverride);
			}
			set
			{
				QualitySpecificationElement.AllowErrorsOverride =
					NullableBooleanItems.GetNullableBoolean(value);
			}
		}

		[UsedImplicitly]
		public BooleanOverride StopOnErrorOverride
		{
			get
			{
				return
					NullableBooleanItems.GetBooleanOverride(
						QualitySpecificationElement.StopOnErrorOverride);
			}
			set
			{
				QualitySpecificationElement.StopOnErrorOverride =
					NullableBooleanItems.GetNullableBoolean(value);
			}
		}

		[Browsable(false)]
		[NotNull]
		public QualitySpecification QualitySpecification { get; }

		[Browsable(false)]
		[NotNull]
		public QualitySpecificationElement QualitySpecificationElement { get; }

		Entity IEntityRow.Entity => QualitySpecification;
	}
}
