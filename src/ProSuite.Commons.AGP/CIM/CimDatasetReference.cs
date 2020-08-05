using System;

namespace ProSuite.Commons.AGP.CIM
{
	public class CimDatasetReference : IEquatable<CimDatasetReference>
	{
		public CimDatasetReference(CimFeatureWorkspaceReference workspaceReference,
		                           string datasetName)
		{
			WorkspaceReference = workspaceReference;
			DatasetName = datasetName;
		}

		public CimFeatureWorkspaceReference WorkspaceReference { get; }
		public string DatasetName { get; }

		#region IEquatable<CimDatasetReference> implementation

		public bool Equals(CimDatasetReference other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(WorkspaceReference, other.WorkspaceReference) &&
			       string.Equals(DatasetName, other.DatasetName);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((CimDatasetReference) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((WorkspaceReference != null ? WorkspaceReference.GetHashCode() : 0) * 397) ^
				       (DatasetName != null ? DatasetName.GetHashCode() : 0);
			}
		}

		#endregion
	}
}
