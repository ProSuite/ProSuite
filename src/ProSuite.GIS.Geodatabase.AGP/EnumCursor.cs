//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Data;
//using System.Runtime.InteropServices;
//using ESRI.ArcGIS.Geodatabase.AO;
//using ProSuite.Commons.Essentials.Assertions;
//using ProSuite.Commons.Essentials.CodeAnnotations;

//namespace ESRI.ArcGIS.Geodatabase
//{
//	extern alias EsriGeodatabase;

//	internal class EnumCursor : IEnumerable<IRow>
//	{
//		private readonly bool _provideFailingOidInException;
//		private readonly bool _isSpatialFilter;
//		private readonly string _subFields;
//		[NotNull] private readonly ITable _table;
//		private readonly string _whereClause;
//		private EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ICursor _cursor;

//		private readonly IQueryFilter _queryFilter;

//		#region Constructors

//		private EnumCursor([NotNull] ITable table, [CanBeNull] IQueryFilter queryFilter)
//		{
//			Assert.ArgumentNotNull(table, nameof(table));

//			_table = table;

//			GetFilterProperties(queryFilter, out _subFields, out _whereClause,
//				out _isSpatialFilter);
//		}

//		internal EnumCursor([NotNull] EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ISelectionSet selectionSet,
//			[CanBeNull] IQueryFilter queryFilter,
//			bool recycle) : this(ToArcTable(selectionSet.Target), queryFilter)
//		{
//			Assert.ArgumentNotNull(selectionSet, nameof(selectionSet));

//			try
//			{
//				var aoQueryFilter = ((ArcQueryFilter)queryFilter)?.AoQueryFilter;

//				selectionSet.Search(aoQueryFilter, recycle, out _cursor);
//			}
//			catch (Exception e)
//			{
//				throw new DataException(
//					CreateMessage(GetTableName(_table), _subFields, _whereClause, _isSpatialFilter),
//					e);
//			}
//		}

//		internal EnumCursor([NotNull] ITable table, [NotNull] EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ICursor cursor)
//		{
//			Assert.ArgumentNotNull(table, nameof(table));
//			Assert.ArgumentNotNull(cursor, nameof(cursor));
//			_table = table;
//			_cursor = cursor;
//		}

//		public EnumCursor([NotNull] ITable table,
//			[CanBeNull] IQueryFilter queryFilter,
//			bool recycle,
//			bool provideFailingOidInException = false)
//			: this(table, queryFilter)
//		{
//			Assert.ArgumentNotNull(table, nameof(table));

//			_provideFailingOidInException = provideFailingOidInException;

//			if (provideFailingOidInException)
//			{
//				_queryFilter = queryFilter;
//			}

//			try
//			{
//				_cursor = table.Search(queryFilter, recycle);
//			}
//			catch (Exception e)
//			{
//				throw new DataException(
//					CreateMessage(GetTableName(_table), _subFields, _whereClause, _isSpatialFilter),
//					e);
//			}
//		}

//		#endregion

//		#region IEnumerable<IRow> Members

//		public IEnumerator<IRow> GetEnumerator()
//		{
//			return new RowEnumerator(this);
//		}

//		IEnumerator IEnumerable.GetEnumerator()
//		{
//			return GetEnumerator();
//		}

//		#endregion

//		~EnumCursor()
//		{
//			Dispose();
//		}

//		private void Dispose()
//		{
//			if (_cursor == null)
//			{
//				return;
//			}

//			if (Marshal.IsComObject(_cursor))
//			{
//				Marshal.ReleaseComObject(_cursor);
//			}
//			else if (_cursor is IDisposable disposable)
//			{
//				disposable.Dispose();
//			}

//			_cursor = null;
//		}

//		private static void GetFilterProperties([CanBeNull] IQueryFilter queryFilter,
//			out string subFields,
//			out string whereClause,
//			out bool isSpatialFilter)
//		{
//			if (queryFilter == null)
//			{
//				subFields = "*";
//				whereClause = null;
//				isSpatialFilter = false;
//			}
//			else
//			{
//				subFields = queryFilter.SubFields;
//				whereClause = queryFilter.WhereClause;
//				isSpatialFilter = queryFilter is EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ISpatialFilter;
//			}
//		}

//		private string CreateMessage([CanBeNull] IRow row)
//		{
//			string msg =
//				CreateMessage(GetTableName(_table), _subFields, _whereClause, _isSpatialFilter);

//			if (row != null)
//			{
//				if (row.HasOID && _table.HasOID)
//				{
//					msg = msg + Environment.NewLine + "Last successful OID: " + row.OID;
//				}
//				else
//				{
//					msg = msg + Environment.NewLine + "(rows do not have OIDs)";
//				}
//			}
//			else
//			{
//				msg = msg + Environment.NewLine + "(error before getting 1. row)";
//			}

//			return msg;
//		}

//		private static string GetTableName(ITable table) =>
//			table is IDataset ds ? ds.Name : "(unknown)";

//		private static string CreateMessage([NotNull] string tableName,
//			[CanBeNull] string fields,
//			[CanBeNull] string where,
//			bool spatial)
//		{
//			string name = tableName;
//			string spatially = spatial ? "(spatially) " : string.Empty;

//			if (!string.IsNullOrEmpty(where))
//			{
//				where = "WHERE " + where;
//			}

//			return $"Error {spatially}querying {fields} FROM {name} {where}";
//		}

//		#region Nested type: RowEnumerator

//		private class RowEnumerator : IEnumerator<IRow>
//		{
//			private readonly EnumCursor _enumerable;

//			public RowEnumerator([NotNull] EnumCursor enumerable)
//			{
//				Assert.ArgumentNotNull(enumerable, nameof(enumerable));

//				_enumerable = enumerable;
//			}

//			#region IEnumerator<IRow> Members

//			void IEnumerator.Reset()
//			{
//				// cannot reset cursor
//			}

//			public void Dispose()
//			{
//				_enumerable.Dispose();
//			}

//			public IRow Current { get; private set; }

//			object IEnumerator.Current => Current;

//			bool IEnumerator.MoveNext()
//			{
//				if (_enumerable._cursor == null)
//				{
//					return false;
//				}

//				IRow lastRow = Current;
//				try
//				{
//					EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRow nextRow = _enumerable._cursor.NextRow();
//					Current = nextRow is EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeature f
//						? new ArcFeature(f)
//						: new ArcRow(nextRow);
//				}
//				catch (Exception e)
//				{
//					throw;
//				}

//				if (Current != null)
//				{
//					return true;
//				}

//				Dispose();
//				return false;
//			}

//			#endregion

//			~RowEnumerator()
//			{
//				Dispose();
//			}
//		}

//		#endregion

//		private static ITable ToArcTable(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ITable aoTable)
//		{
//			var result = aoTable is EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeatureClass featureClass
//				? new ArcFeatureClass(featureClass)
//				: (ITable)new ArcTable((EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ITable)aoTable);

//			return result;
//		}
//	}
//}


