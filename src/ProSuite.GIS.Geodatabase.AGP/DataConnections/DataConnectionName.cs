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

namespace ProSuite.GIS.Geodatabase.AGP.DataConnections
{
	/// <summary>
	/// A name implementation based on the Pro CIMStandardDataConnection, with the main purpose of
	/// comparing data sources.
	/// </summary>
	public class DataConnectionName : IDatasetName
	{
		[NotNull]
		public static DataConnectionName FromCIMDataConnection(
			[NotNull] CIMDataConnection cimDataConnection)
		{
			switch (cimDataConnection)
			{
				case CIMFeatureDatasetDataConnection cimFeatureDatasetDataConnection:
					return FromCIMDataConnection(cimFeatureDatasetDataConnection);
				case CIMStandardDataConnection cimStandardDataConnection:
					return FromCIMDataConnection(cimStandardDataConnection);
				case CIMRelQueryTableDataConnection cimRelQueryTableConnection:
					return new MemoryRelQueryTableName(cimRelQueryTableConnection);
				default:
					throw new ArgumentOutOfRangeException(nameof(cimDataConnection));
			}
		}

		public static DataConnectionName FromCIMDataConnection(
			[NotNull] CIMStandardDataConnection standardConnection)
		{
			return new DataConnectionName(standardConnection);
		}

		public static DataConnectionName FromCIMDataConnection(
			[NotNull] CIMFeatureDatasetDataConnection featureDataConnection)
		{
			return new DataConnectionName(featureDataConnection);
		}

		public DataConnectionName(CIMFeatureDatasetDataConnection featureDataConnection)
			: this(featureDataConnection.Dataset,
			       (esriDatasetType) featureDataConnection.DatasetType,
			       new DataConnectionWorkspaceName(featureDataConnection.WorkspaceConnectionString,
			                                       featureDataConnection.WorkspaceFactory)) { }

		public DataConnectionName(CIMStandardDataConnection standardConnection)
			: this(standardConnection.Dataset, ToDatasetType(standardConnection),
			       new DataConnectionWorkspaceName(standardConnection)) { }

		public DataConnectionName(string name, esriDatasetType type,
		                          DataConnectionWorkspaceName workspaceName)
		{
			NameString = name;
			Type = type;
			DataConnectionWorkspaceName = workspaceName;
		}

		public DataConnectionWorkspaceName DataConnectionWorkspaceName { get; set; }

		public string NameString { get; set; }
		public esriDatasetType Type { get; }
		public IWorkspaceName WorkspaceName => DataConnectionWorkspaceName;

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

		public string Name => NameString;

		public virtual void ChangeVersion(string newVersionName)
		{
			DataConnectionWorkspaceName.ChangeVersion(newVersionName);
		}

		// TODO: Extract base class to be used by clients, or add to IDatasetName interface
		public virtual CIMDataConnection ToCIMDataConnection()
		{
			return new CIMStandardDataConnection
			       {
				       WorkspaceConnectionString = WorkspaceName.ConnectionString,
				       WorkspaceFactory = DataConnectionWorkspaceName.FactoryType,
				       Dataset = NameString,
				       DatasetType = (ArcGIS.Core.CIM.esriDatasetType) Type
			       };
		}

		#region Overrides of Object

		public override string ToString()
		{
			return $"Name : {Name}, Type: {Type} - Datastore: {DataConnectionWorkspaceName}";
		}

		#endregion

		/// <summary>
		/// Converts a ArcGIS.Core.CIM.esriDatasetType to a ProSuite.GIS.Geodatabase.API.esriDatasetType
		/// which are not fully compatible.
		/// </summary>
		/// <param name="standardConnection"></param>
		/// <returns></returns>
		private static esriDatasetType ToDatasetType(CIMStandardDataConnection standardConnection)
		{
			switch (standardConnection.DatasetType)
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
					throw new ArgumentOutOfRangeException(nameof(standardConnection.DatasetType),
					                                      standardConnection.DatasetType,
					                                      $"Unsupported dataset type: {standardConnection.DatasetType}");
			}

			return (esriDatasetType) standardConnection.DatasetType;
		}
	}
}
