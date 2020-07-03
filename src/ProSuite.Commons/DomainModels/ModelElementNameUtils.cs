using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.DomainModels
{
	/// <summary>
	/// Utility methods for model elements
	/// </summary>
	public static class ModelElementNameUtils
	{
		private const char _nameSeparator = '.';

		public static bool IsQualifiedName([NotNull] string name)
		{
			int index = name.IndexOf(_nameSeparator);

			return index > 0 && index < name.Length - 1;
		}

		[NotNull]
		public static string GetUnqualifiedName([NotNull] string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			string[] tokens = name.Split(_nameSeparator);

			return tokens[tokens.Length - 1];
		}

		public static bool TryUnqualifyName([NotNull] string datasetName,
		                                    [NotNull] out string unqualifiedName)
		{
			Assert.ArgumentNotNullOrEmpty(datasetName, nameof(datasetName));

			int index = datasetName.LastIndexOf(_nameSeparator);
			if (index >= 0)
			{
				int startIndex = index + 1;
				if (startIndex < datasetName.Length)
				{
					string unqualified = datasetName.Substring(startIndex).Trim();

					if (unqualified.Length > 0)
					{
						unqualifiedName = unqualified;
						return true;
					}
				}
			}

			unqualifiedName = datasetName;
			return false;
		}

		[NotNull]
		public static string GetQualifiedName([CanBeNull] string masterDatabaseName,
		                                      [CanBeNull] string schemaOwner,
		                                      [NotNull] string modelElementName)
		{
			Assert.ArgumentNotNullOrEmpty(modelElementName, nameof(modelElementName));

			var tokens = new List<string>();

			if (masterDatabaseName != null && StringUtils.IsNotEmpty(masterDatabaseName))
			{
				tokens.Add(masterDatabaseName.Trim());
			}

			if (schemaOwner != null && StringUtils.IsNotEmpty(schemaOwner))
			{
				tokens.Add(schemaOwner.Trim());
			}

			tokens.Add(modelElementName);

			return StringUtils.Concatenate(tokens, _nameSeparator.ToString());
		}

		[NotNull]
		public static string GetOwnerName([NotNull] string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			string[] tokens = name.Split(_nameSeparator);

			switch (tokens.Length)
			{
				case 1:
					return string.Empty; // table

				case 2:
					return tokens[0]; // OWNER.table

				case 3:
					return tokens[1]; // db.OWNER.table

				default:
					throw new ArgumentException(
						$"Unexpected token count: {tokens.Length} ({name})");
			}
		}

		[NotNull]
		public static string GetNameWithoutCatalogPart([NotNull] string fullName)
		{
			Assert.ArgumentNotNullOrEmpty(fullName, nameof(fullName));

			string[] tokens = fullName.Split(_nameSeparator);

			if (tokens.Length < 3)
			{
				return fullName;
			}

			if (tokens.Length == 3)
			{
				return string.Format("{0}{1}{2}", tokens[1], _nameSeparator, tokens[2]);
			}

			throw new ArgumentException(string.Format("Invalid full name: {0}", fullName));
		}
	}
}
