using System;

namespace ProSuite.Commons.Cryptography
{
	/// <summary>
	/// NOTE: This class must not be used in a production environment!
	/// The actual class that provides symmetric string encryptors is linked
	/// in the project file using the environment variable ProSuiteEncryptorFactoryDir
	/// which should be set to the directory that contains the actual implementation.
	/// This is only relevant when using
	/// - encrypted connection strings to connect to the data dictionary.
	/// - repository owner / schema owner connections in the data dictionary.
	/// </summary>
	public static class EncryptorFactory
	{
		/// <summary>
		/// The encryptor for strings in the data dictionary (passwords).
		/// </summary>
		/// <returns></returns>
		public static StringEncryptor GetDomainStringEncryptor()
		{
			// Example - take keys from a secure place in your environment:
			//return new StringEncryptor(
			//	new AesManaged(),
			//	new byte[]
			//	{
			//		0xd6, 0x42, 0x17, 0x12, 0xe8, 0x06, 0x5d, 0x83,
			//		0x5f, 0x23, 0xa4, 0x8c, 0x1f, 0x6a, 0x05, 0x5e
			//	},
			//	new byte[]
			//	{
			//		0x1f, 0xe8, 0x6d, 0xa6, 0xe1, 0x15, 0xb3, 0x25,
			//		0xca, 0x5f, 0xc8, 0xa5, 0x91, 0x5f, 0xf2, 0xcf
			//	});

			throw new NotImplementedException("No encryptor factory linked at compile time.");
		}

		/// <summary>
		/// The encryptor for connection strings (e.g. in config files).
		/// </summary>
		/// <returns></returns>
		public static StringEncryptor GetConnectionStringEncryptor()
		{
			throw new NotImplementedException("No encryptor factory linked at compile time.");
		}
	}
}
