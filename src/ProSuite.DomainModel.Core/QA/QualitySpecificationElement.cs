using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA
{
	public class QualitySpecificationElement : IEquatable<QualitySpecificationElement>
	{
		[UsedImplicitly] private bool? _stopOnErrorOverride;
		[UsedImplicitly] private bool? _allowErrorsOverride;
		[UsedImplicitly] private bool? _reportIndividualErrorsOverride;
		[UsedImplicitly] private readonly QualityCondition _qualityCondition;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QualitySpecificationElement"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		[UsedImplicitly]
		protected QualitySpecificationElement() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="QualitySpecificationElement"/> class.
		/// </summary>
		/// <param name="condition">The quality condition.</param>
		/// <param name="stopOnErrorOverride">The stop on error override.</param>
		/// <param name="allowErrorsOverride">The allow errors override.</param>
		/// <param name="disabled">if set to <c>true</c> the element will be disabled.</param>
		public QualitySpecificationElement([NotNull] QualityCondition condition,
		                                   bool? stopOnErrorOverride = null,
		                                   bool? allowErrorsOverride = null,
		                                   bool disabled = false)
		{
			Assert.ArgumentNotNull(condition, nameof(condition));

			_qualityCondition = condition;
			_stopOnErrorOverride = stopOnErrorOverride;
			_allowErrorsOverride = allowErrorsOverride;
			Enabled = ! disabled;
		}

		#endregion

		public bool StopOnError => _stopOnErrorOverride ?? _qualityCondition.StopOnError;

		public bool AllowErrors => _allowErrorsOverride ?? _qualityCondition.AllowErrors;

		// TODO use when writing errors using standard verification service
		public bool ReportIndividualErrors
			=> _reportIndividualErrorsOverride ?? _qualityCondition.ReportIndividualErrors;

		public bool? StopOnErrorOverride
		{
			get { return _stopOnErrorOverride; }
			set { _stopOnErrorOverride = value; }
		}

		public bool? AllowErrorsOverride
		{
			get { return _allowErrorsOverride; }
			set { _allowErrorsOverride = value; }
		}

		// TODO expose in ddx editor, include in xml schema
		public bool? ReportIndividualErrorsOverride
		{
			get { return _reportIndividualErrorsOverride; }
			set { _reportIndividualErrorsOverride = value; }
		}

		[NotNull]
		public QualityCondition QualityCondition => _qualityCondition;

		public bool Enabled { get; set; } = true;

		#region Object overrides

		public override string ToString()
		{
			return _qualityCondition.Name;
		}

		public bool Equals(QualitySpecificationElement qualitySpecificationElement)
		{
			if (qualitySpecificationElement == null)
			{
				return false;
			}

			if (! Equals(_qualityCondition, qualitySpecificationElement._qualityCondition))
			{
				return false;
			}

			if (! Equals(_stopOnErrorOverride,
			             qualitySpecificationElement._stopOnErrorOverride))
			{
				return false;
			}

			if (! Equals(_allowErrorsOverride,
			             qualitySpecificationElement._allowErrorsOverride))
			{
				return false;
			}

			if (! Equals(_reportIndividualErrorsOverride,
			             qualitySpecificationElement._reportIndividualErrorsOverride))
			{
				return false;
			}

			if (! Equals(Enabled, qualitySpecificationElement.Enabled))
			{
				return false;
			}

			return true;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			return Equals(obj as QualitySpecificationElement);
		}

		public override int GetHashCode()
		{
			int result = _stopOnErrorOverride.GetHashCode();
			result = 29 * result + _allowErrorsOverride.GetHashCode();
			result = 29 * result + _reportIndividualErrorsOverride.GetHashCode();
			result = 29 * result + _qualityCondition.GetHashCode();
			result = 29 * result + Enabled.GetHashCode();
			return result;
		}

		#endregion
	}
}
