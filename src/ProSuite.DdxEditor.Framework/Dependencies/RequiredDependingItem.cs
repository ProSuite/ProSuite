using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Dependencies
{
	public class RequiredDependingItem : DependingItem
	{
		public RequiredDependingItem([NotNull] Entity entity, [NotNull] string name)
			: base(entity, name) { }

		public override bool RemovedByCascadingDeletion => false;

		public override bool CanRemove => false;

		protected override void RemoveDependencyCore()
		{
			throw new InvalidOperationException("Unable to remove dependency");
		}
	}
}
