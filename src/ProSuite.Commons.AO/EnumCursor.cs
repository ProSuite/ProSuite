using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO
{
	public class EnumCursor : IEnumerable<IRow>
	{
		private readonly bool _provideFailingOidInException;
		private readonly bool _isSpatialFilter;
		private readonly string _subFields;
		[NotNull] private readonly ITable _table;
		private readonly string _whereClause;
		private ICursor _cursor;

		private readonly IQueryFilter _queryFilter;

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
					CreateMessage(GetTableName(_table), _subFields, _whereClause, _isSpatialFilter),
					e);
			}
		}

		public EnumCursor([NotNull] ITable table, [NotNull] ICursor cursor)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(cursor, nameof(cursor));
			_table = table;
			_cursor = cursor;
		}

		public EnumCursor([NotNull] ITable table,
		                  [CanBeNull] IQueryFilter queryFilter,
		                  bool recycle,
		                  bool provideFailingOidInException = false)
			: this(table, queryFilter)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			_provideFailingOidInException = provideFailingOidInException;

			if (provideFailingOidInException)
			{
				_queryFilter = queryFilter;
			}

			try
			{
				_cursor = GdbQueryUtils.OpenCursor(table, recycle, queryFilter);
			}
			catch (Exception e)
			{
				throw new DataException(
					CreateMessage(GetTableName(_table), _subFields, _whereClause, _isSpatialFilter),
					e);
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

		public static string CreateMessage([NotNull] IReadOnlyTable table,
		                                   [CanBeNull] ITableFilter filter)
			=> CreateMessage(table.Name, TableFilterUtils.GetQueryFilter(filter));

		public static string CreateMessage([NotNull] ITable table,
		                                   [CanBeNull] IQueryFilter filter)
			=> CreateMessage(GetTableName(table), filter);

		public static string CreateMessage([NotNull] string tableName,
		                                   [CanBeNull] IQueryFilter filter)
		{
			string fields;
			string where;
			bool spatial;
			GetFilterProperties(filter, out fields, out where, out spatial);

			return CreateMessage(tableName, fields, where, spatial);
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
			else if (_cursor is IDisposable disposable)
			{
				disposable.Dispose();
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
				CreateMessage(GetTableName(_table), _subFields, _whereClause, _isSpatialFilter);

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

		private static string GetTableName(ITable table) =>
			table is IDataset ds ? ds.Name : "(unknown)";

		private static string CreateMessage([NotNull] string tableName,
		                                    [CanBeNull] string fields,
		                                    [CanBeNull] string where,
		                                    bool spatial)
		{
			string name = tableName;
			string spatially = spatial ? "(spatially) " : string.Empty;

			if (! string.IsNullOrEmpty(where))
			{
				where = "WHERE " + where;
			}

			return $"Error {spatially}querying {fields} FROM {name} {where}";
		}

		#region Nested type: RowEnumerator

		private class RowEnumerator : IEnumerator<IRow>
		{
			private static readonly IMsg _msg = Msg.ForCurrentClass();

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
					if (_enumerable._provideFailingOidInException)
					{
						// Try finding out if it is just an empty geometry and if so which one -> catchable exception
						ITable table = _enumerable._table;

						long? oid = TryGetInvalidOid(table, _enumerable._queryFilter);

						if (oid != null)
						{
							string tableName = GetTableName(table);

							throw new DataAccessException(
								$"Error getting {tableName} <oid> {oid}. Its geometry might be corrupt.",
								oid.Value, tableName, exp);
						}
					}

					throw new DataException(_enumerable.CreateMessage(lastRow), exp);
				}

				if (Current != null)
				{
					return true;
				}

				Dispose();
				return false;
			}

			private static long? TryGetInvalidOid([NotNull] ITable table,
			                                      [CanBeNull] IQueryFilter filter)
			{
				try
				{
					if (! table.HasOID)
					{
						return null;
					}

					IQueryFilter oidOnlyFilter
						= filter == null
							  ? new QueryFilterClass()
							  : (IQueryFilter) ((IClone) filter).Clone();

					oidOnlyFilter.SubFields = table.OIDFieldName;

					long lastOid = -1;
					try
					{
						foreach (IRow row in GdbQueryUtils.GetRows(table, oidOnlyFilter, true))
						{
							try
							{
								var fullRow = table.GetRow(row.OID);
								lastOid = row.OID;

								Marshal.ReleaseComObject(fullRow);
							}
							catch (Exception e)
							{
								_msg.Debug($"Error at row {row.OID}. Last successful: {lastOid}",
								           e);

								return row.OID;
							}
						}
					}
					catch (COMException comException)
					{
						_msg.Debug(
							$"Unexpected Error reading objects. Last successful OID: {lastOid}",
							comException);

						return null;
					}

					_msg.DebugFormat("The exception did not occur any more!");
					return null;
				}
				catch (Exception e)
				{
					_msg.Debug("Error in exception handling", e);
					return null;
				}
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
