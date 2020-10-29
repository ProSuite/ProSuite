using System;
using System.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Core
{
	public class TestParameter
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TestParameter"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="paramType">Type of the param.</param>
		/// <param name="description">The description.</param>
		/// <param name="isConstructorParameter"></param>
		public TestParameter([NotNull] string name,
		                     [NotNull] Type paramType,
		                     [CanBeNull] string description = null,
		                     bool isConstructorParameter = true)
		{
			Assert.ArgumentNotNull(name, nameof(name));
			Assert.ArgumentNotNull(paramType, nameof(paramType));

			Name = name;

			var arrayDimension = 0;
			while (paramType.IsGenericType &&
			       typeof(IEnumerable).IsAssignableFrom(paramType) &&
			       paramType.GetGenericArguments().Length == 1)
			{
				paramType = Assert.NotNull(paramType.GetGenericArguments()[0]);
				arrayDimension++;
			}

			while (paramType.HasElementType)
			{
				paramType = Assert.NotNull(paramType.GetElementType());
				arrayDimension++;
			}

			Type = paramType;
			Description = description;
			ArrayDimension = arrayDimension;
			IsConstructorParameter = isConstructorParameter;
		}

		[NotNull]
		public string Name { get; }

		[NotNull]
		public Type Type { get; }

		public int ArrayDimension { get; }

		public bool IsConstructorParameter { get; }

		[CanBeNull]
		public object DefaultValue { get; set; }

		[CanBeNull]
		public string Description { get; set; }

		public override string ToString()
		{
			return $"Name: {Name}, Type: {Type}, " +
			       $"IsConstructorParameter: {IsConstructorParameter}";
		}
	}
}
