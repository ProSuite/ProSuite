﻿using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA
{
	public class RowsWithStopConditions
	{
		private readonly Dictionary<RowReference, StopInfo> _rowsWithStopConditions
			= new Dictionary<RowReference, StopInfo>();

		public void Add([NotNull] string tableName,
		                int objectID,
		                [NotNull] StopInfo stopInfo)
		{
			var rowReference = new RowReference(tableName, objectID);

			if (_rowsWithStopConditions.ContainsKey(rowReference))
			{
				return;
			}

			_rowsWithStopConditions.Add(rowReference, stopInfo);
		}

		[CanBeNull]
		[CLSCompliant(false)]
		public StopInfo GetStopInfo([NotNull] IRow row)
		{
			if (! row.HasOID)
			{
				return null;
			}

			var rowReference = new RowReference(DatasetUtils.GetName(row.Table), row.OID);

			StopInfo stopInfo;
			return _rowsWithStopConditions.TryGetValue(rowReference, out stopInfo)
				       ? stopInfo
				       : null;
		}

		[NotNull]
		public IEnumerable<RowWithStopCondition> GetRowsWithStopConditions()
		{
			foreach (KeyValuePair<RowReference, StopInfo> pair
				in _rowsWithStopConditions)
			{
				RowReference rowReference = pair.Key;
				StopInfo stopInfo = pair.Value;

				yield return new RowWithStopCondition(rowReference.TableName,
				                                      rowReference.OID,
				                                      stopInfo);
			}
		}

		public int Count => _rowsWithStopConditions.Count;

		public void Clear()
		{
			_rowsWithStopConditions.Clear();
		}

		private class RowReference : IEquatable<RowReference>
		{
			private readonly string _tableName;

			private readonly int _oid;
			// might be another key, in this case use string with keyValue.ToString() 

			public RowReference([NotNull] string tableName, int oid)
			{
				_tableName = tableName;
				_oid = oid;
			}

			[NotNull]
			public string TableName => _tableName;

			public int OID => _oid;

			public bool Equals(RowReference other)
			{
				if (ReferenceEquals(null, other))
				{
					return false;
				}

				if (ReferenceEquals(this, other))
				{
					return true;
				}

				return _tableName.Equals(other._tableName, StringComparison.OrdinalIgnoreCase) &&
				       other._oid == _oid;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj))
				{
					return false;
				}

				if (ReferenceEquals(this, obj))
				{
					return true;
				}

				if (obj.GetType() != typeof(RowReference))
				{
					return false;
				}

				return Equals((RowReference) obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (_tableName.GetHashCode() * 397) ^ _oid;
				}
			}
		}
	}
}