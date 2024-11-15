namespace ProSuite.GIS.Geodatabase.API
{
	public interface IDomain
	{
		int DomainID { get; set; }

		string Description { get; set; }

		esriFieldType FieldType { get; set; }

		esriMergePolicyType MergePolicy { get; set; }

		esriSplitPolicyType SplitPolicy { get; set; }

		string Name { get; set; }

		string Owner { get; set; }

		//esriDomainType Type { get; }

		bool MemberOf(object value);
	}

	public interface ICodedValueDomain : IDomain
	{
		int CodeCount { get; }

		string get_Name(int index);

		object get_Value(int index);
	}

	public interface IRangeDomain : IDomain
	{
		object MinValue { get; }

		object MaxValue { get; }
	}

	public enum esriMergePolicyType
	{
		esriMPTSumValues = 1,
		esriMPTAreaWeighted = 2,
		esriMPTDefaultValue = 3,
	}

	public enum esriSplitPolicyType
	{
		esriSPTGeometryRatio = 1,
		esriSPTDuplicate = 2,
		esriSPTDefaultValue = 3,
	}
}
