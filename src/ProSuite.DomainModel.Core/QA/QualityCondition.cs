using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA
{
	public class QualityCondition : VersionedEntityWithMetadata, INamed, IAnnotated,
	                                IPersistenceAware
	{
		private int _cloneId = -1;

		#region Persisted fields

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private readonly
			IList<TestParameterValue> _parameterValues =
				new List<TestParameterValue>();

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private DataQualityCategory _category;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _description;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _url;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _name;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _notes;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private bool? _stopOnErrorOverride;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private bool? _allowErrorsOverride;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private bool?
			_reportIndividualErrorsOverride;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private TestDescriptor _testDescriptor;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private bool
			_neverStoreRelatedGeometryForTableRowIssues;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private bool
			_neverFilterTableRowsUsingRelatedGeometry;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _uuid;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _versionUuid;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityCondition"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected QualityCondition() : this(assignUuids : false) { }

		public QualityCondition(bool assignUuids)
		{
			if (assignUuids)
			{
				_uuid = GenerateUuid();
				_versionUuid = GenerateUuid();
			}
		}

		public QualityCondition(string name,
		                        [NotNull] TestDescriptor testDescriptor,
		                        [CanBeNull] string description = "",
		                        bool assignUuids = true)
			: this(assignUuids)
		{
			Assert.ArgumentNotNull(testDescriptor, nameof(testDescriptor));

			_name = name;
			_testDescriptor = testDescriptor;
			_description = description;
		}

		#endregion

		[Required]
		public TestDescriptor TestDescriptor
		{
			get { return _testDescriptor; }
			set { _testDescriptor = value; }
		}

		[Required]
		public string Uuid
		{
			get { return _uuid; }
			set
			{
				Assert.ArgumentNotNull(value, nameof(value));

				_uuid = GetUuid(value);
			}
		}

		[Required]
		public string VersionUuid
		{
			get { return _versionUuid; }
			set
			{
				Assert.ArgumentNotNull(value, nameof(value));

				_versionUuid = GetUuid(value);
			}
		}

		public bool Updated { get; private set; }

		public void AssignNewVersionUuid()
		{
			_versionUuid = GenerateUuid();
		}

		[Required]
		[MaximumStringLength(200)]
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		[MaximumStringLength(2000)]
		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		[MaximumStringLength(2000)]
		public string Url
		{
			get { return _url; }
			set { _url = value; }
		}

		[MaximumStringLength(2000)]
		public string Notes
		{
			get { return _notes; }
			set { _notes = value; }
		}

		public bool StopOnError
		{
			get
			{
				if (_stopOnErrorOverride.HasValue)
				{
					return _stopOnErrorOverride.Value;
				}

				return _testDescriptor != null && _testDescriptor.StopOnError;
			}
		}

		public bool? StopOnErrorOverride
		{
			get { return _stopOnErrorOverride; }
			set { _stopOnErrorOverride = value; }
		}

		public bool AllowErrors
		{
			get
			{
				if (_allowErrorsOverride.HasValue)
				{
					return _allowErrorsOverride.Value;
				}

				return _testDescriptor != null && _testDescriptor.AllowErrors;
			}
		}

		public bool? AllowErrorsOverride
		{
			get { return _allowErrorsOverride; }
			set { _allowErrorsOverride = value; }
		}

		public bool ReportIndividualErrors
		{
			get
			{
				if (_reportIndividualErrorsOverride.HasValue)
				{
					return _reportIndividualErrorsOverride.Value;
				}

				return _testDescriptor == null || _testDescriptor.ReportIndividualErrors;
			}
		}

		public bool? ReportIndividualErrorsOverride
		{
			get { return _reportIndividualErrorsOverride; }
			set { _reportIndividualErrorsOverride = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether issues violating this condition can be grouped by their description text
		/// </summary>
		/// <value>
		/// 	<c>true</c> if issues for this condition can be grouped by their description; otherwise, <c>false</c>.
		/// </value>
		/// <remarks>This property is still experimental, name/type might change as issue reporting matures.</remarks>
		public bool CanGroupIssuesByDescription { get; set; }

		public bool NeverStoreRelatedGeometryForTableRowIssues
		{
			get { return _neverStoreRelatedGeometryForTableRowIssues; }
			set { _neverStoreRelatedGeometryForTableRowIssues = value; }
		}

		public bool NeverFilterTableRowsUsingRelatedGeometry
		{
			get { return _neverFilterTableRowsUsingRelatedGeometry; }
			set { _neverFilterTableRowsUsingRelatedGeometry = value; }
		}

		[CanBeNull]
		public DataQualityCategory Category
		{
			get { return _category; }
			set { _category = value; }
		}

		public new int Id
		{
			get
			{
				if (base.Id < 0 && _cloneId >= 0)
				{
					return _cloneId;
				}

				return base.Id;
			}
		}

		[NotNull]
		public IList<TestParameterValue> ParameterValues =>
			new ReadOnlyList<TestParameterValue>(_parameterValues);

		public void AddParameterValue([NotNull] TestParameterValue parameterValue)
		{
			Assert.ArgumentNotNull(parameterValue, nameof(parameterValue));

			_parameterValues.Add(parameterValue);
		}

		public void RemoveParameterValue([NotNull] TestParameterValue parameterValue)
		{
			_parameterValues.Remove(parameterValue);
		}

		public void ClearParameterValues()
		{
			_parameterValues.Clear();
		}

		/// <summary>
		/// Get the parameter values for a test parameter name.
		/// </summary>
		/// <returns>List with parameter values (for collections there might be more than one)
		/// for test parameter name, or empty if not found.</returns>
		[NotNull]
		public IList<TestParameterValue> GetParameterValues([NotNull] string testParameterName)
		{
			Assert.ArgumentNotNullOrEmpty(testParameterName, nameof(testParameterName));

			var result = new List<TestParameterValue>();

			foreach (TestParameterValue parameterValue in ParameterValues)
			{
				if (string.Compare(testParameterName,
				                   parameterValue.TestParameterName,
				                   StringComparison.OrdinalIgnoreCase) == 0)
				{
					result.Add(parameterValue);
				}
			}

			return result;
		}

		[NotNull]
		public string GetParameterValuesString(int maxLength = int.MaxValue)
		{
			return TestParameterStringUtils.FormatParameterValues(ParameterValues, maxLength);
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}

			var qualityCondition = obj as QualityCondition;
			if (qualityCondition == null)
			{
				return false;
			}

			if (! Equals(_name, qualityCondition._name))
			{
				return false;
			}

			if (! Equals(_testDescriptor, qualityCondition._testDescriptor))
			{
				return false;
			}

			// NOTE: comparison on parameter values (count, values) omitted (https://issuetracker02.eggits.net/browse/TOP-3665)

			return true;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = _testDescriptor.GetHashCode();
				result = (result * 397) ^ _name.GetHashCode();
				// result = (result * 397) ^ _parameterValues.GetHashCode();
				return result;
			}
		}

		public override string ToString()
		{
			return Name;
		}

		[NotNull]
		internal QualityCondition Clone()
		{
			var clone = new QualityCondition(assignUuids : false)
			            {
				            Uuid = Uuid,
				            VersionUuid = VersionUuid,
				            Version = Version,
				            _cloneId = Id
			            };

			CopyProperties(clone);

			return clone;
		}

		[NotNull]
		public QualityCondition CreateCopy()
		{
			var copy = new QualityCondition(assignUuids : true);

			CopyProperties(copy);

			return copy;
		}

		public void UpdateParameterValuesFrom([NotNull] QualityCondition updateCondition,
		                                      bool updateIsOriginal = false)
		{
			var updateValues = new Dictionary<string, List<TestParameterValue>>();
			foreach (TestParameterValue updateParameterValue in updateCondition.ParameterValues)
			{
				List<TestParameterValue> values;
				if (! updateValues.TryGetValue(updateParameterValue.TestParameterName, out values))
				{
					values = new List<TestParameterValue>();
					updateValues.Add(updateParameterValue.TestParameterName, values);
				}

				values.Add(updateParameterValue);
			}

			var hasUpdates = false;

			var toRemoves = new List<TestParameterValue>();
			foreach (TestParameterValue parameterValue in ParameterValues)
			{
				List<TestParameterValue> updates;
				if (! updateValues.TryGetValue(parameterValue.TestParameterName, out updates) ||
				    updates.Count == 0)
				{
					toRemoves.Add(parameterValue);
					hasUpdates = true;
					continue;
				}

				TestParameterValue update = updates[0];
				updates.RemoveAt(0);

				if (parameterValue.UpdateFrom(update))
				{
					hasUpdates = true;
				}
			}

			foreach (TestParameterValue toRemove in toRemoves)
			{
				RemoveParameterValue(toRemove);
			}

			foreach (List<TestParameterValue> missingValues in updateValues.Values)
			{
				foreach (TestParameterValue missingValue in missingValues)
				{
					AddParameterValue(missingValue);
					hasUpdates = true;
				}
			}

			if (hasUpdates)
			{
				Updated = true;
			}

			if (updateIsOriginal)
			{
				Updated = false;
			}
		}

		public bool IsApplicableFor([NotNull] ICollection<Dataset> verifiedDatasets,
		                            bool onlyIfNotUsedAsReferenceData = false)
		{
			Assert.ArgumentNotNull(verifiedDatasets, nameof(verifiedDatasets));

			foreach (TestParameterValue parameterValue in ParameterValues)
			{
				var datasetTestParameterValue = parameterValue as DatasetTestParameterValue;
				if (datasetTestParameterValue == null)
				{
					continue;
				}

				if (onlyIfNotUsedAsReferenceData && datasetTestParameterValue.UsedAsReferenceData)
				{
					continue;
				}

				if (verifiedDatasets.Contains(datasetTestParameterValue.DatasetValue))
				{
					return true;
				}
			}

			return false;
		}

		[NotNull]
		public IEnumerable<Dataset> GetDatasetParameterValues()
		{
			foreach (TestParameterValue parameterValue in ParameterValues)
			{
				var datasetTestParameterValue = parameterValue as DatasetTestParameterValue;

				Dataset dataset = datasetTestParameterValue?.DatasetValue;

				if (dataset != null)
				{
					yield return dataset;
				}
			}
		}

		[NotNull]
		public IList<TestParameterValue> GetDeletedParameterValues()
		{
			List<TestParameterValue> result = new List<TestParameterValue>();

			foreach (TestParameterValue parameterValue in ParameterValues)
			{
				var datasetTestParameterValue = parameterValue as DatasetTestParameterValue;

				Dataset dataset = datasetTestParameterValue?.DatasetValue;

				if (dataset != null && dataset.Deleted)
				{
					result.Add(datasetTestParameterValue);
				}
			}

			return result;
		}

		[NotNull]
		private static string GetUuid([NotNull] string value)
		{
			// this fails if the string is not a valid guid:
			var guid = new Guid(value);

			return FormatUuid(guid);
		}

		[NotNull]
		private static string GenerateUuid()
		{
			return FormatUuid(Guid.NewGuid());
		}

		[NotNull]
		private static string FormatUuid(Guid guid)
		{
			// default format (no curly braces)
			return guid.ToString().ToUpper();
		}

		private void CopyProperties([NotNull] QualityCondition target)
		{
			Assert.ArgumentNotNull(target, nameof(target));

			target._name = Name;
			target._testDescriptor = TestDescriptor;
			target._description = Description;
			target._notes = Notes;
			target._url = Url;

			target._allowErrorsOverride = AllowErrorsOverride;
			target._stopOnErrorOverride = StopOnErrorOverride;
			target._reportIndividualErrorsOverride = ReportIndividualErrorsOverride;
			target.CanGroupIssuesByDescription = CanGroupIssuesByDescription;

			target._neverFilterTableRowsUsingRelatedGeometry =
				NeverFilterTableRowsUsingRelatedGeometry;
			target._neverStoreRelatedGeometryForTableRowIssues =
				NeverStoreRelatedGeometryForTableRowIssues;

			foreach (TestParameterValue testParameterValue in ParameterValues)
			{
				target.AddParameterValue(testParameterValue.Clone());
			}

			target._category = _category;
		}

		#region Implementation of IPersistenceAware

		void IPersistenceAware.OnCreate() { }

		void IPersistenceAware.OnUpdate() { }

		void IPersistenceAware.OnDelete() { }

		#endregion
	}
}