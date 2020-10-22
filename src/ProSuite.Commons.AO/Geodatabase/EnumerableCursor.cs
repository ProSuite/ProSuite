using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class EnumerableCursor : IEnumerable<IRow>
	{
		private readonly ICursor _cursor;

		#region Constructors

		[CLSCompliant(false)]
		public EnumerableCursor(ICursor cursor)
		{
			_cursor = cursor;
		}

		#endregion

		#region IEnumerable implementation

		[CLSCompliant(false)]
		public IEnumerator<IRow> GetEnumerator()
		{
			return new CursorEnumerator(_cursor);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region Nested types

		private class CursorEnumerator : IEnumerator<IRow>
		{
			private ICursor _cursor;
			private IRow _row;

			public CursorEnumerator(ICursor cursor)
			{
				_cursor = cursor;
			}

			#region IEnumerator Members

			public void Reset()
			{
				// cannot reset _cursor
			}

			public void Dispose()
			{
				if (_cursor != null)
				{
					Marshal.ReleaseComObject(_cursor);
					_cursor = null;
				}
			}

			public IRow Current => _row;

			object IEnumerator.Current => Current;

			public bool MoveNext()
			{
				if (_cursor == null)
				{
					return false;
				}

				_row = _cursor.NextRow();
				if (_row != null)
				{
					return true;
				}
				else
				{
					Marshal.ReleaseComObject(_cursor);
					_cursor = null;
					return false;
				}
			}

			#endregion
		}

		#endregion
	}
}
