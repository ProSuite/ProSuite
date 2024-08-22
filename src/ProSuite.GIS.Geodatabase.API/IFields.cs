using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ESRI.ArcGIS.Geodatabase
{
	public interface IFields
	{
		int FieldCount { get; }

		IList<IField> Field { get; }

		IField get_Field(int index);

		int FindField(string name);

		int FindFieldByAliasName(string name);
	}
}
