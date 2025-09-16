using System;
using ProSuite.DomainModel.Core.Geodatabase;
using ProSuite.Shared.IoCRoot;

namespace ProSuite.Commons.Orm.NHibernate
{
	public class NhUtils
	{
		/// <summary>
		/// Returns an NHibernate connection for the given connection provider.
		/// </summary>
		public static NhConfiguration ToNhConfiguration(ConnectionProvider connectionProvider,
		                                              string schemaOwner)
		{
			string connectionString;
			if (connectionProvider is SdeDirectOsaConnectionProvider osaConnection)
			{
				connectionString = $"User Id=/;Data Source={osaConnection.DatabaseName}";

				return new NhConfiguration
				       {
					       DatabaseType = osaConnection.DbmsTypeName,
					       Connection = connectionString,
					       DefaultSchema = schemaOwner
				       };
			}

			throw new NotImplementedException("Currently only SdeDirectOsaConnectionProvider can be converted to NhConfiguration");
		}
	}
}
