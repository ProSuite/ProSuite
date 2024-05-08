using System;
using ProSuite.Commons.Cryptography;

namespace ProSuite.Commons.DomainModels
{
	public abstract class EncryptedStringBase : IEquatable<EncryptedStringBase>
	{
		private string _encryptedValue;

		protected abstract IStringEncryptor Encryptor { get; }

		public string PlainTextValue
		{
			get
			{
				string result;
				if (_encryptedValue == null)
				{
					result = null;
				}
				else
				{
					result = _encryptedValue == string.Empty
						         ? string.Empty
						         : Encryptor.Decrypt(_encryptedValue);
				}

				return result;
			}
			set
			{
				_encryptedValue = value == null
					                  ? null
					                  : Encryptor.Encrypt(value);
			}
		}

		public string EncryptedValue
		{
			get { return _encryptedValue; }
			set
			{
				_encryptedValue = value;
			}
		}

		public bool Equals(EncryptedStringBase encryptedString)
		{
			if (encryptedString == null)
			{
				return false;
			}

			return Equals(_encryptedValue, encryptedString._encryptedValue);
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
			return _encryptedValue != null
				       ? _encryptedValue.GetHashCode()
				       : 0;
		}
	}
}
