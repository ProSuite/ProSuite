using NUnit.Framework;
using ProSuite.Commons.Cryptography;

namespace ProSuite.DomainModel.Core.Test.DataModel
{
	[TestFixture]
	public class EncryptorFactoryTest
	{
		[Test]
		public void CanGetConnectionStringEncryptor()
		{
			string encrypted = "9feaee773b54050146af33df533fcb8c";
			string plainText = "pa$$w0rd";

			StringEncryptor encryptor = EncryptorFactory.GetConnectionStringEncryptor();

			Assert.AreEqual(plainText, encryptor.Decrypt(encrypted));
		}

		[Test]
		public void CanGetDomainStringEncryptor()
		{
			string encrypted = "8eeb05da53fbf39650bd088ed233de61";
			string plainText = "pa$$w0rd";

			StringEncryptor encryptor = EncryptorFactory.GetDomainStringEncryptor();

			Assert.AreEqual(plainText, encryptor.Decrypt(encrypted));
		}
	}
}
