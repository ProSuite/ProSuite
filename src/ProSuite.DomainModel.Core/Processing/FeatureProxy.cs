using System;

namespace ProSuite.DomainModel.Core.Processing
{
	/// <summary>
	/// Represents a feature by its OID and Class ID,
	/// but has no reference nor dependency to a real feature.
	/// </summary>
	public class FeatureProxy : IEquatable<FeatureProxy>
	{
		public int OID { get; }
		public int ClassId { get; }
		public string TableName { get; }

		public FeatureProxy(int oid, int classId, string tableName = null)
		{
			OID = oid;
			ClassId = classId;
			TableName = tableName;
		}

		public bool Equals(FeatureProxy other)
		{
			if (other is null) return false;
			if (ReferenceEquals(other, this)) return true;
			return OID == other.OID && ClassId == other.ClassId;
		}

		public override string ToString()
		{
			return $"OID = {OID}, ClassID = {ClassId}, TableName = {TableName}";
		}
	}
}
