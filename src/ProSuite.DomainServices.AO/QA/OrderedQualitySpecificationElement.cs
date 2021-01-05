using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA
{
	public class OrderedQualitySpecificationElement :
		IComparable<OrderedQualitySpecificationElement>
	{
		private readonly int _listOrder;
		private readonly int _executionPriority;
		private readonly bool _stopOnError;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderedQualitySpecificationElement"/> class.
		/// </summary>
		/// <param name="qualitySpecificationElement">The quality condition.</param>
		/// <param name="listOrder">The list order.</param>
		public OrderedQualitySpecificationElement(
			[NotNull] QualitySpecificationElement qualitySpecificationElement,
			int listOrder)
		{
			Assert.ArgumentNotNull(qualitySpecificationElement,
			                       nameof(qualitySpecificationElement));

			QualitySpecificationElement = qualitySpecificationElement;
			_listOrder = listOrder;

			_executionPriority =
				qualitySpecificationElement.QualityCondition.TestDescriptor.ExecutionPriority ??
				int.MaxValue;
			_stopOnError = qualitySpecificationElement.StopOnError;
		}

		[NotNull]
		public QualitySpecificationElement QualitySpecificationElement { get; }

		#region Implementation of IComparable<OrderedQualitySpecificationElement>

		public int CompareTo(OrderedQualitySpecificationElement other)
		{
			if (this == other)
			{
				return 0;
			}

			int i = -_stopOnError.CompareTo(other._stopOnError);
			if (i != 0)
			{
				return i;
			}

			int xPriority = _executionPriority;
			int yPriority = other._executionPriority;

			i = xPriority.CompareTo(yPriority);

			return i != 0
				       ? i
				       : _listOrder.CompareTo(other._listOrder);
		}

		#endregion
	}
}
