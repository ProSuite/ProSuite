using ArcGIS.Core.CIM;
using ProSuite.Commons.Essentials.CodeAnnotations;
using esriDatasetType = ProSuite.GIS.Geodatabase.API.esriDatasetType;

namespace ProSuite.GIS.Geodatabase.AGP.DataConnections;

public class FeatureDatasetDataConnectionName : CIMBasedDataConnectionName
{
	public static FeatureDatasetDataConnectionName FromCIMDataConnection(
		[NotNull] CIMFeatureDatasetDataConnection featureDataConnection)
	{
		return new FeatureDatasetDataConnectionName(featureDataConnection);
	}

	public FeatureDatasetDataConnectionName(
		[NotNull] CIMFeatureDatasetDataConnection cimFeatureDatasetConnection)
		: this(cimFeatureDatasetConnection.Dataset,
		       cimFeatureDatasetConnection.FeatureDataset,
		       ToGISDatasetType(cimFeatureDatasetConnection.DatasetType),
		       new DataConnectionWorkspaceName(
			       cimFeatureDatasetConnection.WorkspaceConnectionString,
			       cimFeatureDatasetConnection.WorkspaceFactory)) { }

	public FeatureDatasetDataConnectionName([NotNull] string name,
	                                        [NotNull] string featureDatasetName,
	                                        esriDatasetType type,
	                                        [NotNull] DataConnectionWorkspaceName workspaceName)
		: base(name, type, workspaceName)
	{
		FeatureDataset = featureDatasetName;
	}

	public string FeatureDataset { get; set; }

	#region Overrides of CIMBasedDataConnectionName

	public override CIMDataConnection ToCIMDataConnection()
	{
		return new CIMFeatureDatasetDataConnection()
		       {
			       WorkspaceConnectionString = WorkspaceName.ConnectionString,
			       WorkspaceFactory = DataConnectionWorkspaceName.FactoryType,
			       Dataset = NameString,
			       DatasetType = ToCIMDatasetType(Type),
			       FeatureDataset = FeatureDataset
		       };
	}

	#endregion
}
