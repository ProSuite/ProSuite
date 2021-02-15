using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ValueRowProxy : RowProxy
	{
		private readonly object[] _values;

		/// <summary>
		/// Initializes a new instance of the <see cref="ValueRowProxy"/> class.
		/// </summary>
		/// <param name="values">The values.</param>
		/// <param name="table">The table.</param>
		/// <param name="oid">The oid.</param>
		public ValueRowProxy(object[] values, ITable table, int oid)
			: base(table, oid)
		{
			_values = values;
		}

		protected override object GetValueCore(int fieldIndex)
		{
			return _values[fieldIndex];
		}
	}
}
