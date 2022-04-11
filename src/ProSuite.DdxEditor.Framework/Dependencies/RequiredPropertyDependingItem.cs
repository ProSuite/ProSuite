using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Dependencies
{
	public class RequiredPropertyDependingItem : PropertyDependingItem
	{
		public RequiredPropertyDependingItem([NotNull] Entity entity,
		                                     [CanBeNull] string entityName,
		                                     [NotNull] string propertyName)
			: base(entity, entityName, propertyName) { }

		public override bool CanRemove => false;

		public override bool RemovedByCascadingDeletion => false;

		protected override void RemoveDependencyCore()
		{
			throw new InvalidOperationException(
				"Unable to remove dependency, property is required");
		}
	}
}
