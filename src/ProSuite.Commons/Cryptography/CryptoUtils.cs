using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Cryptography
{
	/// <summary>
	/// Utility methods for encrypting and decrypting.
	/// </summary>
	public static class CryptoUtils
	{
		[NotNull]
		public static string ComputeSHA1ForFile([NotNull] string filePath)
		{
			Assert.ArgumentNotNullOrEmpty(filePath, nameof(filePath));

			using (var fileStream = new FileStream(filePath, FileMode.Open))
			{
				using (var sha1 = new SHA1Managed())
				{
					byte[] hash = sha1.ComputeHash(fileStream);
					var formatted = new StringBuilder(hash.Length);
					foreach (byte b in hash)
					{
						formatted.AppendFormat("{0:X2}", b);
					}

					return formatted.ToString();
				}
			}
		}

		/// <summary>
		/// Symmetric encryption.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="encryption">The symmetric encryption algorithm.</param>
		/// <param name="key">The secret key to be used for the symmetric algorithm.</param>
		/// <param name="iv">The initialization vector.</param>
		/// <returns>The encrypted string.</returns>
		[NotNull]
		public static string Encrypt([NotNull] string text,
		                             [NotNull] SymmetricAlgorithm encryption,
		                             [NotNull] byte[] key,
		                             [NotNull] byte[] iv)
		{
			var asciiEncoding = new ASCIIEncoding();

			byte[] encryptedBytes = asciiEncoding.GetBytes(text);

			using (ICryptoTransform cryptoTransform =
			       encryption.CreateEncryptor(key, iv))
			{
				using (var memoryStream = new MemoryStream())
				{
					using (var cryptoStream = new CryptoStream(
						       memoryStream, cryptoTransform, CryptoStreamMode.Write))
					{
						cryptoStream.Write(encryptedBytes, 0, encryptedBytes.Length);
						cryptoStream.FlushFinalBlock();

						encryptedBytes = memoryStream.ToArray();
					}
				}
			}

			string encryptedString = string.Empty;
			foreach (byte b in encryptedBytes)
			{
				encryptedString += $"{b:x2}";
			}

			return encryptedString;
		}

		/// <summary>
		/// Symmetric rijndael decryption of a string.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="encryption">The symmetric encryption algorithm.</param>
		/// <param name="key">The secret key to be used for the symmetric algorithm.</param>
		/// <param name="iv">The initialization vector.</param>
		/// <returns>The decrypted string.</returns>
		[NotNull]
		public static string Decrypt([NotNull] string text,
		                             [NotNull] SymmetricAlgorithm encryption,
		                             [NotNull] byte[] key,
		                             [NotNull] byte[] iv)
		{
			var encryptedBytes = new byte[text.Length / 2];

			// convert the text to bytes and write it to a stream
			for (int i = 0; i < text.Length / 2; i++)
			{
				encryptedBytes[i] = Convert.ToByte(text.Substring(2 * i, 2), 16);
			}

			return Decrypt(encryptedBytes, encryption, key, iv);
		}

		/// <summary>
		/// Symmetric rijndael decryption of a byte array.
		/// </summary>
		/// <param name="encryptedBytes">The encrypted bytes.</param>
		/// <param name="encryption">The symmetric encryption algorithm.</param>
		/// <param name="key">The secret key to be used for the symmetric algorithm.</param>
		/// <param name="iv">The initialization vector.</param>
		/// <returns>The decrypted string.</returns>
		[NotNull]
		public static string Decrypt([NotNull] byte[] encryptedBytes,
		                             [NotNull] SymmetricAlgorithm encryption,
		                             [NotNull] byte[] key,
		                             [NotNull] byte[] iv)
		{
			using (ICryptoTransform cryptoTransform =
			       encryption.CreateDecryptor(key, iv))
			{
				using (var memoryStream = new MemoryStream(encryptedBytes))
				{
					memoryStream.Seek(0, SeekOrigin.Begin);

					// decrypt the stream.
					using (var cryptoStream = new CryptoStream(
						       memoryStream, cryptoTransform, CryptoStreamMode.Read))
					{
						//Read the stream.
						var reader = new StreamReader(cryptoStream);

						return reader.ReadToEnd();
					}
				}
			}
		}
	}
}
