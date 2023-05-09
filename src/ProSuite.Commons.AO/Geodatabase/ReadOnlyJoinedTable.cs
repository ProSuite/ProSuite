using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	/// ReadOnly Table implementation for AO table joins that provides adapted logic for the
	/// FindField() method and an implementation of <see cref="ITableBased"/> in order
	/// to allow deterministic detection of the involved tables on which the join is based. 
	/// </summary>
	public class ReadOnlyJoinedTable : ReadOnlyTable, ITableBased
	{
		/// <summary>
		/// NOTE: Do not call this method directly, instead use <see cref="ReadOnlyTableFactory.CreateQueryTable"/>.
		/// </summary>
		/// <param name="joinedTable"></param>
		/// <param name="baseTables"></param>
		/// <returns></returns>
		internal static ReadOnlyJoinedTable Create(
			[NotNull] ITable joinedTable,
			[NotNull] IEnumerable<IReadOnlyTable> baseTables)
		{
			return new ReadOnlyJoinedTable(joinedTable, baseTables);
		}

		private readonly List<IReadOnlyTable> _baseTables = new List<IReadOnlyTable>(2);

		protected ReadOnlyJoinedTable([NotNull] ITable joinedTable,
		                              [NotNull] IEnumerable<IReadOnlyTable> baseTables)
			: base(joinedTable)
		{
			_baseTables.AddRange(baseTables);
		}

		public override int FindField(string name)
		{
			return FindField(BaseTable, name);
		}

		public static int FindField(ITable baseTable, string name)
		{
			int index = baseTable.FindField(name);
			if (index >= 0)
			{
				return index;
			}

			List<string> fieldNames = new List<string>();
			for (int iField = 0; iField < baseTable.Fields.FieldCount; iField++)
			{
				fieldNames.Add(baseTable.Fields.get_Field(iField).Name);
			}

			string searchName = name;
			while (searchName != null)
			{
				List<int> matchIndices = new List<int>();
				for (int iField = 0; iField < fieldNames.Count; iField++)
				{
					string fieldName = fieldNames[iField];
					if (fieldName.Equals(searchName, StringComparison.InvariantCultureIgnoreCase)
					    || fieldName.EndsWith($".{searchName}",
					                          StringComparison.InvariantCultureIgnoreCase))
					{
						matchIndices.Add(iField);
					}
				}

				if (matchIndices.Count == 1)
				{
					return matchIndices[0];
				}

				int sepIdx = searchName.IndexOf('.');
				if (sepIdx >= 0)
				{
					searchName = searchName.Substring(sepIdx + 1);
				}
				else
				{
					searchName = null;
				}
			}

			return -1;
		}

		#region Implementation of ITableBased

		public IList<IReadOnlyTable> GetBaseTables()
		{
			return _baseTables;
		}

		#endregion
	}
}
