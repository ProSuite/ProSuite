using System.Security.Cryptography;
using NUnit.Framework;
using ProSuite.Commons.Cryptography;
using ProSuite.Commons.DomainModels;

namespace ProSuite.Commons.Test.DomainModels
{
	[TestFixture]
	public class EncryptedStringTest
	{
		[Test]
		public void CanDecrypt()
		{
			var encryptedString =
				new TestEncryptedString
				{
					EncryptedValue = "6a713b04fe15c0c7c05bb97bf2a5951e"
				};

			Assert.AreEqual("blah", encryptedString.PlainTextValue);
		}

		[Test]
		public void CanDecryptEmpty()
		{
			var encryptedString = new TestEncryptedString {EncryptedValue = string.Empty};

			Assert.AreEqual("", encryptedString.PlainTextValue);
		}

		[Test]
		public void CanDecryptNull()
		{
			var encryptedString = new TestEncryptedString {EncryptedValue = null};

			Assert.IsNull(encryptedString.EncryptedValue);
		}

		[Test]
		public void CanEncrypt()
		{
			var encryptedString = new TestEncryptedString {PlainTextValue = "blah"};

			Assert.AreEqual("6a713b04fe15c0c7c05bb97bf2a5951e",
			                encryptedString.EncryptedValue);
		}

		[Test]
		public void CanEncryptEmpty()
		{
			var encryptedString = new TestEncryptedString {PlainTextValue = string.Empty};

			Assert.AreEqual("029b3a7f7ef02ccbe13edbb3a23015f8",
			                encryptedString.EncryptedValue);
		}

		[Test]
		public void CanEncryptNull()
		{
			var encryptedString = new TestEncryptedString {PlainTextValue = null};

			Assert.IsNull(encryptedString.EncryptedValue);
		}

		private class TestEncryptedString : EncryptedStringBase
		{
			#region Fields

			private static readonly byte[] _iv =
			{
				0x12, 0xe1, 0xa1, 0x54, 0x2f, 0x08, 0x41, 0xd2,
				0x86, 0x68, 0xec, 0x2d, 0x01, 0x5c, 0x32, 0x7e
			};

			private static readonly byte[] _key =
			{
				0xB1, 0xA2, 0x9F, 0xF2, 0xFC, 0xD5, 0x2B, 0xDD,
				0x4F, 0x45, 0x13, 0xA0, 0xDD, 0xA1, 0x00, 0xE1
			};

			#endregion

			protected override IStringEncryptor Encryptor =>
				new StringEncryptor(new AesManaged(), _key, _iv);
		}
	}
}