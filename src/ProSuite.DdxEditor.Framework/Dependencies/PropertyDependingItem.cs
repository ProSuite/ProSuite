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
		/// <param name="propertyName">Name of the property.</param>
		protected PropertyDependingItem([NotNull] Entity entity,
		                                [CanBeNull] string entityName,
		                                [NotNull] string propertyName)
			: base(entity, GetName(entityName, propertyName))
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));
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
