using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.DatasetReports
{
	public class FieldValueRange
	{
		public void Add([CanBeNull] object value)
		{
			if (value is DBNull || value == null)
			{
				NullCount++;
				return;
			}

			ValueCount++;
			var comparableValue = (IComparable) value;

			if (MinimumValue == null)
			{
				MinimumValue = value;
			}
			else
			{
				if (comparableValue.CompareTo(MinimumValue) < 0)
				{
					MinimumValue = value;
				}
			}

			if (MaximumValue == null)
			{
				MaximumValue = value;
			}
			else
			{
				if (comparableValue.CompareTo(MaximumValue) > 0)
				{
					MaximumValue = value;
				}
			}
		}

		public object MinimumValue { get; private set; }

		public object MaximumValue { get; private set; }

		public int ValueCount { get; private set; }

		public int NullCount { get; private set; }
	}
}
