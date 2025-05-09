using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Xml;

namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// A simple terrain dataset that can be added to an extended model. They are not defined
	/// in the Gdb but are defined in the data dictionary or in XML.
	/// </summary>
	public class ModelSimpleTerrainDataset : SimpleTerrainDataset
	{
		public static bool CanCreateDataset(string name)
		{
			XmlSerializationHelper<XmlSimpleTerrainDataset> terrainHelper =
				new XmlSerializationHelper<XmlSimpleTerrainDataset>();

			return terrainHelper.CanDeserializeString(name);
		}

		public static ModelSimpleTerrainDataset Create(string name,
		                                               DdxModel model,
		                                               Func<string, Dataset> getDataset)
		{
			var helper = new XmlSerializationHelper<XmlSimpleTerrainDataset>();

			XmlSimpleTerrainDataset xmlDefinition = helper.ReadFromString(name, null);

			var result = Create(
				xmlDefinition,
				dsName => getDataset(dsName) as VectorDataset);

			result.Model = model;

			return result;
		}

		private static ModelSimpleTerrainDataset Create(
			[NotNull] XmlSimpleTerrainDataset xml,
			[NotNull] Func<string, VectorDataset> getDataset)
		{
			List<TerrainSourceDataset> datasets = new List<TerrainSourceDataset>();

			foreach (XmlTerrainSource source in xml.Sources)
			{
				VectorDataset sourceDataset = getDataset(source.Dataset);

				if (sourceDataset == null)
				{
					throw new InvalidOperationException(
						$"Terrain source dataset named {source.Dataset} not found");
				}

				datasets.Add(new TerrainSourceDataset(sourceDataset, source.Type));
			}

			return new ModelSimpleTerrainDataset(xml.Name, datasets)
			       {
				       Name = xml.Name,
				       PointDensity = xml.PointDensity
			       };
		}

		public ModelSimpleTerrainDataset() { }

		public ModelSimpleTerrainDataset(
			[NotNull] string name,
			[NotNull] IEnumerable<TerrainSourceDataset> sources)
			: base(name, sources) { }

		public override DatasetImplementationType ImplementationType =>
			DatasetImplementationType.ModelSimpleTerrain;
	}

	// Open questions for later:
	// nh-mapping in the same table as all the other datasets? -> Yes
	// -> they are different from 'normal' datasets because they do not exist as Gdb objects.
	// -> Add to dataset lookup?
	// If it was a separate entity, QualityVerificationDataset would need to be changed changed (and probably other entities
	// referencing them?)
	// Probably a special type of CollectionDataset or non-gdb dataset could be created that can be easily distinguished
	// from normal datasets. They could probably be nh-mapped using <any> to the respective LinearNetwork or SimpleTerrain.
	// Or mix table-per-hierarchy with table-per-class mapping.
	// Another difference is that these datasets are not harvested but created in the DDX.

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
