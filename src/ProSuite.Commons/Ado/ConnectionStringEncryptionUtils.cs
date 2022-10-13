using System.Collections.Generic;
using ProSuite.Commons.Cryptography;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Ado
{
	public static class ConnectionStringEncryptionUtils
	{
		public static bool DecryptConnectionString(
			[NotNull] IStringEncryptor stringEncryptor,
			[NotNull] string connectionString,
			[NotNull] out string decryptedConnectionString)
		{
			var builder = new ConnectionStringBuilder(connectionString);

			bool changed = false;
			foreach (KeyValuePair<string, string> pair in builder.GetEntries())
			{
				string keyword = pair.Key;
				string value = pair.Value;

				string innerValue;
				if (! PropertyValueEncryptionUtils.IsEncryptedValue(value, out innerValue))
				{
					continue;
				}

				builder.Update(keyword, stringEncryptor.Decrypt(innerValue));
				changed = true;
			}

			decryptedConnectionString = changed
				                            ? builder.ConnectionString
				                            : connectionString;
			return changed;
		}
	}
}
