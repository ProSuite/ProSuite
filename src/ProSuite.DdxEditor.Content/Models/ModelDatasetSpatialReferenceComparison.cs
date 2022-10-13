using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
	public class ModelDatasetSpatialReferenceComparison
	{
		private readonly Dataset _dataset;
		private readonly SpatialReferenceProperties _spatialReferenceProperties;
		private bool? _xyPrecisionDifferent;
		private bool? _zPrecisionDifferent;
		private bool? _isMPrecisionEqual;
		private bool? _coordinateSystemDifferent;
		private bool? _verticalCoordinateSystemDifferent;
		private readonly List<string> _issues = new List<string>();
		private bool? _xyToleranceDifferent;
		private bool? _zToleranceDifferent;
		private bool? _mToleranceDifferent;

		public ModelDatasetSpatialReferenceComparison(
			[NotNull] Dataset dataset,
			[CanBeNull] SpatialReferenceProperties spatialReferenceProperties)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			_dataset = dataset;
			_spatialReferenceProperties = spatialReferenceProperties;
		}

		[NotNull]
		public Dataset Dataset => _dataset;

		[CanBeNull]
		public SpatialReferenceProperties SpatialReferenceProperties => _spatialReferenceProperties;

		public void AddIssue([NotNull] string issue)
		{
			Assert.ArgumentNotNullOrEmpty(issue, nameof(issue));

			_issues.Add(issue);
		}

		[NotNull]
		public ICollection<string> Issues => _issues;

		public bool? XyPrecisionDifferent
		{
			get { return _xyPrecisionDifferent; }
			internal set { _xyPrecisionDifferent = value; }
		}

		public bool? ZPrecisionDifferent
		{
			get { return _zPrecisionDifferent; }
			internal set { _zPrecisionDifferent = value; }
		}

		public bool? IsMPrecisionEqual
		{
			get { return _isMPrecisionEqual; }
			internal set { _isMPrecisionEqual = value; }
		}

		public bool? CoordinateSystemDifferent
		{
			get { return _coordinateSystemDifferent; }
			internal set { _coordinateSystemDifferent = value; }
		}

		public bool? VerticalCoordinateSystemDifferent
		{
			get { return _verticalCoordinateSystemDifferent; }
			internal set { _verticalCoordinateSystemDifferent = value; }
		}

		public bool? XyToleranceDifferent
		{
			get { return _xyToleranceDifferent; }
			internal set { _xyToleranceDifferent = value; }
		}

		public bool? ZToleranceDifferent
		{
			get { return _zToleranceDifferent; }
			internal set { _zToleranceDifferent = value; }
		}

		public bool? MToleranceDifferent
		{
			get { return _mToleranceDifferent; }
			internal set { _mToleranceDifferent = value; }
		}
	}
}
