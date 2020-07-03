using System;
using System.Security.Cryptography;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Cryptography
{
	public abstract class StringEncryptorBase : IStringEncryptor
	{
		private readonly SymmetricAlgorithm _encryption;

		private readonly byte[] _initializationVector;
		private readonly byte[] _secretKey;

		/// <summary>
		/// Initializes a new instance of the <see cref="StringEncryptorBase"/> class.
		/// </summary>
		/// <param name="encryption"></param>
		/// <param name="secretKey">The secret key.</param>
		/// <param name="initializationVector">The initialization vector.</param>
		protected StringEncryptorBase([NotNull] SymmetricAlgorithm encryption,
		                              [NotNull] byte[] secretKey,
		                              [NotNull] byte[] initializationVector)
		{
			Assert.ArgumentNotNull(encryption, nameof(encryption));
			Assert.ArgumentNotNull(secretKey, nameof(secretKey));
			Assert.ArgumentNotNull(initializationVector, nameof(initializationVector));

			_encryption = encryption;
			_secretKey = secretKey;
			_initializationVector = initializationVector;
		}

		#region Implementation of IStringEncryptor

		public string Decrypt(string encryptedString)
		{
			Assert.ArgumentNotNull(encryptedString, nameof(encryptedString));

			return CryptoUtils.Decrypt(encryptedString, _encryption, _secretKey,
			                           _initializationVector);
		}

		public string Encrypt(string plainTextString)
		{
			Assert.ArgumentNotNull(plainTextString, nameof(plainTextString));

			return CryptoUtils.Encrypt(plainTextString, _encryption, _secretKey,
			                           _initializationVector);
		}

		public void Dispose()
		{
			Array.Clear(_secretKey, 0, _secretKey.Length);
			Array.Clear(_initializationVector, 0, _secretKey.Length);
		}

		#endregion
	}
}