using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	/// Helper methods for geodatabase domains
	/// </summary>
	public static class DomainUtils
	{
		[CanBeNull]
		public static IDomain GetDomain([NotNull] IFeatureWorkspace featureWorkspace,
		                                [NotNull] string domainName)
		{
			return GetDomain((IWorkspace) featureWorkspace, domainName);
		}

		[CanBeNull]
		public static IDomain GetDomain([NotNull] IWorkspace workspace,
		                                [NotNull] string domainName)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(domainName, nameof(domainName));

			var workspaceDomains = workspace as IWorkspaceDomains2;

			return workspaceDomains?.DomainByName[domainName];
		}

		[NotNull]
		public static IList<IDomain> GetDomains(
			[NotNull] IFeatureWorkspace featureWorkspace)
		{
			return GetDomains((IWorkspace) featureWorkspace);
		}

		[NotNull]
		public static IList<IDomain> GetDomains([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			var domains = new List<IDomain>();

			var workspaceDomains = workspace as IWorkspaceDomains2;
			IEnumDomain enumDomain;
			if (workspaceDomains != null && (enumDomain = workspaceDomains.Domains) != null)
			{
				enumDomain.Reset();

				IDomain domain;
				while ((domain = enumDomain.Next()) != null)
				{
					domains.Add(domain);
				}
			}

			return domains;
		}

		[NotNull]
		public static IList<IDomain> GetDomains([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var domains = new HashSet<IDomain>();

			var subtypes = objectClass as ISubtypes;

			IList<Subtype> subtypeValues = DatasetUtils.GetSubtypes(objectClass);

			foreach (IField field in DatasetUtils.GetFields(objectClass))
			{
				IDomain domain = field.Domain;

				if (domain != null)
				{
					domains.Add(domain);
				}

				if (subtypes == null)
				{
					continue;
				}

				string fieldName = field.Name;

				foreach (IDomain subtypeDomain in GetSubtypeDomains(
					         subtypes, subtypeValues, fieldName))
				{
					domains.Add(subtypeDomain);
				}
			}

			return new List<IDomain>(domains);
		}

		public static IEnumerable<IDomain> GetSubtypeDomains(
			[NotNull] ISubtypes subtypes,
			[NotNull] IEnumerable<Subtype> subtypeValues,
			[NotNull] string fieldName)
		{
			foreach (Subtype subtype in subtypeValues)
			{
				IDomain subtypeDomain = subtypes.Domain[subtype.Code, fieldName];

				if (subtypeDomain != null)
				{
					yield return subtypeDomain;
				}
			}
		}

		[NotNull]
		public static SortedDictionary<T, string> GetCodedValueMap<T>(
			[NotNull] IWorkspace workspace,
			[NotNull] string domainName)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(domainName, nameof(domainName));

			IDomain domain = GetDomain(workspace, domainName);
			Assert.NotNull(domain, "Domain not found: {0}", domainName);

			var cvDomain = domain as ICodedValueDomain;
			Assert.NotNull(cvDomain, "Not a coded value domain: {0}", domainName);

			return GetCodedValueMap<T>(cvDomain);
		}

		[NotNull]
		public static SortedDictionary<T, string> GetCodedValueMap<T>(
			[NotNull] IFeatureWorkspace featureWorkspace,
			[NotNull] string domainName)
		{
			return GetCodedValueMap<T>((IWorkspace) featureWorkspace, domainName);
		}

		[NotNull]
		public static SortedDictionary<T, string> GetCodedValueMap<T>(
			[NotNull] ICodedValueDomain domain)
		{
			Assert.ArgumentNotNull(domain, nameof(domain));

			var entries = new SortedDictionary<T, string>();

			int codeCount = domain.CodeCount;
			for (var index = 0; index < codeCount; index++)
			{
				object value = domain.Value[index];

				// without the conversion, casting an Int16 (SmallInteger field type) into an int fails:
				var typedValue = (T) Convert.ChangeType(value, typeof(T));

				string name = domain.Name[index];

				entries.Add(typedValue, name);
			}

			return entries;
		}

		/// <summary>
		/// Gets list of coded values from a coded value domain.
		/// </summary>
		/// <param name="domain">The domain.</param>
		/// <param name="sortOrder">The sort order for the coded values.</param>
		/// <returns></returns>
		[NotNull]
		public static List<CodedValue> GetCodedValueList(
			[NotNull] ICodedValueDomain domain,
			CodedValueSortOrder sortOrder = CodedValueSortOrder.Original)
		{
			Assert.ArgumentNotNull(domain, nameof(domain));

			var list = new List<CodedValue>(domain.CodeCount);

			int codeCount = domain.CodeCount;
			for (var index = 0; index < codeCount; index++)
			{
				list.Add(new CodedValue(domain.Value[index],
				                        domain.Name[index]));
			}

			switch (sortOrder)
			{
				case CodedValueSortOrder.Original:
					break;

				case CodedValueSortOrder.Value:
					list.Sort(CompareCodedValues);
					break;

				case CodedValueSortOrder.Name:
					list.Sort(CompareCodedValueNames);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(sortOrder));
			}

			return list;
		}

		/// <summary>
		/// Gets the name of a given value in a coded value domain.
		/// </summary>
		/// <param name="domain">The coded value domain.</param>
		/// <param name="value">The field value.</param>
		/// <returns>The name of the coded value, or the unchanged field value if 
		/// there is no entry for that field value in the domain.</returns>
		[CanBeNull]
		public static object GetCodedValueName([NotNull] ICodedValueDomain domain,
		                                       [CanBeNull] object value)
		{
			Assert.ArgumentNotNull(domain, nameof(domain));

			int codeCount = domain.CodeCount;
			for (var index = 0; index < codeCount; index++)
			{
				if (Equals(domain.Value[index], value))
				{
					return domain.Name[index];
				}
			}

			return value;
		}

		/// <summary>
		/// Gets the (first) value (code) of a given name in a coded value domain.
		/// </summary>
		/// <param name="domain">The coded value domain.</param>
		/// <param name="name">The name for the value.</param>
		/// <returns>The coded value of the specified name, or null if 
		/// there is no entry for that name in the domain.</returns>
		[CanBeNull]
		public static object GetCode([NotNull] ICodedValueDomain domain,
		                             [NotNull] string name)
		{
			Assert.ArgumentNotNull(domain, nameof(domain));

			int codeCount = domain.CodeCount;
			for (var index = 0; index < codeCount; index++)
			{
				if (domain.Name[index].Equals(name))
				{
					return domain.Value[index];
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the displayable text for a given merge policy.
		/// </summary>
		/// <param name="mergePolicy">The merge policy.</param>
		/// <returns></returns>
		[NotNull]
		public static string GetMergePolicyDisplayText(esriMergePolicyType mergePolicy)
		{
			switch (mergePolicy)
			{
				case esriMergePolicyType.esriMPTSumValues:
					return "Sum Values";

				case esriMergePolicyType.esriMPTAreaWeighted:
					return "Area-weighted Average";

				case esriMergePolicyType.esriMPTDefaultValue:
					return "Default Value";

				default:
					return mergePolicy.ToString();
			}
		}

		/// <summary>
		/// Gets the displayable text for a given split policy.
		/// </summary>
		/// <param name="splitPolicy">The split policy.</param>
		/// <returns></returns>
		[NotNull]
		public static string GetSplitPolicyDisplayText(esriSplitPolicyType splitPolicy)
		{
			switch (splitPolicy)
			{
				case esriSplitPolicyType.esriSPTGeometryRatio:
					return "Geometry Ratio";

				case esriSplitPolicyType.esriSPTDuplicate:
					return "Duplicate";

				case esriSplitPolicyType.esriSPTDefaultValue:
					return "Default Value";

				default:
					return splitPolicy.ToString();
			}
		}

		/// <summary>
		/// Determines whether the specified domain is owned by connected user.
		/// </summary>
		public static bool IsOwnedByConnectedUser([NotNull] IDomain domain,
		                                          [NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(domain, nameof(domain));
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			string connectedUser = WorkspaceUtils.GetConnectedUser(workspace);

			return string.Equals(domain.Owner ?? string.Empty, connectedUser,
			                     StringComparison.InvariantCultureIgnoreCase);
		}

		[NotNull]
		public static IRangeDomain CreateRangeDomain([NotNull] string name,
		                                             esriFieldType fieldType,
		                                             [NotNull] object minValue,
		                                             [NotNull] object maxValue,
		                                             [CanBeNull] string description = null)
		{
			return CreateRangeDomain(name, fieldType, minValue, maxValue,
			                         description,
			                         esriSplitPolicyType.esriSPTDuplicate,
			                         esriMergePolicyType.esriMPTDefaultValue);
		}

		[NotNull]
		public static IRangeDomain CreateRangeDomain([NotNull] string name,
		                                             esriFieldType fieldType,
		                                             [NotNull] object minValue,
		                                             [NotNull] object maxValue,
		                                             [CanBeNull] string description,
		                                             esriSplitPolicyType splitPolicy,
		                                             esriMergePolicyType mergePolicy)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentNotNull(minValue, nameof(minValue));
			Assert.ArgumentNotNull(maxValue, nameof(maxValue));

			return new RangeDomainClass
			       {
				       Name = name,
				       FieldType = fieldType,
				       Description = description,
				       SplitPolicy = splitPolicy,
				       MergePolicy = mergePolicy,
				       MinValue = minValue,
				       MaxValue = maxValue
			       };
		}

		[NotNull]
		public static ICodedValueDomain CreateCodedValueDomain(
			[NotNull] string name,
			esriFieldType fieldType,
			params CodedValue[] values)
		{
			return CreateCodedValueDomain(name, fieldType, null, values);
		}

		[NotNull]
		public static ICodedValueDomain CreateCodedValueDomain(
			[NotNull] string name,
			esriFieldType fieldType,
			[CanBeNull] string description,
			params CodedValue[] values)
		{
			return CreateCodedValueDomain(name, fieldType,
			                              description,
			                              esriSplitPolicyType.esriSPTDuplicate,
			                              esriMergePolicyType.esriMPTDefaultValue,
			                              values);
		}

		[NotNull]
		public static ICodedValueDomain CreateCodedValueDomain(
			[NotNull] string name,
			esriFieldType fieldType,
			[CanBeNull] string description,
			esriSplitPolicyType splitPolicy,
			esriMergePolicyType mergePolicy,
			params CodedValue[] values)
		{
			var result = new CodedValueDomainClass
			             {
				             Name = name,
				             FieldType = fieldType,
				             Description = description,
				             SplitPolicy = splitPolicy,
				             MergePolicy = mergePolicy
			             };

			foreach (CodedValue codedValue in values)
			{
				result.AddCode(codedValue.Value, codedValue.Name);
			}

			return result;
		}

		/// <summary>
		/// Adds a new range domain to a workspace.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <param name="domain">The domain.</param>
		/// <returns>The domain ID for the added domain.</returns>
		public static IDomain AddDomain([NotNull] IFeatureWorkspace workspace,
		                                [NotNull] IRangeDomain domain)
		{
			return AddDomain((IWorkspace) workspace, (IDomain) domain);
		}

		/// <summary>
		/// Adds a new coded value domain to a workspace.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <param name="domain">The domain.</param>
		/// <returns>The domain ID for the added domain.</returns>
		public static IDomain AddDomain([NotNull] IFeatureWorkspace workspace,
		                                [NotNull] ICodedValueDomain domain)
		{
			return AddDomain((IWorkspace) workspace, (IDomain) domain);
		}

		/// <summary>
		/// Adds a new domain to a workspace.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <param name="domain">The domain.</param>
		/// <returns>The domain ID for the added domain.</returns>
		public static IDomain AddDomain([NotNull] IFeatureWorkspace workspace,
		                                [NotNull] IDomain domain)
		{
			return AddDomain((IWorkspace) workspace, domain);
		}

		/// <summary>
		/// Adds a new domain to a workspace.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <param name="domain">The domain.</param>
		/// <returns>The domain ID for the added domain.</returns>
		public static IDomain AddDomain([NotNull] IWorkspace workspace,
		                                [NotNull] IDomain domain)
		{
			Assert.ArgumentNotNull(domain, nameof(domain));
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			var workspaceDomains = (IWorkspaceDomains2) workspace;

			workspaceDomains.AddDomain(domain);

			return domain;
		}

		#region Non-public

		private static int CompareCodedValues(CodedValue x, CodedValue y)
		{
			if (x == null)
			{
				return y == null
					       ? 0 // If x is null and y is null, they're equal. 
					       : -1; // If x is null and y is not null, y is greater. 
			}

			// If x is not null and and y is not null, compare the coded values by their value
			return y == null
				       ? 1 // If x is not null and y is null, x is greater.
				       : CollectionUtils.CompareAscending(x.Value, y.Value);
		}

		private static int CompareCodedValueNames(CodedValue x, CodedValue y)
		{
			if (x == null)
			{
				return y == null
					       ? 0 // If x is null and y is null, they're equal. 
					       : -1; // If x is null and y is not null, y is greater.
			}

			// If x is not null and and y is not null, compare the coded values by their name
			return y == null
				       ? 1 // If x is not null and y is null, x is greater.
				       : CollectionUtils.CompareAscending(x.Name, y.Name);
		}

		#endregion
	}
}
