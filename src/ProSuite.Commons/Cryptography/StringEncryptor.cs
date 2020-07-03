using System.Security.Cryptography;

namespace ProSuite.Commons.Cryptography
{
	public sealed class StringEncryptor : StringEncryptorBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SymmetricAlgorithm"/> class.
		/// </summary>
		public StringEncryptor(SymmetricAlgorithm encryption, byte[] key, byte[] iv)
			: base(encryption, key, iv) { }
	}
}
