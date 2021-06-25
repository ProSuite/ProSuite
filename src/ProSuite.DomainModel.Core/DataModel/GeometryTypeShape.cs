using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.EsriShape;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class GeometryTypeShape : GeometryType
	{
		[UsedImplicitly] private readonly ProSuiteGeometryType _shapeType;

		/// <summary>
		/// Initializes a new instance of the <see cref="GeometryTypeShape"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		[UsedImplicitly]
		protected GeometryTypeShape() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="GeometryTypeShape"/> class.
		/// </summary>
		/// <param name="name">The name for the geometry type.</param>
		/// <param name="geometryType">The corresponding esri geometry type.</param>
		public GeometryTypeShape([NotNull] string name, ProSuiteGeometryType geometryType)
			: base(name)
		{
			_shapeType = geometryType;
		}

		public ProSuiteGeometryType ShapeType => _shapeType;

		protected override GeometryType CreateClone()
		{
			return new GeometryTypeShape(Name, _shapeType);
		}
	}
}
