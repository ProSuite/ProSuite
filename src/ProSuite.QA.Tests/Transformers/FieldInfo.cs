using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.QA.Tests.Transformers
{
	public class FieldInfo
	{
		public FieldInfo(string name, int index, int sourceIndex)
		{
			Name = name;
			Index = index;
			SourceIndex = sourceIndex;
		}

		public string Name { get; }
		public int Index { get; }
		public int SourceIndex { get; }

		public static void SetGroupValue(IRow groupRow, IList<IRow> groupedRows,
		                                 IEnumerable<FieldInfo> fieldInfos)
		{
			foreach (FieldInfo fi in fieldInfos)
			{
				int iSource = fi.SourceIndex;
				{
					object value = null;
					bool unique = true;
					foreach (IRow row in groupedRows)
					{
						object v = row.Value[iSource];
						if (value == null)
						{
							value = v;
						}
						else if (! value.Equals(v))
						{
							unique = false;
						}
					}

					groupRow.set_Value(fi.Index, value);
				}
			}
		}
	}
}
