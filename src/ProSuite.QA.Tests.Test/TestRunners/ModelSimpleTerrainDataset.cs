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
		// Open questions for later:
		// nh-mapping in the same table as all the other datasets?
		// or create a new top-level entity (DatasetCollection?) that could also contain linear networks?
		// -> they are different from 'normal' datasets because they do not exist as Gdb objects.
		// -> Add to dataset lookup?
		// If it's a separate entity, QualityVerificationDataset must be changed (and probably other entities
		// referencing them?)
		// Probably a special type of CollectionDataset or non-gdb dataset could be created that can be easily distinguished
		// from normal datasets. They could probably be nh-mapped using <any> to the respective LinearNetwork or SimpleTerrain.
		// Or mix table-per-hierarchy with table-per-class mapping.
		// Another difference is that these datasets are not harvested but created in the DDX.
		// Completely different approach: So far it's just relevant for the QualityVerificationDataset. Use just the dataset name instead?
		private static readonly string _geometryTypeName = "SimpleTerrain";

		public ModelSimpleTerrainDataset(string terrainName, string terrainDs)
			: base(terrainName, terrainDs)
		{
			GeometryType = new GeometryTypeTerrain(_geometryTypeName);
		}

		public ModelSimpleTerrainDataset(string xmlDefinition)
			: this(Create(xmlDefinition)) { }

		private ModelSimpleTerrainDataset(XmlSimpleTerrainDataset xml)
			: base(
				xml.Sources.Select(
					   x =>
						   new TerrainSourceDataset(new ModelVectorDataset(x.Dataset), x.Type))
				   .ToList()
			)
		{
			Name = xml.Name;
			PointDensity = xml.PointDensity;
			GeometryType = new GeometryTypeTerrain(_geometryTypeName);
		}

		private static XmlSimpleTerrainDataset Create(string xmlDefinition)
		{
			var helper = new XmlSerializationHelper<XmlSimpleTerrainDataset>();
			XmlSimpleTerrainDataset xml = helper.ReadFromString(xmlDefinition, null);
			return xml;
		}
	}

	internal class TerrainSource : ITerrainSoure
	{
		public ModelVectorDataset Dataset { get; set; }
		IVectorDataset ITerrainSoure.Dataset => Dataset;
		public esriTinSurfaceType Type { get; set; }
	}

	public class XmlSimpleTerrainDataset
	{
		public string Name { get; set; }
		public double PointDensity { get; set; }
		public List<XmlTerrainSource> Sources { get; set; }
	}

	public class XmlTerrainSource
	{
		public string Dataset { get; set; }
		public TinSurfaceType Type { get; set; }
	}
}
