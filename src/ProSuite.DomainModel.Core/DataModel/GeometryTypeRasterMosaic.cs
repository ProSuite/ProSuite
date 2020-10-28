using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class GeometryTypeRasterMosaic : GeometryType
	{
		public GeometryTypeRasterMosaic() { }

		public GeometryTypeRasterMosaic([NotNull] string name) : base(name) { }
	}
}