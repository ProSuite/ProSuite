using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry
{
	/// <summary>
	/// Implementation of <see cref="IEnumGeometry"/> used e.g. to pass to <see cref="IBufferConstruction.ConstructBuffers"/>.
	/// </summary>
	[CLSCompliant(false)]
	public class GeometryEnumerator<T> : IEnumGeometry, IDisposable where T : IGeometry
	{
		[NotNull] private readonly IEnumerable<T> _geometries;
		[NotNull] private readonly IEnumerator<T> _enumerator;

		private int? _count; // lazily determined

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="GeometryEnumerator&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="geometries">The geometries.</param>
		[CLSCompliant(false)]
		public GeometryEnumerator([NotNull] IEnumerable<T> geometries)
		{
			Assert.ArgumentNotNull(geometries, nameof(geometries));

			_geometries = geometries;
			_enumerator = _geometries.GetEnumerator();
		}

		#endregion

		[CLSCompliant(false)]
		public IGeometry Next()
		{
			if (_enumerator.MoveNext())
			{
				return _enumerator.Current;
			}

			return null;
		}

		public void Reset()
		{
			_enumerator.Reset();
		}

		public int Count
		{
			get
			{
				if (_count == null)
				{
					_count = (new List<T>(_geometries)).Count;
				}

				return _count.Value;
			}
		}

		public void Dispose()
		{
			_enumerator.Dispose();
		}
	}
}
