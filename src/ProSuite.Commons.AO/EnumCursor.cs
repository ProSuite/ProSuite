using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO
{
	public class EnumCursor : IEnumerable<IRow>
	{
		private readonly bool _isSpatialFilter;
		private readonly string _subFields;
		[NotNull] private readonly ITable _table;
		private readonly string _whereClause;
		private ICursor _cursor;

		#region Constructors

		private EnumCursor([NotNull] ITable table, [CanBeNull] IQueryFilter queryFilter)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			_table = table;

			GetFilterProperties(queryFilter, out _subFields, out _whereClause,
			                    out _isSpatialFilter);
		}

		public EnumCursor([NotNull] ISelectionSet selectionSet,
		                  [CanBeNull] IQueryFilter queryFilter,
		                  bool recycle) : this(selectionSet.Target, queryFilter)
		{
			Assert.ArgumentNotNull(selectionSet, nameof(selectionSet));

			try
			{
				selectionSet.Search(queryFilter, recycle, out _cursor);
			}
			catch (Exception e)
			{
				throw new DataException(
					CreateMessage(_table, _subFields, _whereClause, _isSpatialFilter), e);
			}
		}

		public EnumCursor([NotNull] ITable table,
		                  [CanBeNull] IQueryFilter queryFilter,
		                  bool recycle) : this(table, queryFilter)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			try
			{
				_cursor = GdbQueryUtils.OpenCursor(table, recycle, queryFilter);
			}
			catch (Exception e)
			{
				throw new DataException(
					CreateMessage(_table, _subFields, _whereClause, _isSpatialFilter), e);
			}
		}

		#endregion

		#region IEnumerable<IRow> Members

		public IEnumerator<IRow> GetEnumerator()
		{
			return new RowEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		~EnumCursor()
		{
			Dispose();
		}

		public static string CreateMessage([NotNull] ITable table,
		                                   [CanBeNull] IQueryFilter filter)
		{
			string fields;
			string where;
			bool spatial;
			GetFilterProperties(filter, out fields, out where, out spatial);

			return CreateMessage(table, fields, where, spatial);
		}

		private void Dispose()
		{
			if (_cursor == null)
			{
				return;
			}

			if (Marshal.IsComObject(_cursor))
			{
				Marshal.ReleaseComObject(_cursor);
			}
			else if (_cursor is IDisposable)
			{
				((IDisposable) _cursor).Dispose();
			}

			_cursor = null;
		}

		private static void GetFilterProperties([CanBeNull] IQueryFilter queryFilter,
		                                        out string subFields,
		                                        out string whereClause,
		                                        out bool isSpatialFilter)
		{
			if (queryFilter == null)
			{
				subFields = "*";
				whereClause = null;
				isSpatialFilter = false;
			}
			else
			{
				subFields = queryFilter.SubFields;
				whereClause = queryFilter.WhereClause;
				isSpatialFilter = queryFilter is ISpatialFilter;
			}
		}

		private string CreateMessage([CanBeNull] IRow row)
		{
			string msg =
				CreateMessage(_table, _subFields, _whereClause, _isSpatialFilter);

			if (row != null)
			{
				if (row.HasOID && _table.HasOID)
				{
					msg = msg + Environment.NewLine + "Last successful OID: " + row.OID;
				}
				else
				{
					msg = msg + Environment.NewLine + "(rows do not have OIDs)";
				}
			}
			else
			{
				msg = msg + Environment.NewLine + "(error before getting 1. row)";
			}

			return msg;
		}

		private static string CreateMessage([NotNull] ITable table,
		                                    [CanBeNull] string fields,
		                                    [CanBeNull] string where,
		                                    bool spatial)
		{
			var ds = table as IDataset;
			string name = "(unknown)";
			if (ds != null)
			{
				name = ds.Name;
			}

			string sSpatial = "";
			if (spatial)
			{
				sSpatial = " (spatially) ";
			}

			if (! string.IsNullOrEmpty(where))
			{
				where = "WHERE " + where;
			}

			return $"Error {sSpatial}querying {fields} FROM {name} {where}";
		}

		#region Nested type: RowEnumerator

		private class RowEnumerator : IEnumerator<IRow>
		{
			private readonly EnumCursor _enumerable;

			public RowEnumerator([NotNull] EnumCursor enumerable)
			{
				Assert.ArgumentNotNull(enumerable, nameof(enumerable));

				_enumerable = enumerable;
			}

			#region IEnumerator<IRow> Members

			void IEnumerator.Reset()
			{
				// cannot reset cursor
			}

			public void Dispose()
			{
				_enumerable.Dispose();
			}

			public IRow Current { get; private set; }

			object IEnumerator.Current => Current;

			bool IEnumerator.MoveNext()
			{
				if (_enumerable._cursor == null)
				{
					return false;
				}

				IRow lastRow = Current;
				try
				{
					Current = _enumerable._cursor.NextRow();
				}
				catch (Exception exp)
				{
					throw new DataException(_enumerable.CreateMessage(lastRow), exp);
				}

				if (Current != null)
				{
					return true;
				}

				Dispose();
				return false;
			}

			#endregion

			~RowEnumerator()
			{
				Dispose();
			}
		}

		#endregion
	}
}
