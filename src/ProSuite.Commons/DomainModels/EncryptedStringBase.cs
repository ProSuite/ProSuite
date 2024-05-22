using System;
using ProSuite.Commons.Cryptography;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DomainModels
{
	public abstract class EncryptedStringBase : IEquatable<EncryptedStringBase>
	{
		[UsedImplicitly] private string _encryptedValue;

		protected abstract IStringEncryptor Encryptor { get; }

		public string PlainTextValue
		{
			get
			{
				string result;
				if (EncryptedValue == null)
				{
					result = null;
				}
				else
				{
					result = EncryptedValue == string.Empty
						         ? string.Empty
						         : Encryptor.Decrypt(EncryptedValue);
				}

				return result;
			}
			set
			{
				EncryptedValue = value == null
					                 ? null
					                 : Encryptor.Encrypt(value);
			}
		}

		public string EncryptedValue
		{
			get => _encryptedValue;
			set => _encryptedValue = value;
		}

		public bool Equals(EncryptedStringBase encryptedString)
		{
			if (encryptedString == null)
			{
				return false;
			}

			return Equals(EncryptedValue, encryptedString.EncryptedValue);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			return Equals(obj as EncryptedStringBase);
		}

		public override int GetHashCode()
		{
			return EncryptedValue != null
				       ? EncryptedValue.GetHashCode()
				       : 0;
		}
	}
}
