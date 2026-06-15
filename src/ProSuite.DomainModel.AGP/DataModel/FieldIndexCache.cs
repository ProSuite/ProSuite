using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AGP.DataModel;

public class FieldIndexCache
{
	[NotNull] private readonly Dictionary<Table, FieldIndexes> _fieldIndexesByTable =
		new Dictionary<Table, FieldIndexes>();

	public int GetFieldIndex(Table table, string fieldName, AttributeRole role = null)
	{
		FieldIndexes fieldIndexes;
		if (! _fieldIndexesByTable.TryGetValue(table, out fieldIndexes))
		{
			fieldIndexes = new FieldIndexes(table);
			_fieldIndexesByTable.Add(table, fieldIndexes);
		}

		return fieldIndexes.GetFieldIndex(fieldName, role);
	}

	public int GetSubtypeFieldIndex(Table table)
	{
		FieldIndexes fieldIndexes;
		if (! _fieldIndexesByTable.TryGetValue(table, out fieldIndexes))
		{
			fieldIndexes = new FieldIndexes(table);
			_fieldIndexesByTable.Add(table, fieldIndexes);
		}

		return fieldIndexes.GetSubtypeFieldIndex();
	}

	public void Clear([NotNull] Table table)
	{
		_fieldIndexesByTable.Remove(table);
	}

	public void ClearAll()
	{
		_fieldIndexesByTable.Clear();
	}

	private class FieldIndexes
	{
		[NotNull] private readonly Table _table;

		private readonly Dictionary<string, int> _fieldIndexes = new Dictionary
			<string, int>(StringComparer.OrdinalIgnoreCase);

		private int? _subtypeFieldIndex;

		public FieldIndexes([NotNull] Table table)
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
