using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Dependencies
{
	public abstract class PropertyDependingItem : DependingItem
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyDependingItem"/> class.
		/// </summary>
		/// <param name="entity">The entity.</param>
		/// <param name="entityName">Name of the entity.</param>
		/// <param name="affectedProperty">Descriptive name of the affected property.</param>
		protected PropertyDependingItem([NotNull] Entity entity,
		                                [CanBeNull] string entityName,
		                                [NotNull] string affectedProperty)
			: base(entity, GetName(entityName, affectedProperty))
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNullOrEmpty(affectedProperty, nameof(affectedProperty));
		}

		[NotNull]
		private static string GetName([CanBeNull] string entityName,
		                              [NotNull] string dependencyName)
		{
			return string.Format("{0} [{1}]",
			                     entityName ?? "<no name>",
			                     dependencyName);
		}
	}
}
