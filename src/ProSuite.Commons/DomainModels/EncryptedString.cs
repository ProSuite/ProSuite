using ProSuite.Commons.Cryptography;

namespace ProSuite.Commons.DomainModels
{
	public sealed class EncryptedString : EncryptedStringBase
	{
		protected override IStringEncryptor Encryptor =>
			EncryptorFactory.GetDomainStringEncryptor();
	}
}