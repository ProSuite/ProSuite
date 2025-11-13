using System;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Analyst3D;
using ArcGIS.Core.Data.Raster;
using ArcGIS.Core.Data.Topology;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.GIS.Geodatabase.API;
using esriDatasetType = ProSuite.GIS.Geodatabase.API.esriDatasetType;

namespace ProSuite.GIS.Geodatabase.AGP.DataConnections;

/// <summary>
/// A name hierarchy based on the Pro CIMDataConnection, with the main purpose of
/// comparing and manipulating data sources.
/// </summary>
public abstract class CIMBasedDataConnectionName : IDatasetName
{
	[NotNull]
	public static CIMBasedDataConnectionName FromCIMDataConnection(
		[NotNull] CIMDataConnection cimDataConnection)
	{
		switch (cimDataConnection)
		{
			case CIMFeatureDatasetDataConnection cimFeatureDatasetDataConnection:
				return FeatureDatasetDataConnectionName.FromCIMDataConnection(
					cimFeatureDatasetDataConnection);
			case CIMStandardDataConnection cimStandardDataConnection:
				return StandardDataConnectionName.FromCIMDataConnection(cimStandardDataConnection);
			case CIMRelQueryTableDataConnection cimRelQueryTableConnection:
				return new MemoryRelQueryTableName(cimRelQueryTableConnection);
			default:
				throw new ArgumentOutOfRangeException(nameof(cimDataConnection));
		}
	}

	protected CIMBasedDataConnectionName([NotNull] string name,
	                                     esriDatasetType type,
	                                     [NotNull] DataConnectionWorkspaceName workspaceName)
	{
		NameString = name;
		Type = type;
		DataConnectionWorkspaceName = workspaceName;
	}

	public DataConnectionWorkspaceName DataConnectionWorkspaceName { get; private set; }

	public string NameString { get; set; }

	public esriDatasetType Type { get; set; }

	public IWorkspaceName WorkspaceName => DataConnectionWorkspaceName;

	public string Name => NameString;

	public object Open()
	{
		switch (Type)
		{
			case esriDatasetType.esriDTFeatureClass:
			case esriDatasetType.esriDTTable:
				return ArcGeodatabaseUtils.ToArcTable((Table) OpenDataset());
			case esriDatasetType.esriDTTopology:
			default:
				throw new ArgumentOutOfRangeException(
					$"Unsupported dataset type for IName.Open(): {Type}");
		}
	}

	public abstract CIMDataConnection ToCIMDataConnection();

	public Dataset OpenDataset()
	{
		Datastore datastore = DataConnectionWorkspaceName.OpenDatastore();

		try
		{
			switch (Type)
			{
				case esriDatasetType.esriDTFeatureClass:
					return DatasetUtils.OpenDataset<FeatureClass>(datastore, NameString);
				case esriDatasetType.esriDTTable:
					return DatasetUtils.OpenDataset<Table>(datastore, NameString);
				case esriDatasetType.esriDTTopology:
					return DatasetUtils.OpenDataset<Topology>(datastore, NameString);
				case esriDatasetType.esriDTRelationshipClass:
					return DatasetUtils.OpenDataset<RelationshipClass>(datastore, NameString);
				case esriDatasetType.esriDTRasterDataset:
					return DatasetUtils.OpenDataset<RasterDataset>(datastore, NameString);
				case esriDatasetType.esriDTTerrain:
					return DatasetUtils.OpenDataset<Terrain>(datastore, NameString);
				case esriDatasetType.esriDTMosaicDataset:
					return DatasetUtils.OpenDataset<MosaicDataset>(datastore, NameString);
				case esriDatasetType.esriDTLasDataset:
					return DatasetUtils.OpenDataset<LasDataset>(datastore, NameString);
				case esriDatasetType.esriDTTin:
					return DatasetUtils.OpenDataset<TinDataset>(datastore, NameString);
				default:
					throw new ArgumentOutOfRangeException($"Unsupported dataset type: {Type}");
			}
		}
		finally
		{
			datastore?.Dispose();
		}
	}

	public virtual void ReplaceWorkspaceName(DataConnectionWorkspaceName newWorkspaceName)
	{
		DataConnectionWorkspaceName = newWorkspaceName;
	}

	public virtual void ChangeVersion(string newVersionName)
	{
		DataConnectionWorkspaceName.ChangeVersion(newVersionName);
	}

	/// <summary>
	/// Converts a ArcGIS.Core.CIM.esriDatasetType to a ProSuite.GIS.Geodatabase.API.esriDatasetType
	/// which are not fully compatible.
	/// </summary>
	/// <param name="cimDatasetType"></param>
	/// <returns></returns>
	protected static esriDatasetType ToGISDatasetType(
		ArcGIS.Core.CIM.esriDatasetType cimDatasetType)
	{
		switch (cimDatasetType)
		{
			case ArcGIS.Core.CIM.esriDatasetType.esriDTFeatureClass:
				return esriDatasetType.esriDTFeatureClass;
			case ArcGIS.Core.CIM.esriDatasetType.esriDTTable:
				return esriDatasetType.esriDTTable;
			case ArcGIS.Core.CIM.esriDatasetType.esriDTRelationshipClass:
				return esriDatasetType.esriDTRelationshipClass;
			case ArcGIS.Core.CIM.esriDatasetType.esriDTRasterDataset:
				return esriDatasetType.esriDTRasterDataset;
			case ArcGIS.Core.CIM.esriDatasetType.esriDTRasterBand:
				return esriDatasetType.esriDTRasterBand;
			case ArcGIS.Core.CIM.esriDatasetType.esriDTTin:
				return esriDatasetType.esriDTTin;
			case ArcGIS.Core.CIM.esriDatasetType.esriDTRasterCatalog:
				return esriDatasetType.esriDTRasterCatalog;
			case ArcGIS.Core.CIM.esriDatasetType.esriDTTopology:
				return esriDatasetType.esriDTTopology;
			case ArcGIS.Core.CIM.esriDatasetType.esriDTNetworkDataset:
				return esriDatasetType.esriDTNetworkDataset;
			case ArcGIS.Core.CIM.esriDatasetType.esriDTTerrain:
				return esriDatasetType.esriDTTerrain;
			case ArcGIS.Core.CIM.esriDatasetType.esriDTRepresentationClass:
				return esriDatasetType.esriDTRepresentationClass;
			case ArcGIS.Core.CIM.esriDatasetType.esriDTCadastralFabric:
				return esriDatasetType.esriDTCadastralFabric;
			case ArcGIS.Core.CIM.esriDatasetType.esriDTSchematicDataset:
				return esriDatasetType.esriDTSchematicDataset;
			case ArcGIS.Core.CIM.esriDatasetType.esriDTMosaicDataset:
				return esriDatasetType.esriDTMosaicDataset;
			case ArcGIS.Core.CIM.esriDatasetType.esriDTLasDataset:
				return esriDatasetType.esriDTLasDataset;

			default:
				throw new ArgumentOutOfRangeException(
					nameof(cimDatasetType), cimDatasetType,
					$"Unsupported dataset type: {cimDatasetType}");
		}
	}

	/// <summary>
	/// Converts a ProSuite.GIS.Geodatabase.API.esriDatasetType to a ArcGIS.Core.CIM.esriDatasetType
	/// which are not fully compatible.
	/// </summary>
	/// <param name="gisDatasetType"></param>
	/// <returns></returns>
	protected static ArcGIS.Core.CIM.esriDatasetType ToCIMDatasetType(
		esriDatasetType gisDatasetType)
	{
		switch (gisDatasetType)
		{
			case esriDatasetType.esriDTFeatureClass:
				return ArcGIS.Core.CIM.esriDatasetType.esriDTFeatureClass;
			case esriDatasetType.esriDTTable:
				return ArcGIS.Core.CIM.esriDatasetType.esriDTTable;
			case esriDatasetType.esriDTRelationshipClass:
				return ArcGIS.Core.CIM.esriDatasetType.esriDTRelationshipClass;
			case esriDatasetType.esriDTRasterDataset:
				return ArcGIS.Core.CIM.esriDatasetType.esriDTRasterDataset;
			case esriDatasetType.esriDTRasterBand:
				return ArcGIS.Core.CIM.esriDatasetType.esriDTRasterBand;
			case esriDatasetType.esriDTTin:
				return ArcGIS.Core.CIM.esriDatasetType.esriDTTin;
			case esriDatasetType.esriDTRasterCatalog:
				return ArcGIS.Core.CIM.esriDatasetType.esriDTRasterCatalog;
			case esriDatasetType.esriDTTopology:
				return ArcGIS.Core.CIM.esriDatasetType.esriDTTopology;
			case esriDatasetType.esriDTNetworkDataset:
				return ArcGIS.Core.CIM.esriDatasetType.esriDTNetworkDataset;
			case esriDatasetType.esriDTTerrain:
				return ArcGIS.Core.CIM.esriDatasetType.esriDTTerrain;
			case esriDatasetType.esriDTRepresentationClass:
				return ArcGIS.Core.CIM.esriDatasetType.esriDTRepresentationClass;
			case esriDatasetType.esriDTCadastralFabric:
				return ArcGIS.Core.CIM.esriDatasetType.esriDTCadastralFabric;
			case esriDatasetType.esriDTSchematicDataset:
				return ArcGIS.Core.CIM.esriDatasetType.esriDTSchematicDataset;
			case esriDatasetType.esriDTMosaicDataset:
				return ArcGIS.Core.CIM.esriDatasetType.esriDTMosaicDataset;
			case esriDatasetType.esriDTLasDataset:
				return ArcGIS.Core.CIM.esriDatasetType.esriDTLasDataset;

			default:
				throw new ArgumentOutOfRangeException(
					nameof(gisDatasetType), gisDatasetType,
					$"Unsupported dataset type: {gisDatasetType}");
		}
	}
}
