using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Cryptography
{
	public interface IStringEncryptor : IDisposable
	{
		[NotNull]
		string Decrypt([NotNull] string encryptedString);

		[NotNull]
		string Encrypt([NotNull] string plainTextString);
	}
}