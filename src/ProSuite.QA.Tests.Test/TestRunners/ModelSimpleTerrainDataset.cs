using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.QA.Tests.Test.TestRunners
{
	internal class ModelSimpleTerrainDataset : SimpleTerrainDataset
	{
		public ModelSimpleTerrainDataset(string terrainName, string terrainDs)
			: base(terrainName, terrainDs)
		{
			GeometryType = new GeometryTypeTerrain("Terrain");
		}
		public ModelSimpleTerrainDataset(string xmlDefinition)
				:this(Create(xmlDefinition))
		{ }

		private ModelSimpleTerrainDataset(XmlSimpleTerrainDataset xml)
			: base(
				xml.Sources.Select(
					x => (ITerrainSoure)
						new TerrainSource
						{
							Dataset = new ModelVectorDataset(x.Dataset),
							Type = x.Type
						}).ToList()
			)
		{
			GeometryType = new GeometryTypeTerrain("Terrain");
		}

		private static XmlSimpleTerrainDataset Create(string xmlDefinition)
		{
			var helper = new XmlSerializationHelper<XmlSimpleTerrainDataset>();
			XmlSimpleTerrainDataset xml = helper.ReadFromString(xmlDefinition, null);
			return xml; }

	}

	internal class TerrainSource : ITerrainSoure
	{
		public ModelVectorDataset Dataset { get; set; }
		IVectorDataset ITerrainSoure.Dataset => Dataset;
		public esriTinSurfaceType Type { get; set; }
	}
	public class XmlSimpleTerrainDataset
	{
		public List<XmlTerrainSource> Sources { get; set; }
	}

	public class XmlTerrainSource
	{
		public string Dataset { get; set; }
		public esriTinSurfaceType Type { get; set; }
	}
}
