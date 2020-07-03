using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Cryptography
{
	/// <summary>
	/// Utilities for handling encrypted property values
	/// </summary>
	public static class PropertyValueEncryptionUtils
	{
		private const string _encryptedValuePrefix = "ENC(";
		private const string _encryptedValueSuffix = ")";

		[NotNull]
		public static string WrapEncryptedValue([NotNull] string encryptedValue)
		{
			Assert.ArgumentNotNull(encryptedValue, nameof(encryptedValue));

			return string.Format("{0}{1}{2}",
			                     _encryptedValuePrefix,
			                     encryptedValue.Trim(),
			                     _encryptedValueSuffix);
		}

		public static bool IsEncryptedValue([NotNull] string value,
		                                    [NotNull] out string innerValue)
		{
			Assert.ArgumentNotNull(value, nameof(value));

			string trimmedValue = value.Trim();

			if (IsEncryptedValue(trimmedValue))
			{
				innerValue = GetInnerEncryptedValue(trimmedValue);
				return true;
			}

			innerValue = value;
			return false;
		}

		private static bool IsEncryptedValue([NotNull] string trimmedValue)
		{
			Assert.ArgumentNotNull(trimmedValue, nameof(trimmedValue));

			return trimmedValue.StartsWith(_encryptedValuePrefix) &&
			       trimmedValue.EndsWith(_encryptedValueSuffix);
		}

		private static string GetInnerEncryptedValue([NotNull] string trimmedValue)
		{
			Assert.ArgumentNotNull(trimmedValue, nameof(trimmedValue));

			int length = trimmedValue.Length - _encryptedValueSuffix.Length -
			             _encryptedValuePrefix.Length;

			return trimmedValue.Substring(_encryptedValuePrefix.Length, length);
		}
	}
}