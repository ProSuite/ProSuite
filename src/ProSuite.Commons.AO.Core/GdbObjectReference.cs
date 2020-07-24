using System;

namespace ProSuite.Commons.AO.Core
{
	/// <summary>
	/// A lightweight proxy for a Geodatabase object
	/// (a table row or a feature class row).
	/// </summary>
	public struct GdbObjectReference : IEquatable<GdbObjectReference>
	{
		public GdbObjectReference(int oid, int classID)
		{
			if (oid < 0)
				throw new ArgumentOutOfRangeException(nameof(oid), "must be non-negative");
			if (classID < 0)
				throw new ArgumentOutOfRangeException(nameof(classID), "must be non-negative");

			OID = oid;
			ClassID = classID;
		}

		public int OID { get; }
		public int ClassID { get; }

		#region IEquatable

		public bool Equals(GdbObjectReference other)
		{
			return OID == other.OID && ClassID == other.ClassID;
		}

		#endregion

		public override bool Equals(object obj)
		{
			return obj is GdbObjectReference other && Equals(other);
		}

		public override int GetHashCode()
		{
			return OID + 29 * ClassID;
		}

		public override string ToString()
		{
			return $"OID={OID} ClassID={ClassID}";
		}
	}
}
