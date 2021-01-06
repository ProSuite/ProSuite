using System.Data;
using NHibernate.Dialect;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Orm.NHibernate.Dialect
{
	/// <summary>
	/// Oracle dialect that overrides some aspects of the standard NH oracle dialect.
	/// - DateTime properties are by default mapped to DATE field instead TIMESTAMP(4)
	/// </summary>
	[UsedImplicitly]
	public class OracleDialect : Oracle12cDialect
	{
		public OracleDialect()
		{
			RegisterColumnType(DbType.DateTime, "DATE");
		}
	}
}