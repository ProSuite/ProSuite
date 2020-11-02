using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain
{
	public abstract class WorkItem : NotifyPropertyChangedBase, IWorkItem
	{
		// todo daro: see WorkListLayer.GetProjectedDraftGeometry
		//			  Is this the right place for minimum size?
		readonly double _minimumSize = 30;

		private readonly double _extentExpansionFactor = 1.1;
		private readonly double _minimumSizeDegrees = 15;
		private readonly double _minimumSizeProjected = 0.001;
		private bool _hasZ;
		private WorkItemStatus _status;
		private bool _visited;

		private double _xmax;
		private double _xmin;
		private double _ymax;
		private double _ymin;
		private double _zmax;
		private double _zmin;

		public string Description { get; }

		#region constructors

		protected WorkItem(int id, [NotNull] Row row) : this(id, new GdbRowIdentity(row))
		{
			var feature = row as Feature;

			SetGeometryFromFeature(feature);

			Description = GetDescription(feature);
		}

		protected WorkItem(int id, GdbRowIdentity identity)
		{
			OID = id;
			Proxy = identity;
			Status = WorkItemStatus.Todo;
		}

		protected WorkItem(int id, Geometry geometry) : this(id, default(GdbRowIdentity))
		{
			SetGeometry(geometry);
		}

		#endregion

		public bool HasGeometry { get; set; }

		#region IWorkItem

		public int OID { get; set; }

		public bool Visited
		{
			get => _visited;
			set
			{
				_visited = value;
			}
		}

		public WorkItemStatus Status
		{
			get => _status;
			set
			{
				_status = value;
			}
		}

		public GdbRowIdentity Proxy { get; }

		[CanBeNull]
		public Envelope Extent { get; private set; }

		public GeometryType? GeometryType { get; set; }

		public void QueryPoints(out double xmin, out double ymin,
		                        out double xmax, out double ymax,
		                        out double zmax)
		{
			QueryPoints(out xmin, out ymin, out xmax, out ymax, out zmax, _minimumSize);
		}

		public void QueryPoints(out double xmin, out double ymin,
		                        out double xmax, out double ymax,
		                        out double zmax, double minimumSize)
		{
			// calculate offsets to meet minimum size
			double offsetX = CalculateMinimumSizeOffset(_xmin, _xmax, minimumSize);
			double offsetY = CalculateMinimumSizeOffset(_ymin, _ymax, minimumSize);

			xmin = _xmin - offsetX;
			ymin = _ymin - offsetY;

			xmax = _xmax + offsetX;
			ymax = _ymax + offsetY;

			zmax = _zmax;
		}

		#endregion

		public override string ToString()
		{
			return $"{Proxy}: {Status}, {Visited}";
		}

		[CanBeNull]
		public string GetDescription()
		{
			// todo daro: this is copied from the old world. Why not set description in constructor?
			Row row = GetRow();

			return GetDescription(row);
		}
		
		[CanBeNull]
		public string GetDescription(Row row)
		{
			return row == null
				       ? "Row not found for work item"
				       : GetDescriptionCore(row) ?? string.Empty;

		}

		protected virtual string GetDescriptionCore(Row row)
		{
			// todo daro: GetTable() might be a performance issue?
			return $"{DatasetUtils.GetTableDisplayName(row.GetTable())} ID={row.GetObjectID()}";
		}

		[CanBeNull]
		protected virtual Envelope GetExtent([CanBeNull] Feature feature)
		{
			return feature?.GetShape().Extent;
		}

		[CanBeNull]
		private Row GetRow()
		{
			return Proxy.GetRow();
		}

		public void SetGeometryFromFeature([CanBeNull] Feature feature)
		{
			Geometry geometry = feature?.GetShape();

			SetGeometry(geometry);
		}

		private void SetGeometry([CanBeNull] Geometry geometry)
		{
			Envelope extent = geometry?.Extent;
			GeometryType = geometry?.GeometryType;

			if (extent == null)
			{
				return;
			}

			SetGeometry(extent);
		}

		public void SetGeometry([CanBeNull] Envelope extent)
		{
			if (extent == null || extent.IsEmpty)
			{
				HasGeometry = false;

				_xmin = 0;
				_ymin = 0;
				_xmax = 0;
				_ymax = 0;
				_zmin = 0;
				_zmax = 0;
			}
			else
			{
				HasGeometry = true;

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
				_xmin = xmin - offset;
				_ymin = ymin - offset;
				_xmax = xmax + offset;
				_ymax = ymax + offset;

				// use the z boundary values unchanged
				if (extent.HasZ)
				{
					_hasZ = true;
					_zmin = extent.ZMin;
					_zmax = extent.ZMax;

					Extent = EnvelopeBuilder.CreateEnvelope(new Coordinate3D(_xmin, _ymin, _zmin),
					                                        new Coordinate3D(_xmax, _ymax, _zmax),
					                                        extent.SpatialReference);
				}
				else
				{
					Extent = EnvelopeBuilder.CreateEnvelope(new Coordinate2D(_xmin, _ymin),
					                                        new Coordinate2D(_xmax, _ymax),
					                                        extent.SpatialReference);
				}
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
