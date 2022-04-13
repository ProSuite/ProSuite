using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors.CreateQualityConditions
{
	internal class QualityConditionParameters
	{
		[NotNull] private readonly Dataset _dataset;
		[NotNull] private readonly string _name;
		[CanBeNull] private readonly string _filterExpression;

		private readonly IList<ScalarParameterValue> _scalarParameters =
			new List<ScalarParameterValue>();

		private readonly List<QualitySpecification> _qualitySpecifications =
			new List<QualitySpecification>();

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityConditionParameters"/> class.
		/// </summary>
		/// <param name="dataset">The dataset.</param>
		/// <param name="name">The name.</param>
		/// <param name="filterExpression">The filter expression.</param>
		public QualityConditionParameters([NotNull] Dataset dataset,
		                                  [NotNull] string name,
		                                  [CanBeNull] string filterExpression)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			_dataset = dataset;
			_name = name;
			_filterExpression = filterExpression;
		}

		[NotNull]
		public Dataset Dataset => _dataset;

		[NotNull]
		public string Name => _name;

		[CanBeNull]
		public string FilterExpression => _filterExpression;

		[NotNull]
		public IEnumerable<ScalarParameterValue> ScalarParameters => _scalarParameters;

		[NotNull]
		public IEnumerable<QualitySpecification> QualitySpecifications => _qualitySpecifications;

		public void AddQualitySpecification(
			[NotNull] QualitySpecification qualitySpecification)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));
			Assert.ArgumentCondition(! _qualitySpecifications.Contains(qualitySpecification),
			                         "Quality specification already in list");

			_qualitySpecifications.Add(qualitySpecification);
		}

		public void AddScalarParameter([NotNull] string name, [CanBeNull] object value)
		{
			string stringValue = value == null
				                     ? string.Empty
				                     : value.ToString();

			var scalarParameterValue = new ScalarParameterValue(name, stringValue);

			_scalarParameters.Add(scalarParameterValue);
		}
	}
}
