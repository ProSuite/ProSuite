using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Xml;

namespace ProSuite.DomainModel.Core.DataModel
{
	public static class SimpleTerrainDatasetUtils
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

		public static SimpleTerrainDataset Create(
			[NotNull] string xmlDefinition,
			[NotNull] Func<string, VectorDataset> getDataset)
		{
			return Create(Create(xmlDefinition), getDataset);
		}

		private static SimpleTerrainDataset Create(
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

			return new XmlBasedSimpleTerrainDataset(xml.Name, xml.PointDensity, datasets);
		}

		private static XmlSimpleTerrainDataset Create(string xmlDefinition)
		{
			var helper = new XmlSerializationHelper<XmlSimpleTerrainDataset>();
			XmlSimpleTerrainDataset xml = helper.ReadFromString(xmlDefinition, null);
			return xml;
		}

		private class XmlBasedSimpleTerrainDataset : SimpleTerrainDataset
		{
			public XmlBasedSimpleTerrainDataset(string name, double pointDensity,
			                                    [NotNull]
			                                    List<TerrainSourceDataset> terrainSourceDatasets)
				: base(terrainSourceDatasets)
			{
				Name = name;
				PointDensity = pointDensity;
			}
		}
	}
}
