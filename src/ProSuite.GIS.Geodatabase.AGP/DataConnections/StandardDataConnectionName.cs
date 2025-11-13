using ArcGIS.Core.CIM;
using ProSuite.Commons.Essentials.CodeAnnotations;
using esriDatasetType = ProSuite.GIS.Geodatabase.API.esriDatasetType;

namespace ProSuite.GIS.Geodatabase.AGP.DataConnections;

/// <summary>
/// A name implementation based on the Pro CIMStandardDataConnection, with the main purpose of
/// comparing data sources.
/// </summary>
public class StandardDataConnectionName : CIMBasedDataConnectionName
{
	public static StandardDataConnectionName FromCIMDataConnection(
		[NotNull] CIMStandardDataConnection standardConnection)
	{
		return new StandardDataConnectionName(standardConnection);
	}

	public StandardDataConnectionName(CIMFeatureDatasetDataConnection featureDataConnection)
		: this(featureDataConnection.Dataset,
		       (esriDatasetType) featureDataConnection.DatasetType,
		       new DataConnectionWorkspaceName(featureDataConnection.WorkspaceConnectionString,
		                                       featureDataConnection.WorkspaceFactory)) { }

	public StandardDataConnectionName(CIMStandardDataConnection standardConnection)
		: this(standardConnection.Dataset, ToGISDatasetType(standardConnection.DatasetType),
		       new DataConnectionWorkspaceName(standardConnection)) { }

	public StandardDataConnectionName(string name, esriDatasetType type,
	                                  DataConnectionWorkspaceName workspaceName)
		: base(name, type, workspaceName) { }

	public override CIMDataConnection ToCIMDataConnection()
	{
		return new CIMStandardDataConnection
		       {
			       WorkspaceConnectionString = WorkspaceName.ConnectionString,
			       WorkspaceFactory = DataConnectionWorkspaceName.FactoryType,
			       Dataset = NameString,
			       DatasetType = ToCIMDatasetType(Type)
		       };
	}

	#region Overrides of Object

	public override string ToString()
	{
		return $"Name : {Name}, Type: {Type} - Datastore: {DataConnectionWorkspaceName}";
	}

	#endregion
}
