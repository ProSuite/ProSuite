using System;
using ArcGIS.Core.CIM;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.CIM
{
	public class CimFeatureWorkspaceReference : IEquatable<CimFeatureWorkspaceReference>
	{
		[NotNull] private readonly string _connectionString;
		[CanBeNull] private readonly string _featureDataset;
		private readonly WorkspaceFactory _workspaceFactory;

		public CimFeatureWorkspaceReference(CIMFeatureDatasetDataConnection connection)
		{
			_workspaceFactory = connection.WorkspaceFactory;
			_connectionString = connection.WorkspaceConnectionString;
			_featureDataset = connection.FeatureDataset;
		}

		#region IEquatable<CimFeatureWorkspaceReference> implementation

		public bool Equals(CimFeatureWorkspaceReference other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return string.Equals(_connectionString, other._connectionString) &&
			       string.Equals(_featureDataset, other._featureDataset) &&
			       _workspaceFactory == other._workspaceFactory;
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

			return Equals((CimFeatureWorkspaceReference) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = _connectionString.GetHashCode();
				hashCode = (hashCode * 397) ^
				           (_featureDataset != null ? _featureDataset.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (int) _workspaceFactory;
				return hashCode;
			}
		}

		#endregion
	}
}
