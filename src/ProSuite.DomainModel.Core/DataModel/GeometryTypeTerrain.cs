using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class GeometryTypeTerrain : GeometryType
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GeometryTypeTerrain"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected GeometryTypeTerrain() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="GeometryTypeTerrain"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		public GeometryTypeTerrain([NotNull] string name) : base(name) { }
	}
}