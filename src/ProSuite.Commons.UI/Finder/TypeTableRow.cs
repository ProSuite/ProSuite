using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Finder
{
	internal class TypeTableRow : IEquatable<TypeTableRow>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TypeTableRow"/> class.
		/// </summary>
		/// <param name="type">The type.</param>
		public TypeTableRow([NotNull] Type type)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			Type = type;
		}

		[NotNull]
		public string Name => Type.Name;

		[NotNull]
		public string Namespace => Type.Namespace ?? string.Empty;

		[NotNull]
		public Type Type { get; }

		public bool Equals(TypeTableRow typeTableRow)
		{
			if (typeTableRow == null)
			{
				return false;
			}

			return Type == typeTableRow.Type;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			return Equals(obj as TypeTableRow);
		}

		public override int GetHashCode()
		{
			return Type.GetHashCode();
		}
	}
}
