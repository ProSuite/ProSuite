using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class GeometryTypeGeometricNetwork : GeometryType
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GeometryTypeGeometricNetwork"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected GeometryTypeGeometricNetwork() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="GeometryTypeGeometricNetwork"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		public GeometryTypeGeometricNetwork([NotNull] string name) : base(name) { }
	}
}
