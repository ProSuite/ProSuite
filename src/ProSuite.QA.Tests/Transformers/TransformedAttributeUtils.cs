using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.QA.Tests.Transformers
{
	public static class TransformedAttributeUtils
	{
		public static IValueList ToSimpleValueList(
			[NotNull] ICollection<CalculatedValue> extraValues,
			out IDictionary<int, int> copyMatrix)
		{
			copyMatrix = new Dictionary<int, int>();
			IValueList simpleList = new SimpleValueList(extraValues.Count);

			int index = 0;
			foreach (CalculatedValue calculated in extraValues)
			{
				simpleList.SetValue(index, calculated.Value);

				// Update the target-source copy matrix to redirect from the calculated index
				// in the target to the local row-value count + index:
				copyMatrix[calculated.TargetIndex] = index++;
			}

			return simpleList;
		}
	}
}
