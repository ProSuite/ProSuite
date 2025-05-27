using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA
{
	public class QualityCondition : InstanceConfiguration, IPersistenceAware
	{
		private int _cloneId = -1;

		#region Persisted fields

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private bool? _stopOnErrorOverride;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private bool? _allowErrorsOverride;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private bool? _reportIndividualErrorsOverride;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private TestDescriptor _testDescriptor;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private bool _neverStoreRelatedGeometryForTableRowIssues;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private bool _neverFilterTableRowsUsingRelatedGeometry;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private readonly IList<IssueFilterConfiguration> _issueFilterConfigurations =
			new List<IssueFilterConfiguration>();

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _issueFilterExpression;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _versionUuid;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityCondition"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected QualityCondition() : this(assignUuids: false) { }

		public QualityCondition(bool assignUuids) : base(assignUuids)
		{
			if (assignUuids)
			{
				_versionUuid = GenerateUuid();
			}
		}

		public QualityCondition(string name,
		                        [NotNull] TestDescriptor testDescriptor,
		                        [CanBeNull] string description = "",
		                        bool assignUuids = true)
			: base(name, description, assignUuids)
		{
			Assert.ArgumentNotNull(testDescriptor, nameof(testDescriptor));

			_testDescriptor = testDescriptor;

			if (assignUuids)
			{
				_versionUuid = GenerateUuid();
			}
		}

		#endregion

		public override InstanceDescriptor InstanceDescriptor => TestDescriptor;

		[Required]
		public TestDescriptor TestDescriptor
		{
			get { return _testDescriptor; }
			set { _testDescriptor = value; }
		}

		protected override void InstanceDescriptorChanged()
		{
			// Redundancy (due to Nh mapping) must be accounted for:
			TestDescriptor = (TestDescriptor) base.InstanceDescriptor;
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

		/// <summary>
		/// The clone Id can be set if the instance is a clone of a persistent condition.
		/// </summary>
		/// <param name="id"></param>
		public void SetCloneId(int id)
		{
			Assert.True(base.Id < 0, "Persistent entity or already initialized clone.");
			_cloneId = id;
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

		[NotNull]
		public IList<IssueFilterConfiguration> IssueFilterConfigurations =>
			new ReadOnlyList<IssueFilterConfiguration>(_issueFilterConfigurations);

		[CanBeNull]
		public string IssueFilterExpression
		{
			get => _issueFilterExpression;
			set => _issueFilterExpression = value;
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

		/// <summary>
		/// This is a trick to avoid NHibernate creating a duplicate index name.
		/// </summary>
		[CanBeNull]
		public new DataQualityCategory Category
		{
			get => base.Category;
			set => base.Category = value;
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

		public void AddIssueFilterConfiguration(
			[NotNull] IssueFilterConfiguration issueFilterConfiguration)
		{
			_issueFilterConfigurations.Add(issueFilterConfiguration);
		}

		public bool RemoveIssueFilterConfiguration(
			[NotNull] IssueFilterConfiguration issueFilterConfiguration)
		{
			return _issueFilterConfigurations.Remove(issueFilterConfiguration);
		}

		public void ClearIssueFilterConfigurations()
		{
			_issueFilterConfigurations.Clear();
		}

		[NotNull]
		internal QualityCondition Clone()
		{
			var clone = new QualityCondition(assignUuids: false)
			            {
				            Uuid = Uuid,
				            VersionUuid = VersionUuid,
				            Version = Version,
				            _cloneId = Id
			            };

			CopyProperties(clone);

			return clone;
		}

		public override string TypeDisplayName => "Quality Condition";

		[NotNull]
		public override InstanceConfiguration CreateCopy()
		{
			var copy = new QualityCondition(assignUuids: true);

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

				foreach (Dataset dataset in datasetTestParameterValue.GetAllSourceDatasets(
					         onlyIfNotUsedAsReferenceData))
				{
					if (verifiedDatasets.Contains(dataset))
					{
						return true;
					}
				}
			}

			return false;
		}

		[NotNull]
		public IList<string> GetDeletedParameterValues()
		{
			List<string> result = new List<string>();

			result.AddRange(GetDeletedParameterValueMessages(ParameterValues));

			foreach (IssueFilterConfiguration issueFilter in IssueFilterConfigurations)
			{
				foreach (string deletedFilterParam in GetDeletedParameterValueMessages(
					         issueFilter.ParameterValues))
				{
					result.Add($"Issue filter {issueFilter.Name}: {deletedFilterParam}");
				}
			}

			return result;
		}

		private static IEnumerable<string> GetDeletedParameterValueMessages(
			[NotNull] IEnumerable<TestParameterValue> parameterValues)
		{
			foreach (TestParameterValue parameterValue in parameterValues)
			{
				var datasetTestParameterValue = parameterValue as DatasetTestParameterValue;

				Dataset dataset = datasetTestParameterValue?.DatasetValue;

				if (dataset != null && dataset.Deleted)
				{
					yield return $"{parameterValue.TestParameterName}: {dataset.Name}";
				}

				if (datasetTestParameterValue?.ValueSource != null)
				{
					foreach (Dataset deleted in datasetTestParameterValue.GetAllSourceDatasets()
						         .Where(d => d.Deleted))
					{
						yield return $"{parameterValue.TestParameterName}: {deleted.Name}";
					}
				}
			}
		}

		private void CopyProperties([NotNull] QualityCondition target)
		{
			Assert.ArgumentNotNull(target, nameof(target));

			CopyBaseProperties(target);

			target._testDescriptor = TestDescriptor;

			target._allowErrorsOverride = AllowErrorsOverride;
			target._stopOnErrorOverride = StopOnErrorOverride;
			target._reportIndividualErrorsOverride = ReportIndividualErrorsOverride;
			target.CanGroupIssuesByDescription = CanGroupIssuesByDescription;

			target._neverFilterTableRowsUsingRelatedGeometry =
				NeverFilterTableRowsUsingRelatedGeometry;
			target._neverStoreRelatedGeometryForTableRowIssues =
				NeverStoreRelatedGeometryForTableRowIssues;

			target._issueFilterExpression = IssueFilterExpression;

			foreach (var issueFilter in IssueFilterConfigurations)
			{
				target.AddIssueFilterConfiguration(issueFilter);
			}
		}

		protected override IEnumerable<Dataset> EnumReferencedDatasetParameterValues()
		{
			foreach (IssueFilterConfiguration issueFilterConfiguration in
			         IssueFilterConfigurations)
			{
				foreach (Dataset dataset in issueFilterConfiguration.GetDatasetParameterValues(
					         includeReferencedProcessors: true))
				{
					yield return dataset;
				}
			}

			foreach (Dataset dataset in base.EnumReferencedDatasetParameterValues())
			{
				yield return dataset;
			}
		}

		#region Implementation of IPersistenceAware

		void IPersistenceAware.OnCreate() { }

		void IPersistenceAware.OnUpdate() { }

		void IPersistenceAware.OnDelete() { }

		#endregion

		public override string ToString()
		{
			return $"Quality Condition '{Name}'";
		}
	}
}
