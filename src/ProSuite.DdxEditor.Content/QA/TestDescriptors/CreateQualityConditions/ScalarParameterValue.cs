using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors.CreateQualityConditions
{
	internal class ScalarParameterValue
	{
		private readonly string _name;
		private readonly string _value;

		/// <summary>
		/// Initializes a new instance of the <see cref="ScalarParameterValue"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		public ScalarParameterValue([NotNull] string name, [NotNull] string value)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentNotNull(value, nameof(value));

			_name = name;
			_value = value;
		}

		[NotNull]
		public string Name => _name;

		[NotNull]
		public string Value => _value;
	}
}
