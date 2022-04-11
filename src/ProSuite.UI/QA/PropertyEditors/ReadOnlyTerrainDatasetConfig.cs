using System.ComponentModel;
using System.Data;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class ReadOnlyTerrainDatasetConfig : ReadOnlyDatasetConfig
	{
		protected override TestParameterType TestParameterTypes
		{
			get { return TestParameterType.TerrainDataset; }
		}

		[DisplayName("Terrain Dataset")]
		[ReadOnly(true)]
		public override Dataset Data
		{
			get { return base.Data; }
			set { throw new ReadOnlyException(); }
		}
	}
}