using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public class FieldIndexCache : IFieldIndexCache
	{
		[NotNull] private readonly Dictionary<ITable, FieldIndexes> _fieldIndexesByTable =
			new Dictionary<ITable, FieldIndexes>();

		[CLSCompliant(false)]
		public int GetFieldIndex(IObjectClass objectClass,
		                         string fieldName,
		                         AttributeRole role)
		{
			return GetFieldIndex((ITable) objectClass, fieldName, role);
		}

		[CLSCompliant(false)]
		public int GetFieldIndex(ITable table, string fieldName, AttributeRole role)
		{
			FieldIndexes fieldIndexes;
			if (! _fieldIndexesByTable.TryGetValue(table, out fieldIndexes))
			{
				fieldIndexes = new FieldIndexes(table);
				_fieldIndexesByTable.Add(table, fieldIndexes);
			}

			return fieldIndexes.GetFieldIndex(fieldName, role);
		}

		[CLSCompliant(false)]
		public int GetSubtypeFieldIndex(IObjectClass objectClass)
		{
			return GetSubtypeFieldIndex((ITable) objectClass);
		}

		[CLSCompliant(false)]
		public int GetSubtypeFieldIndex(ITable table)
		{
			FieldIndexes fieldIndexes;
			if (! _fieldIndexesByTable.TryGetValue(table, out fieldIndexes))
			{
				fieldIndexes = new FieldIndexes(table);
				_fieldIndexesByTable.Add(table, fieldIndexes);
			}

			return fieldIndexes.GetSubtypeFieldIndex();
		}

		[CLSCompliant(false)]
		public void Clear([NotNull] ITable table)
		{
			_fieldIndexesByTable.Remove(table);
		}

		public void ClearAll()
		{
			_fieldIndexesByTable.Clear();
		}

		private class FieldIndexes
		{
			[NotNull] private readonly ITable _table;

			private readonly Dictionary<string, int> _fieldIndexes = new Dictionary
				<string, int>(StringComparer.OrdinalIgnoreCase);

			private int? _subtypeFieldIndex;

			public FieldIndexes([NotNull] ITable table)
			{
				Assert.ArgumentNotNull(table, nameof(table));

				_table = table;
			}

			public int GetFieldIndex([NotNull] string fieldName, [CanBeNull] AttributeRole role)
			{
				int fieldIndex;
				if (! _fieldIndexes.TryGetValue(fieldName, out fieldIndex))
				{
					fieldIndex = AttributeUtils.GetFieldIndex(_table, fieldName, role);
					_fieldIndexes.Add(fieldName, fieldIndex);
				}

				return fieldIndex;
			}

			public int GetSubtypeFieldIndex()
			{
				if (_subtypeFieldIndex == null)
				{
					_subtypeFieldIndex = DatasetUtils.GetSubtypeFieldIndex(_table);
				}

				return _subtypeFieldIndex.Value;
			}
		}
	}
}
