using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA
{
	public class CustomQualitySpecification : QualitySpecification
	{
		public CustomQualitySpecification(
			[NotNull] QualitySpecification customizedSpecification,
			[NotNull] string name,
			bool assignUuid = true)
			: base(name, assignUuid)
		{
			Assert.ArgumentNotNull(customizedSpecification, nameof(customizedSpecification));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			CustomizedSpecification = customizedSpecification;

			customizedSpecification.CopyPropertiesTo(this);
		}

		protected sealed override bool IsCustomCore => true;

		protected override CustomQualitySpecification GetCustomizableCore()
		{
			return this;
		}

		[NotNull]
		public QualitySpecification CustomizedSpecification { get; }

		public IEnumerable<QualityCondition> GetDisabledConditions()
		{
			return Elements.Where(x => ! x.Enabled &&
			                           StringUtils.IsNotEmpty(x.QualityCondition.Uuid))
			               .Select(x => x.QualityCondition);
		}

		public override QualitySpecification BaseSpecification
			=> CustomizedSpecification.BaseSpecification;

		public void UpdateFrom([NotNull] QualitySpecification source,
		                       [NotNull] HashSet<Dataset> verifiedDatasets)
		{
			Assert.ArgumentNotNull(source, nameof(source));

			IDictionary<string, QualitySpecificationElement> updatedElements =
				Elements.Where(x => x.QualityCondition.Updated &&
				                    StringUtils.IsNotEmpty(x.QualityCondition.Uuid))
				        .ToDictionary(x => x.QualityCondition.Uuid);

			var disabledElements = new HashSet<string>(
				GetDisabledConditions().Select(q => q.Uuid));

			Clear();

			foreach (QualitySpecificationElement sourceElement in source.Elements)
			{
				if (! IsQualityConditionApplicable(sourceElement.QualityCondition,
				                                   verifiedDatasets))
				{
					continue;
				}

				string key = sourceElement.QualityCondition.Uuid;

				QualitySpecificationElement updated;
				if (updatedElements.TryGetValue(key, out updated))
				{
					AddElement(updated.QualityCondition,
					           updated.StopOnErrorOverride,
					           updated.AllowErrorsOverride,
					           ! updated.Enabled);

					updatedElements.Remove(key);
				}
				else
				{
					// not an updated element - check if it was disabled
					bool disabled = disabledElements.Contains(key) || ! sourceElement.Enabled;

					AddElement(sourceElement.QualityCondition.Clone(),
					           sourceElement.StopOnErrorOverride,
					           sourceElement.AllowErrorsOverride,
					           disabled);
				}
			}

			// re-add updated elements that are no longer part of source specification
			foreach (QualitySpecificationElement updated in updatedElements.Values)
			{
				AddElement(updated.QualityCondition,
				           updated.StopOnErrorOverride,
				           updated.AllowErrorsOverride,
				           ! updated.Enabled);
			}
		}
	}
}
