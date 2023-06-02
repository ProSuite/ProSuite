namespace ProSuite.Commons.GeoDb
{
	public interface ITableFilter
	{
		string SubFields { get; set; }

		bool AddField(string field);

		string WhereClause { get; set; }
		string PostfixClause { get; set; }

		ITableFilter Clone();
	}
}
