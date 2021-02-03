using System.Collections.Generic;

namespace ProSuite.Processing.Utils
{
	/// <summary>
	/// An abstraction of Rows and RowBuffers
	/// </summary>
	public interface IRowValues
	{
		IReadOnlyList<string> FieldNames { get; }

		object this[int index] { get; set; }

		int FindField(string fieldName);
	}
}
