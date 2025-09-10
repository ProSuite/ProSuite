using ArcGIS.Core.CIM;
using ProSuite.GIS.Geodatabase.API;
using esriDatasetType = ProSuite.GIS.Geodatabase.API.esriDatasetType;
using esriRelCardinality = ProSuite.GIS.Geodatabase.API.esriRelCardinality;

namespace ProSuite.GIS.Geodatabase.AGP.DataConnections;

/// <summary>
/// The name implementation for a layer join (rel query table) in memory,
/// representing a CIMRelQueryTableDataConnection in Pro.
/// </summary>
public class MemoryRelQueryTableName : DataConnectionName, IMemoryRelQueryTableName
{
	// TODO: abstract base rather than deriving from DataConnectionName.

	private readonly DataConnectionName _sourceConnectionName;
	private readonly DataConnectionName _destinationConnectionName;
	private readonly esriJoinType _joinType;

	public MemoryRelQueryTableName(CIMRelQueryTableDataConnection relQueryConnection)
		: base(relQueryConnection.Name, esriDatasetType.esriDTRelationshipClass,
		       DataConnectionWorkspaceName.FromDataConnection(relQueryConnection.SourceTable))
	{
		// TODO: Proper enum translation -> ProSuite JoinCardinality, etc The enums don't match between AO and Pro!
		ForwardDirection = relQueryConnection.JoinForward;
		Cardinality = (esriRelCardinality) relQueryConnection.Cardinality;

		_sourceConnectionName = FromCIMDataConnection(relQueryConnection.SourceTable);
		_destinationConnectionName = FromCIMDataConnection(relQueryConnection.DestinationTable);

		PrimaryKey = relQueryConnection.PrimaryKey;
		ForeignKey = relQueryConnection.ForeignKey;

		_joinType = relQueryConnection.JoinType;
	}

	public MemoryRelQueryTableName(string name,
	                               esriDatasetType type,
	                               DataConnectionWorkspaceName workspaceName)
		: base(name, type, workspaceName)
	{
		// TODO: Proper constructor
	}

	#region Implementation of IMemoryRelQueryTableName

	public bool ForwardDirection { get; }
	public esriRelCardinality Cardinality { get; }

	public IDatasetName SourceTable => _sourceConnectionName;
	public IDatasetName DestinationTable => _destinationConnectionName;

	public string PrimaryKey { get; }
	public string ForeignKey { get; }

	#endregion

	public override void ChangeVersion(string newVersionName)
	{
		_sourceConnectionName.ChangeVersion(newVersionName);
		_destinationConnectionName.ChangeVersion(newVersionName);
	}

	public override CIMDataConnection ToCIMDataConnection()
	{
		var result = new CIMRelQueryTableDataConnection()
		             {
			             Name = Name,
			             Cardinality = (ArcGIS.Core.CIM.esriRelCardinality) Cardinality,
			             JoinType = _joinType,
			             JoinForward = ForwardDirection,
			             // TODO: Deal with one-to-first once properly implemented
			             //OneToFirst = false,
			             SourceTable = _sourceConnectionName.ToCIMDataConnection(),
			             DestinationTable = _destinationConnectionName.ToCIMDataConnection(),
			             PrimaryKey = PrimaryKey,
			             ForeignKey = ForeignKey
		             };

		return result;
	}

	public override string ToString()
	{
		return $"Name : {Name}, Type: {Type} - Datastore: {DataConnectionWorkspaceName}";
	}
}
