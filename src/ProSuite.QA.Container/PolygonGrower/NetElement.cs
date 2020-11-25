using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.PolygonGrower
{
	public abstract class NetElement
	{
		private readonly TableIndexRow _row;

		/// <summary>
		/// Initializes a new instance of the <see cref="NetElement"/> class.
		/// </summary>
		/// <param name="row">The row.</param>
		internal NetElement([NotNull] TableIndexRow row)
		{
			_row = row;
		}

		[NotNull]
		public TableIndexRow Row
		{
			get { return _row; }
		}

		[CLSCompliant(false)]
		public IPoint NetPoint
		{
			get { return NetPoint__.Point; }
		}

		protected abstract NetPoint_ NetPoint__ { get; }

		// NOTE: queryPoint is not necessarily updated (depends on subclass). Must use the returned point
		// NOTE: the method name is misleading, and the overall design should be cleaned up to make it less obscure (e.g., no NetPoint, NetPoint_ and NetPoint__ classes/methods)
		[CLSCompliant(false)]
		[NotNull]
		public IPoint QueryNetPoint([NotNull] IPoint queryPoint)
		{
			return QueryNetPoint(new NetPoint_(queryPoint)).Point;
		}

		[NotNull]
		protected abstract NetPoint_ QueryNetPoint([NotNull] NetPoint_ queryPoint);

		#region nested classes

		protected class NetPoint_
		{
			private readonly IPoint _point;

			[CLSCompliant(false)]
			public NetPoint_([NotNull] IPoint point)
			{
				_point = point;
			}

			[CLSCompliant(false)]
			[NotNull]
			public IPoint Point
			{
				get { return _point; }
			}
		}

		#endregion
	}
}
