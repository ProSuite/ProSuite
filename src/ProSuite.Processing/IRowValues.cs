using System.Collections.Generic;
using ProSuite.Commons.Collections;

namespace ProSuite.Processing
{
	/// <summary>
	/// An abstraction of Rows and RowBuffers
	/// </summary>
	public interface IRowValues : INamedValues
	{
		IReadOnlyList<string> FieldNames { get; }

		object this[int index] { get; set; }

		int FindField(string fieldName);
	}
}
