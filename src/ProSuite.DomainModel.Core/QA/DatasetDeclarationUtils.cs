using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA
{
	public static class DatasetDeclarationUtils
	{
		/// <summary>
		/// Resolves one transported role assignment. An unknown role name is an error rather than
		/// something to skip: silently dropping it would leave the dataset without the field it
		/// needs and fail much later.
		/// </summary>
		[NotNull]
		public static DatasetFieldRole CreateFieldRole([CanBeNull] string roleName,
		                                               [CanBeNull] string fieldName)
		{
			if (! AttributeRole.TryResolve(roleName, out AttributeRole role))
			{
				throw new InvalidConfigurationException($"Unknown attribute role '{roleName}'");
			}

			if (string.IsNullOrWhiteSpace(fieldName))
			{
				throw new InvalidConfigurationException(
					$"No field name for attribute role '{roleName}'");
			}

			return new DatasetFieldRole(Assert.NotNull(role), fieldName);
		}

		/// <summary>
		/// Asserts that a dataset parameter value carrying field roles also declares what the
		/// dataset is. The roles say which field holds the path, not what to do with it.
		/// </summary>
		public static void AssertTypeDeclared(SupportedDatasetType datasetType,
		                                      [NotNull] string datasetName,
		                                      [NotNull] string configurationName)
		{
			if (datasetType != SupportedDatasetType.Null)
			{
				return;
			}

			throw new InvalidConfigurationException(
				$"Dataset {datasetName} in {configurationName} carries field roles but no " +
				"datasetType. Field roles are only meaningful together with the kind of dataset " +
				"they describe.");
		}

		/// <summary>
		/// Adds a declaration to the declarations collected so far, keyed by dataset name. The
		/// same dataset may be declared by any number of conditions: identical declarations are
		/// fine, conflicting ones are a configuration error, because only one of them can end up
		/// on the dataset.
		/// </summary>
		public static void AddDeclaration(
			[NotNull] IDictionary<string, DatasetDeclaration> declarationsByDatasetName,
			[NotNull] DatasetDeclaration declaration)
		{
			Assert.ArgumentNotNull(declarationsByDatasetName,
			                       nameof(declarationsByDatasetName));
			Assert.ArgumentNotNull(declaration, nameof(declaration));

			if (declarationsByDatasetName.TryGetValue(declaration.DatasetName,
			                                          out DatasetDeclaration existing))
			{
				AssertSameDeclaration(existing, declaration);
				return;
			}

			declarationsByDatasetName.Add(declaration.DatasetName, declaration);
		}

		private static void AssertSameDeclaration([NotNull] DatasetDeclaration existing,
		                                          [NotNull] DatasetDeclaration other)
		{
			if (existing.DatasetType != other.DatasetType)
			{
				throw new InvalidConfigurationException(
					$"Dataset {existing.DatasetName} is referenced as both " +
					$"{existing.DatasetType} and {other.DatasetType}");
			}

			AssertSameFieldRoles(existing.DatasetName, existing.FieldRoles, other.FieldRoles);
		}

		private static void AssertSameFieldRoles(
			[NotNull] string datasetName,
			[NotNull] ICollection<DatasetFieldRole> existing,
			[NotNull] ICollection<DatasetFieldRole> other)
		{
			if (existing.Count == other.Count && ! existing.Except(other).Any())
			{
				return;
			}

			throw new InvalidConfigurationException(
				$"Dataset {datasetName} is referenced with conflicting field roles: " +
				$"[{StringUtils.Concatenate(existing, ", ")}] vs " +
				$"[{StringUtils.Concatenate(other, ", ")}]");
		}
	}
}
