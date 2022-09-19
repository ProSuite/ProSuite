using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Items
{
	public abstract class EntityTypeItem<T> : GroupItem, IEntityTypeItem where T : Entity
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="EntityTypeItem&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="text">The text.</param>
		protected EntityTypeItem([NotNull] string text) : base(text) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="EntityTypeItem&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="description">The description.</param>
		protected EntityTypeItem([NotNull] string text,
		                         [CanBeNull] string description) : base(text, description) { }

		#endregion

		public bool IsBasedOn(Type type)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			return typeof(T).IsAssignableFrom(type);
		}
	}
}
