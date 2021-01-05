using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class Issue
	{
		[NotNull] private readonly List<InvolvedTable> _involvedTables;

		/// <summary>
		/// Initializes a new instance of the <see cref="Issue"/> class.
		/// </summary>
		/// <param name="qualitySpecificationElement">The quality condition.</param>
		/// <param name="qaError">The QA error.</param>
		public Issue([NotNull] QualitySpecificationElement qualitySpecificationElement,
		             [NotNull] QaError qaError)
			: this(qualitySpecificationElement,
			       qaError.Description,
			       IssueUtils.GetInvolvedTables(qaError.InvolvedRows),
			       qaError.IssueCode,
			       qaError.AffectedComponent,
			       qaError.Values) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Issue"/> class.
		/// </summary>
		/// <param name="qualitySpecificationElement">The quality condition.</param>
		/// <param name="description">The description.</param>
		/// <param name="involvedRows">The involved rows.</param>
		/// <param name="issueCode">The issue code.</param>
		/// <param name="affectedComponent">The affected component.</param>
		/// <param name="values"></param>
		public Issue([NotNull] QualitySpecificationElement qualitySpecificationElement,
		             [NotNull] string description,
		             [NotNull] IEnumerable<InvolvedTable> involvedRows,
		             [CanBeNull] IssueCode issueCode = null,
		             [CanBeNull] string affectedComponent = null,
		             [CanBeNull] IEnumerable<object> values = null)
		{
			Assert.ArgumentNotNull(qualitySpecificationElement,
			                       nameof(qualitySpecificationElement));
			Assert.ArgumentNotNullOrEmpty(description, nameof(description));
			Assert.ArgumentNotNull(involvedRows, nameof(involvedRows));

			QualitySpecificationElement = qualitySpecificationElement;
			Description = description;
			IssueCode = issueCode;
			Values = values?.ToList();
			AffectedComponent = StringUtils.IsNullOrEmptyOrBlank(affectedComponent)
				                    ? null
				                    : affectedComponent;
			_involvedTables = new List<InvolvedTable>(involvedRows);
		}

		[NotNull]
		public string Description { get; }

		[NotNull]
		public IList<InvolvedTable> InvolvedTables => _involvedTables;

		[CanBeNull]
		public IssueCode IssueCode { get; }

		[CanBeNull]
		public string AffectedComponent { get; }

		[NotNull]
		public QualitySpecificationElement QualitySpecificationElement { get; }

		[NotNull]
		public QualityCondition QualityCondition
			=> QualitySpecificationElement.QualityCondition;

		public bool StopCondition => QualitySpecificationElement.StopOnError;

		public bool Allowable => QualitySpecificationElement.AllowErrors;

		[CanBeNull]
		public IList<object> Values { get; }

		public override string ToString()
		{
			return $"Description: {Description}, QualityCondition: {QualityCondition.Name}";
		}
	}
}
