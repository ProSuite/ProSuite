using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.DependencyGraph
{
	public class DatasetDependency
	{
		[NotNull] private readonly QualitySpecificationElement _element;
		[NotNull] private readonly DatasetNode _fromDatasetNode;
		[NotNull] private readonly DatasetNode _toDatasetNode;
		[NotNull] private readonly string _fromParameterName;
		[NotNull] private readonly string _toParameterName;
		[CanBeNull] private readonly string _fromFilterExpression;
		[CanBeNull] private readonly string _toFilterExpression;
		private readonly bool _directed;

		public DatasetDependency([NotNull] QualitySpecificationElement element,
		                         [NotNull] DatasetNode fromDatasetNode,
		                         [NotNull] DatasetNode toDatasetNode,
		                         [NotNull] string fromParameterName,
		                         [NotNull] string toParameterName,
		                         [CanBeNull] string fromFilterExpression = null,
		                         [CanBeNull] string toFilterExpression = null,
		                         bool directed = true)
		{
			Assert.ArgumentNotNull(element, nameof(element));
			Assert.ArgumentNotNull(fromDatasetNode, nameof(fromDatasetNode));
			Assert.ArgumentNotNull(toDatasetNode, nameof(toDatasetNode));
			Assert.ArgumentNotNullOrEmpty(fromParameterName, nameof(fromParameterName));
			Assert.ArgumentNotNullOrEmpty(toParameterName, nameof(toParameterName));

			_element = element;
			_fromDatasetNode = fromDatasetNode;
			_toDatasetNode = toDatasetNode;
			_fromParameterName = fromParameterName;
			_toParameterName = toParameterName;
			_fromFilterExpression = fromFilterExpression;
			_toFilterExpression = toFilterExpression;
			_directed = directed;
		}

		[NotNull]
		public QualityCondition QualityCondition
		{
			get { return _element.QualityCondition; }
		}

		[NotNull]
		public QualitySpecificationElement Element
		{
			get { return _element; }
		}

		[NotNull]
		public DatasetNode FromDatasetNode
		{
			get { return _fromDatasetNode; }
		}

		[NotNull]
		public DatasetNode ToDatasetNode
		{
			get { return _toDatasetNode; }
		}

		[NotNull]
		public string FromParameterName
		{
			get { return _fromParameterName; }
		}

		[NotNull]
		public string ToParameterName
		{
			get { return _toParameterName; }
		}

		[CanBeNull]
		public string FromFilterExpression
		{
			get { return _fromFilterExpression; }
		}

		[CanBeNull]
		public string ToFilterExpression
		{
			get { return _toFilterExpression; }
		}

		public bool Directed
		{
			get { return _directed; }
		}

		public override string ToString()
		{
			return string.Format("QualityCondition: {0}, " +
			                     "FromDatasetNode: {1}, ToDatasetNode: {2}, " +
			                     "FromParameterName: {3}, ToParameterName: {4}, " +
			                     "FromFilterExpression: {5}, ToFilterExpression: {6}",
			                     _element.QualityCondition,
			                     _fromDatasetNode, _toDatasetNode,
			                     _fromParameterName, _toParameterName,
			                     _fromFilterExpression, _toFilterExpression);
		}
	}
}
