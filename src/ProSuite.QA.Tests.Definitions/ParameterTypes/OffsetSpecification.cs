using System;
using System.Globalization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public class OffsetSpecification
	{
		[CanBeNull]
		public static OffsetSpecification Parse([CanBeNull] string offset,
		                                        [CanBeNull] IFormatProvider formatProvider)
		{
			if (offset == null)
			{
				return null;
			}

			string s = offset.Trim();

			if (s.Length == 0)
			{
				return null;
			}

			bool isNegative;
			string firstChar = s.Substring(0, 1);
			if (firstChar == "-")
			{
				isNegative = true;
				s = s.Remove(0, 1);
			}
			else if (firstChar == "+")
			{
				isNegative = false;
				s = s.Remove(0, 1);
			}
			else
			{
				isNegative = false;
			}

			if (s.Length == 0)
			{
				throw CreateArgumentException(offset);
			}

			string lastChar = s.Substring(s.Length - 1, 1);
			bool isPercentage;
			if (lastChar == "%")
			{
				s = s.Remove(s.Length - 1);
				isPercentage = true;
			}
			else
			{
				isPercentage = false;
			}

			double value;
			if (! double.TryParse(s.Trim(), NumberStyles.Any, formatProvider, out value))
			{
				throw CreateArgumentException(offset);
			}

			return new OffsetSpecification(isNegative
				                               ? value * -1
				                               : value, isPercentage);
		}

		public OffsetSpecification(double offsetValue, bool isPercentage = false)
		{
			OffsetValue = offsetValue;
			IsPercentage = isPercentage;
		}

		public double OffsetValue { get; }

		public bool IsPercentage { get; }

		public double ApplyTo(double referenceValue)
		{
			double absoluteOffset = IsPercentage
				                        ? referenceValue * OffsetValue / 100
				                        : OffsetValue;

			return referenceValue + absoluteOffset;
		}

		[NotNull]
		private static ArgumentException CreateArgumentException([NotNull] string offset)
		{
			return new ArgumentException(
				$"Invalid offset specification '{offset}'");
		}
	}
}
