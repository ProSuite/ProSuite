using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain
{
	public abstract class WorkItem : NotifyPropertyChangedBase, IWorkItem
	{
		private readonly double _extentExpansionFactor = 1.1;
		private readonly double _minimumSizeDegrees = 0.001;
		private readonly double _minimumSizeProjected = 30;
			
		private WorkItemStatus _status;
		private Geometry _geometry;
		private Envelope _extent;
		private long _oid;

		private static readonly object _obj = new();

		#region constructors

		protected WorkItem(long uniqueTableId, [NotNull] Row row)
			: this(uniqueTableId, new GdbRowIdentity(row)) { }

		protected WorkItem(long uniqueTableId, GdbRowIdentity identity)
		{
			UniqueTableId = uniqueTableId;
			GdbRowProxy = identity;
			Status = WorkItemStatus.Todo;
		}

		#endregion

		public bool HasExtent => _extent != null;

		public bool HasBufferedGeometry => _geometry != null;

		#region IWorkItem

		#region thread safe

		public long OID
		{
			get
			{
				lock (_obj)
				{
					return _oid;
				}
			}
			set
			{
				lock (_obj)
				{
					_oid = value;
				}
			}
		}

		public WorkItemStatus Status
		{
			get
			{
				lock (_obj)
				{
					return _status;
				}
			}
			set
			{
				lock (_obj)
				{
					_status = value;
				}
			}
		}

		public Envelope Extent
		{
			get
			{
				lock (_obj)
				{
					return _extent;
				}
			}
			// Use SetExtent! Protected setter is only for unit testing.
			protected set
			{
				lock (_obj)
				{
					_extent = value;
				}
			}
		}

		public Geometry BufferedGeometry => _geometry;

		#endregion

		public long UniqueTableId { get; }

		public long ObjectID => GdbRowProxy.ObjectId;

		public bool Visited { get; set; }

		public GdbRowIdentity GdbRowProxy { get; }

		public GeometryType? GeometryType
		{
			get => _geometry?.GeometryType;
			set => throw new NotImplementedException();
		}

		#endregion

		public override string ToString()
		{
			return
				$"item id={OID}, row oid={ObjectID}, {GdbRowProxy.Table.Name}, {Status}, {Visited}";
		}

		[NotNull]
		public string GetDescription()
		{
			return GetDescriptionCore(GdbRowProxy) ?? string.Empty;
		}

		private string GetDescriptionCore(GdbRowIdentity row)
		{
			string tableName = row.Table.Name;
			return $"{tableName} OID={row.ObjectId} (item ID={OID})";
		}

		public void SetBufferedGeometry([NotNull] Geometry geometry)
		{
			lock (_obj)
			{
				_geometry = geometry;
			}

			SetExtent(geometry.Extent);
		}

		public void SetExtent([NotNull] Envelope extent)
		{
			double xmin;
			double ymin;
			double xmax;
			double ymax;

			double minimumSize = GetMinimumSize(extent.SpatialReference);

			// calculate the boundary values in x and y, ensuring a minimum size
			if (extent.Width < minimumSize)
			{
				double centerX = extent.XMin + extent.Width / 2;
				xmin = centerX - minimumSize / 2;
				xmax = centerX + minimumSize / 2;
			}
			else
			{
				xmin = extent.XMin;
				xmax = extent.XMax;
			}

			if (extent.Height < minimumSize)
			{
				double centerY = extent.YMin + extent.Height / 2;
				ymin = centerY - minimumSize / 2;
				ymax = centerY + minimumSize / 2;
			}
			else
			{
				ymin = extent.YMin;
				ymax = extent.YMax;
			}

			// calculate the offset to apply for a given expansion factor
			double offset = CalculateExpansionOffset(xmin, xmax, ymin, ymax,
			                                         _extentExpansionFactor);

			// apply the offset
			double xminOffset = xmin - offset;
			double yminOffset = ymin - offset;
			double xmaxOffset = xmax + offset;
			double ymaxOffset = ymax + offset;

			// use the z boundary values unchanged
			if (extent.HasZ)
			{
				double zminOffset = extent.ZMin;
				double zmaxOffset = extent.ZMax;

				Extent = EnvelopeBuilderEx.CreateEnvelope(
					new Coordinate3D(xminOffset, yminOffset, zminOffset),
					new Coordinate3D(xmaxOffset, ymaxOffset, zmaxOffset),
					extent.SpatialReference);
			}
			else
			{
				Extent = EnvelopeBuilderEx.CreateEnvelope(
					new Coordinate2D(xminOffset, yminOffset),
					new Coordinate2D(xmaxOffset, ymaxOffset),
					extent.SpatialReference);
			}
		}

		/// <summary>
		///     Gets the minimum size for the work item, for a given spatial reference.
		/// </summary>
		/// <param name="spatialReference">The spatial reference.</param>
		/// <returns></returns>
		private double GetMinimumSize([CanBeNull] SpatialReference spatialReference)
		{
			if (spatialReference == null)
			{
				return _minimumSizeDegrees;
			}

			return spatialReference.IsProjected
				       ? _minimumSizeProjected
				       : _minimumSizeDegrees;
		}

		private static double CalculateExpansionOffset(double xmin, double xmax,
		                                               double ymin, double ymax,
		                                               double expansionFactor)
		{
			double expandX = CalculateExpansionOffset(xmin, xmax, expansionFactor);
			double expandY = CalculateExpansionOffset(ymin, ymax, expansionFactor);

			return Math.Max(expandX, expandY);
		}

		/// <summary>
		/// Calculates the offset to apply to expand a range by a given ratio.
		/// </summary>
		/// <param name="min">The lower boundary of the range.</param>
		/// <param name="max">The upper boundary of the range.</param>
		/// <param name="expansionFactor">The expansion factor for the range. 
		/// A factor of 1 maintains the original range, a factor of 2 doubles the range.</param>
		/// <returns>The offset to apply to each boundary value such that the range 
		/// is expanded according to the expansion factor.</returns>
		private static double CalculateExpansionOffset(double min, double max,
		                                               double expansionFactor)
		{
			Assert.ArgumentCondition(expansionFactor > 0.01,
			                         "Expansion factor must be larger than 0.01");

			return Math.Abs(expansionFactor - 1) < double.Epsilon
				       ? 0
				       : (max - min) * (expansionFactor - 1) / 2;
		}

		// TODO: (daro) drop!
		/// <summary>
		/// Calculates the offset to apply to expand a range by a given minimum size.
		/// </summary>
		/// <param name="min">The lower boundary of the range.</param>
		/// <param name="max">The upper boundary of the range.</param>
		/// <param name="minimumSize">The minimum interval size for the range.</param>
		/// <returns>The offset to apply to each boundary value such that the range 
		/// is expanded to the minimum size if needed.</returns>
		private static double CalculateMinimumSizeOffset(double min, double max,
		                                                 double minimumSize)
		{
			Assert.ArgumentCondition(minimumSize >= 0, "minimum size must be >= 0");

			double difference = minimumSize - (max - min);

			return difference < 0
				       ? 0
				       : difference / 2;
		}
	}
}
