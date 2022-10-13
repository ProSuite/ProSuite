using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class GeometryTypeNoGeometry : GeometryType
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GeometryTypeNoGeometry"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected GeometryTypeNoGeometry() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="GeometryTypeNoGeometry"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		public GeometryTypeNoGeometry([NotNull] string name) : base(name) { }
	}
}
