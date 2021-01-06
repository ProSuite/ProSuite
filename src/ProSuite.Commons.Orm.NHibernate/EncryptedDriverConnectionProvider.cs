using System.Collections.Generic;
using NHibernate.Connection;
using ProSuite.Commons.Ado;
using ProSuite.Commons.Cryptography;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Orm.NHibernate
{
	[UsedImplicitly]
	public class EncryptedDriverConnectionProvider : DriverConnectionProvider
	{
		private const string _connectionStringKey =
			"hibernate.connection.connection_string";

		public override void Configure(IDictionary<string, string> settings)
		{
			if (settings.ContainsKey(_connectionStringKey))
			{
				string connectionString = settings[_connectionStringKey];

				if (connectionString != null)
				{
					using (StringEncryptor stringEncryptor = GetStringEncryptor())
					{
						string decryptedConnectionString;

						if (ConnectionStringEncryptionUtils.DecryptConnectionString(
							stringEncryptor, connectionString, out decryptedConnectionString))
						{
							settings[_connectionStringKey] = decryptedConnectionString;
						}
					}
				}
			}

			base.Configure(settings);
		}

		[NotNull]
		private StringEncryptor GetStringEncryptor()
		{
			return EncryptorFactory.GetConnectionStringEncryptor();
		}
	}
}
