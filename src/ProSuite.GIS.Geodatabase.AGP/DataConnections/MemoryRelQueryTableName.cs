using ArcGIS.Core.CIM;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.GIS.Geodatabase.API;
using esriDatasetType = ProSuite.GIS.Geodatabase.API.esriDatasetType;
using esriRelCardinality = ProSuite.GIS.Geodatabase.API.esriRelCardinality;

namespace ProSuite.GIS.Geodatabase.AGP.DataConnections;

/// <summary>
/// The name implementation for a layer join (rel query table) in memory,
/// representing a CIMRelQueryTableDataConnection in Pro.
/// </summary>
public class MemoryRelQueryTableName : CIMBasedDataConnectionName, IMemoryRelQueryTableName
{
	private readonly CIMBasedDataConnectionName _sourceConnectionName;
	private readonly CIMBasedDataConnectionName _destinationConnectionName;
	private readonly esriJoinType _joinType;

	public MemoryRelQueryTableName(CIMRelQueryTableDataConnection relQueryConnection)
		: this(relQueryConnection.Name,
		       FromCIMDataConnection(relQueryConnection.SourceTable),
		       FromCIMDataConnection(relQueryConnection.DestinationTable),
		       relQueryConnection.PrimaryKey,
		       relQueryConnection.ForeignKey,
		       (esriRelCardinality) relQueryConnection.Cardinality,
		       relQueryConnection.JoinType,
		       relQueryConnection.JoinForward) { }

	public MemoryRelQueryTableName(
		[NotNull] string name,
		[NotNull] CIMBasedDataConnectionName sourceConnectionName,
		[NotNull] CIMBasedDataConnectionName destinationConnectionName,
		[NotNull] string primaryKey,
		[NotNull] string foreignKey,
		esriRelCardinality cardinality,
		esriJoinType joinType,
		bool forwardDirection)
		: base(name, esriDatasetType.esriDTRelationshipClass,
		       sourceConnectionName.DataConnectionWorkspaceName)
	{
		// TODO: Proper enum translation -> ProSuite JoinCardinality, etc The enums don't match between AO and Pro!

		ForwardDirection = forwardDirection;
		Cardinality = cardinality;

		_sourceConnectionName = sourceConnectionName;
		_destinationConnectionName = destinationConnectionName;

		PrimaryKey = primaryKey;
		ForeignKey = foreignKey;

		_joinType = joinType;
	}

	#region Implementation of IMemoryRelQueryTableName

	public bool ForwardDirection { get; }
	public esriRelCardinality Cardinality { get; }

	public IDatasetName SourceTable => _sourceConnectionName;
	public IDatasetName DestinationTable => _destinationConnectionName;

	public string PrimaryKey { get; }
	public string ForeignKey { get; }

	#endregion

	public override void ReplaceWorkspaceName(
		[NotNull] DataConnectionWorkspaceName newWorkspaceName)
	{
		base.ReplaceWorkspaceName(newWorkspaceName);

		_sourceConnectionName.ReplaceWorkspaceName(newWorkspaceName);
		_destinationConnectionName.ReplaceWorkspaceName(newWorkspaceName);
	}

	public override void ChangeVersion([NotNull] string newVersionName)
	{
		_sourceConnectionName.ChangeVersion(newVersionName);
		_destinationConnectionName.ChangeVersion(newVersionName);
	}

	public override CIMDataConnection ToCIMDataConnection()
	{
		var result = new CIMRelQueryTableDataConnection
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
