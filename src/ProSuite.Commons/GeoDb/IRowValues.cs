using System.Collections.Generic;
using ProSuite.Commons.Collections;

namespace ProSuite.Commons.Gdb
{
	/// <summary>
	/// An abstraction of Rows and RowBuffers. Adapters exist for Row and
	/// RowBuffer. Historical remark: with ArcObjects, IRow derived from
	/// IRowBuffer; with the Pro SDK, Row does not derive from RowBuffer.
	/// </summary>
	public interface IRowValues : INamedValues
	{
		IReadOnlyList<string> FieldNames { get; }

		object this[int index] { get; set; }

		int FindField(string fieldName);
	}
}
