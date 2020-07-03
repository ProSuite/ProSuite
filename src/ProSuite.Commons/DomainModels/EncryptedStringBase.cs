using System;
using ProSuite.Commons.Cryptography;

namespace ProSuite.Commons.DomainModels
{
	public abstract class EncryptedStringBase : IEquatable<EncryptedStringBase>
	{
		private string _encryptedValue;
		private bool _plainTextKnown;
		private string _plainTextValue = string.Empty;

		protected abstract IStringEncryptor Encryptor { get; }

		public string PlainTextValue
		{
			get
			{
				if (! _plainTextKnown)
				{
					if (_encryptedValue == null)
					{
						_plainTextValue = null;
					}
					else
					{
						_plainTextValue = _encryptedValue == string.Empty
							                  ? string.Empty
							                  : Encryptor.Decrypt(_encryptedValue);
					}

					_plainTextKnown = true;
				}

				return _plainTextValue;
			}
			set
			{
				_plainTextValue = value;
				_encryptedValue = _plainTextValue == null
					                  ? null
					                  : Encryptor.Encrypt(value);

				_plainTextKnown = true;
			}
		}

		public string EncryptedValue
		{
			get { return _encryptedValue; }
			set
			{
				_encryptedValue = value;
				_plainTextKnown = false;
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