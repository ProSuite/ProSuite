using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class GeometryTypeTopology : GeometryType
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GeometryTypeTopology"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected GeometryTypeTopology() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="GeometryTypeTopology"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		public GeometryTypeTopology([NotNull] string name) : base(name) { }
	}
}
