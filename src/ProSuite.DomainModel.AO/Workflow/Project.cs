using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.Workflow
{
	public abstract class Project<T> : VersionedEntityWithMetadata, INamed, IAnnotated,
	                                   IDetachedState
		where T : ProductionModel
	{
		[UsedImplicitly] private string _name;
		[UsedImplicitly] private string _shortName;
		[UsedImplicitly] private string _description;

		[UsedImplicitly] private T _productionModel;

		[UsedImplicitly] private double? _fullExtentXMax;
		[UsedImplicitly] private double? _fullExtentXMin;
		[UsedImplicitly] private double? _fullExtentYMax;
		[UsedImplicitly] private double? _fullExtentYMin;

		[UsedImplicitly] private double _minimumScaleDenominator = 500;
		[UsedImplicitly] private int _minimumStickyMoveTolerance = 100;

		[UsedImplicitly] private double _qualityVerificationTileSize = 20000;

		[UsedImplicitly] private string _attributeEditorConfigDirectory;
		[UsedImplicitly] private string _workListConfigDirectory;
		[UsedImplicitly] private string _toolConfigDirectory;

		[UsedImplicitly] private bool _excludeReadOnlyDatasetsFromProjectWorkspace = true;
		[UsedImplicitly] private bool _useOnlyModelDefaultDatabase = true;
		[UsedImplicitly] private string _nonDefaultDatabaseDatasetNameTransformations;
		[UsedImplicitly] private string _nonDefaultDatabaseRestrictions;

		[UsedImplicitly] private int _maximumPointCountForDerivedIssueGeometries = 20000;

		[UsedImplicitly] private QualityVerificationOutsideEditSessionAllowed
			_qualityVerificationOutsideEditSessionAllowed =
				QualityVerificationOutsideEditSessionAllowed.Always;

		[UsedImplicitly] private bool _deriveGeometryOnlyFromVerifiedVectorDatasets;

		private IWorkspaceFilter _childDatabaseWorkspaceFilter;
		private IDatasetNameTransformer _childDatabaseDatasetNameTransformer;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Project&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="description">The description.</param>
		protected Project([CanBeNull] string name = null,
		                  [CanBeNull] string description = null)
		{
			_name = name;
			_description = description;
		}

		#endregion

		[Required]
		[UsedImplicitly]
		[MaximumStringLength(10)]
		public string ShortName
		{
			get { return _shortName; }
			set { _shortName = value; }
		}

		[Required]
		[UsedImplicitly]
		public T ProductionModel
		{
			get { return _productionModel; }
			set { _productionModel = value; }
		}

		[UsedImplicitly]
		public double? FullExtentXMin
		{
			get { return _fullExtentXMin; }
			set { _fullExtentXMin = value; }
		}

		[UsedImplicitly]
		public double? FullExtentYMin
		{
			get { return _fullExtentYMin; }
			set { _fullExtentYMin = value; }
		}

		[UsedImplicitly]
		public double? FullExtentXMax
		{
			get { return _fullExtentXMax; }
			set { _fullExtentXMax = value; }
		}

		[UsedImplicitly]
		public double? FullExtentYMax
		{
			get { return _fullExtentYMax; }
			set { _fullExtentYMax = value; }
		}

		[GreaterThanZero]
		[UsedImplicitly]
		public double QualityVerificationTileSize
		{
			get { return _qualityVerificationTileSize; }
			set
			{
				Assert.ArgumentCondition(value > 0, "must be > 0");

				_qualityVerificationTileSize = value;
			}
		}

		[GreaterOrEqualToZero]
		[UsedImplicitly]
		public int MaximumPointCountForDerivedIssueGeometries
		{
			get { return _maximumPointCountForDerivedIssueGeometries; }
			set
			{
				Assert.ArgumentCondition(value >= 0, "must be >= 0");

				_maximumPointCountForDerivedIssueGeometries = value;
			}
		}

		[UsedImplicitly]
		public QualityVerificationOutsideEditSessionAllowed
			QualityVerificationOutsideEditSessionAllowed
		{
			get { return _qualityVerificationOutsideEditSessionAllowed; }
			set { _qualityVerificationOutsideEditSessionAllowed = value; }
		}

		[UsedImplicitly]
		public bool DeriveGeometryOnlyFromVerifiedVectorDatasets
		{
			get { return _deriveGeometryOnlyFromVerifiedVectorDatasets; }
			set { _deriveGeometryOnlyFromVerifiedVectorDatasets = value; }
		}

		[GreaterThanZero]
		[UsedImplicitly]
		public int MinimumStickyMoveTolerance
		{
			get { return _minimumStickyMoveTolerance; }
			set { _minimumStickyMoveTolerance = value; }
		}

		[GreaterThanZero]
		[UsedImplicitly]
		public double MinimumScaleDenominator
		{
			get { return _minimumScaleDenominator; }
			set { _minimumScaleDenominator = value; }
		}

		[CanBeNull]
		[UsedImplicitly]
		public string WorkListConfigDirectory
		{
			get { return _workListConfigDirectory; }
			set { _workListConfigDirectory = value; }
		}

		[CanBeNull]
		[UsedImplicitly]
		public string AttributeEditorConfigDirectory
		{
			get { return _attributeEditorConfigDirectory; }
			set { _attributeEditorConfigDirectory = value; }
		}

		[CanBeNull]
		[UsedImplicitly]
		public string ToolConfigDirectory
		{
			get { return _toolConfigDirectory; }
			set { _toolConfigDirectory = value; }
		}

		[UsedImplicitly]
		public bool ExcludeReadOnlyDatasetsFromProjectWorkspace
		{
			get { return _excludeReadOnlyDatasetsFromProjectWorkspace; }
			set { _excludeReadOnlyDatasetsFromProjectWorkspace = value; }
		}

		[UsedImplicitly]
		public bool UseOnlyModelDefaultDatabase
		{
			get { return _useOnlyModelDefaultDatabase; }
			set { _useOnlyModelDefaultDatabase = value; }
		}

		[UsedImplicitly]
		public string NonDefaultDatabaseRestrictions
		{
			get { return _nonDefaultDatabaseRestrictions; }
			set { _nonDefaultDatabaseRestrictions = value; }
		}

		public bool SupportChildDatabases => ! UseOnlyModelDefaultDatabase;

		[CanBeNull]
		public string NonDefaultDatabaseDatasetNameTransformations
		{
			get { return _nonDefaultDatabaseDatasetNameTransformations; }
			set { _nonDefaultDatabaseDatasetNameTransformations = value; }
		}

		public bool HasFullExtent => _fullExtentXMin != null &&
		                             _fullExtentYMin != null &&
		                             _fullExtentXMax != null &&
		                             _fullExtentYMax != null;

		[CanBeNull]
		public IEnvelope GetFullExtent()
		{
			if (_fullExtentXMin == null ||
			    _fullExtentYMin == null ||
			    _fullExtentXMax == null ||
			    _fullExtentYMax == null)
			{
				return null;
			}

			IEnvelope extent =
				GeometryFactory.CreateEnvelope(_fullExtentXMin.Value,
				                               _fullExtentYMin.Value,
				                               _fullExtentXMax.Value,
				                               _fullExtentYMax.Value);

			extent.SpatialReference =
				_productionModel.SpatialReferenceDescriptor.GetSpatialReference();

			return extent;
		}

		public void SetFullExtent(double xmin, double ymin, double xmax, double ymax)
		{
			_fullExtentXMin = xmin;
			_fullExtentYMin = ymin;
			_fullExtentXMax = xmax;
			_fullExtentYMax = ymax;
		}

		[NotNull]
		public IWorkspaceFilter ChildDatabaseWorkspaceFilter =>
			_childDatabaseWorkspaceFilter ??
			(_childDatabaseWorkspaceFilter =
				 ProjectUtils.CreateChildDatabaseWorkspaceFilter(
					 _nonDefaultDatabaseRestrictions));

		[NotNull]
		public IDatasetNameTransformer ChildDatabaseDatasetNameTransformer =>
			_childDatabaseDatasetNameTransformer ??
			(_childDatabaseDatasetNameTransformer =
				 ProjectUtils.CreateDatasetNameTransformer(
					 _nonDefaultDatabaseDatasetNameTransformations));

		#region IAnnotated Members

		[MaximumStringLength(2000)]
		[UsedImplicitly]
		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		#endregion

		#region IDetachedState Members

		public void ReattachState(IUnitOfWork unitOfWork)
		{
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));

			if (IsPersistent)
			{
				unitOfWork.Reattach(this);
			}

			_productionModel?.ReattachState(unitOfWork);
		}

		#endregion

		#region Object overrides

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}

			var project = obj as Project<T>;
			if (project == null)
			{
				return false;
			}

			return Equals(_name, project._name) && Equals(_shortName, project._shortName);
		}

		public override int GetHashCode()
		{
			return _name.GetHashCode() + 29 * _shortName.GetHashCode();
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return $"{_shortName} ({_name})";
		}

		#endregion

		#region INamed Members

		[Required]
		[MaximumStringLength(200)]
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		#endregion
	}
}
