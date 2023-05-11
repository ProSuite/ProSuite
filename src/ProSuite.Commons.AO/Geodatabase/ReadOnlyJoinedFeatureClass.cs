using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TableBased;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	/// ReadOnly FeatureClass implementation for AO table joins that provides adapted logic for the
	/// <see cref="FindField"/> method and an implementation of <see cref="ITableBased"/> in order
	/// to allow deterministic detection of the involved tables on which the join is based. 
	/// </summary>
	public class ReadOnlyJoinedFeatureClass : ReadOnlyFeatureClass, ITableBased
	{
		/// <summary>
		/// NOTE: Do not call this method directly, instead use <see cref="ReadOnlyTableFactory.CreateQueryTable"/>.
		/// </summary>
		/// <param name="joinedFeatureClass"></param>
		/// <param name="baseTables"></param>
		/// <returns></returns>
		internal static ReadOnlyJoinedFeatureClass Create(
			[NotNull] IFeatureClass joinedFeatureClass,
			[NotNull] IEnumerable<IReadOnlyTable> baseTables)
		{
			return new ReadOnlyJoinedFeatureClass(joinedFeatureClass, baseTables);
		}

		private readonly List<IReadOnlyTable> _baseTables = new List<IReadOnlyTable>(2);

		private ReadOnlyJoinedFeatureClass([NotNull] IFeatureClass joinedTable,
		                                   [NotNull] IEnumerable<IReadOnlyTable> baseTables)
			: base(joinedTable)
		{
			_baseTables.AddRange(baseTables);
		}

		public override int FindField(string name)
		{
			const bool allowUnQualifyFieldNames = true;
			return TableBasedUtils.FindFieldInJoin(BaseTable, name, allowUnQualifyFieldNames);
		}

		#region Implementation of ITableBased

		public IList<IReadOnlyTable> GetInvolvedTables()
		{
			return _baseTables;
		}

		public IEnumerable<Involved> GetInvolvedRows(IReadOnlyRow forTransformedRow)
		{
			// The OBJECTID field typically exists twice and un-qualifying the field name
			// can result in picking the wrong one, resulting in GEN-3538 (wrong involved row).
			const bool allowUnQualifyFieldNames = false;

			Func<string, int> findFieldFunc =
				fieldName =>
					TableBasedUtils.FindFieldInJoin(BaseTable, fieldName, allowUnQualifyFieldNames);

			return TableBasedUtils.GetInvolvedRowsFromJoinedRow(
				forTransformedRow, _baseTables, findFieldFunc);
		}

		#endregion
	}
}
