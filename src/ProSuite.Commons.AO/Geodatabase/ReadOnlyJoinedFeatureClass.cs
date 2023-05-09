using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
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

		protected ReadOnlyJoinedFeatureClass([NotNull] IFeatureClass joinedTable,
		                                     [NotNull] IEnumerable<IReadOnlyTable> baseTables)
			: base(joinedTable)
		{
			_baseTables.AddRange(baseTables);
		}

		public override int FindField(string name)
		{
			return ReadOnlyJoinedTable.FindField(BaseTable, name);
		}

		#region Implementation of ITableBased

		public IList<IReadOnlyTable> GetBaseTables()
		{
			return _baseTables;
		}

		#endregion
	}
}
