using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.DdxEditor.Content.QA
{
	public class NullableBooleanItem
	{
		private readonly BooleanOverride _value;
		private readonly string _name;

		/// <summary>
		/// Initializes a new instance of the <see cref="NullableBooleanItem"/> class.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="name">The name.</param>
		public NullableBooleanItem(BooleanOverride value, string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			_value = value;
			_name = name;
		}

		// accessed from datagrid via reflection
		public BooleanOverride Value => _value;

		// accessed from datagrid via reflection
		public string Name => _name;

		public override string ToString()
		{
			// use properties here (instead of fields) to get rid of resharper warnings (unused props)
			return string.Format("{0}: {1}", Value, Name);
		}
	}
}
