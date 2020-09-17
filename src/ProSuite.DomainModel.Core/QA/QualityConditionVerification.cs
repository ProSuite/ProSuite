using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA
{
	public class QualityConditionVerification
	{
		[UsedImplicitly] private QualityCondition _qualityCondition;

		[UsedImplicitly] private QualityCondition _stopCondition;

		[UsedImplicitly] private readonly string _qualityConditionName;
		[UsedImplicitly] private readonly int _qualityConditionId;
		[UsedImplicitly] private readonly int _qualityConditionVersion;

		[UsedImplicitly] private readonly string _qualityConditionParamValues;
		[UsedImplicitly] private readonly string _testType;
		[UsedImplicitly] private readonly int _constructorId;

		[UsedImplicitly] private int _errorCount;
		[UsedImplicitly] private bool _fulfilled = true;
		[UsedImplicitly] private readonly bool _allowErrors;

		[UsedImplicitly] private readonly bool _stopOnError;

		[UsedImplicitly] private double _executeTime;
		[UsedImplicitly] private double _rowExecuteTime;
		[UsedImplicitly] private double _tileExecuteTime;

		private const int _maxLengthParamValues = 2000; // correspond to hibernate mapping
		private QualityCondition _displayableCondition;

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityConditionVerification"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		[UsedImplicitly]
		protected QualityConditionVerification() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityConditionVerification"/> class.
		/// </summary>
		/// <param name="element">The specification element that was verified.</param>
		public QualityConditionVerification([NotNull] QualitySpecificationElement element)
		{
			Assert.ArgumentNotNull(element, nameof(element));

			QualityCondition qualityCondition = element.QualityCondition;

			_qualityCondition = qualityCondition;
			_qualityConditionId = qualityCondition.Id;
			_qualityConditionVersion = qualityCondition.Version;
			_qualityConditionName = qualityCondition.Name;
			_qualityConditionParamValues =
				qualityCondition.GetParameterValuesString(_maxLengthParamValues);

			TestDescriptor testDescriptor = qualityCondition.TestDescriptor;
			ClassDescriptor testClass = testDescriptor.TestClass;

			if (testClass != null)
			{
				_testType = testClass.TypeName;
				_constructorId = testDescriptor.TestConstructorId;
			}
			else
			{
				ClassDescriptor factoryDescriptor = testDescriptor.TestFactoryDescriptor;
				Assert.NotNull(factoryDescriptor,
				               "both TestClass and TestFactory descriptors are null");

				_testType = factoryDescriptor.TypeName;
				_constructorId = -1;
			}

			_allowErrors = element.AllowErrors;
			_stopOnError = element.StopOnError;
		}

		[CanBeNull]
		public QualityCondition QualityCondition
		{
			get { return _qualityCondition; }
		}

		public void ClearQualityCondition()
		{
			_qualityCondition = null;
			_displayableCondition = null;
		}

		public bool AllowErrors
		{
			get { return _allowErrors; }
		}

		public bool StopOnError
		{
			get { return _stopOnError; }
		}

		public QualityCondition StopCondition
		{
			get { return _stopCondition; }
			set
			{
				_stopCondition = value;
				if (value != null)
				{
					_fulfilled = false;
				}
			}
		}

		public bool Fulfilled
		{
			get { return _fulfilled; }
			set { _fulfilled = value; }
		}

		public int ErrorCount
		{
			get { return _errorCount; }
			set { _errorCount = value; }
		}

		public double TotalExecuteTime
		{
			get { return _executeTime + _rowExecuteTime + _tileExecuteTime; }
		}

		public double ExecuteTime
		{
			get { return _executeTime; }
			set { _executeTime = value; }
		}

		public double RowExecuteTime
		{
			get { return _rowExecuteTime; }
			set { _rowExecuteTime = value; }
		}

		public double TileExecuteTime
		{
			get { return _tileExecuteTime; }
			set { _tileExecuteTime = value; }
		}

		[UsedImplicitly]
		private int QualityConditionId => _qualityConditionId;

		[UsedImplicitly]
		private string QualityConditionName => _qualityConditionName;

		[UsedImplicitly]
		private int QualityConditionVersion => _qualityConditionVersion;

		[UsedImplicitly]
		private string QualityConditionParamValues => _qualityConditionParamValues;

		[UsedImplicitly]
		private string TestType => _testType;

		[UsedImplicitly]
		private int ConstructorId => _constructorId;

		public double LoadTime([NotNull] QualityVerification verification)
		{
			double result = 0;
			QualityCondition condition = DisplayableCondition;

			foreach (Dataset dataset in condition.GetDatasetParameterValues())
			{
				QualityVerificationDataset verificationDataset =
					verification.GetVerificationDataset(dataset);

				if (verificationDataset == null)
				{
					continue;
				}

				result += verificationDataset.LoadTime;
			}

			return result;
		}

		[NotNull]
		public QualityCondition DisplayableCondition
		{
			get
			{
				return _displayableCondition ??
				       (_displayableCondition = GetDisplayableCondition());
			}
		}

		[NotNull]
		private QualityCondition GetDisplayableCondition()
		{
			// if there is a life reference to the correct version of the 
			// quality condition, directly return that reference
			if (_qualityCondition != null)
			{
				if (_qualityCondition.Id == _qualityConditionId &&
				    _qualityCondition.Version == _qualityConditionVersion)
				{
					return _qualityCondition;
				}

				if (string.IsNullOrEmpty(_qualityConditionName))
				{
					// for backwards compatiblity

					// _qualityConditionName == null --> _qualityConditionId/ -Version invalid
					// assume that quality condition is still valid
					return _qualityCondition;
				}
			}

			// either the qualiy condition was deleted, or it was changed since the verification
			var result = new QualityCondition(assignUuids : true);

			if (string.IsNullOrEmpty(_qualityConditionName))
			{
				result.Name = "<unknown>";
				result.Description =
					"unknown, deleted quality condition without stored " +
					"information in quality verification";
				return result;
			}

			result.Name = _qualityConditionName;

			foreach (TestParameterValue testParameterValue
				in TestParameterStringUtils.ParseTestParameterValues(
					_qualityConditionParamValues))
			{
				result.AddParameterValue(testParameterValue);
			}

			result.TestDescriptor = TestDescriptor.CreateDisplayableTestDescriptor(
				_testType, _constructorId);

			string description = _qualityCondition != null &&
			                     _qualityCondition.Id == _qualityConditionId
				                     ? "changed test without stored description information" +
				                       " (Version " + _qualityConditionVersion + " )"
				                     : "unknown, deleted or changed test without stored description information";

			result.Description = description + Environment.NewLine +
			                     result.TestDescriptor.Description;

			result.AllowErrorsOverride = _allowErrors;
			result.StopOnErrorOverride = _stopOnError;

			return result;
		}
	}
}