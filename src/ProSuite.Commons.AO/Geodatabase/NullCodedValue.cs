using System;

namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	/// Special coded value, representing NULL.
	/// </summary>
	public class NullCodedValue : CodedValue
	{
		private const string _defaultName = "<Null>";

		public NullCodedValue() : base(DBNull.Value, _defaultName) { }

		public NullCodedValue(string name) : base(DBNull.Value, name) { }
	}
}
