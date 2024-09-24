using System.Collections.Generic;

namespace ProSuite.GIS.Geodatabase.API
{
	public interface ISubtypes
	{
		bool HasSubtype { get; }

		int DefaultSubtypeCode { get; set; }

		object get_DefaultValue(int subtypeCode, string fieldName);

		void set_DefaultValue(int subtypeCode, string fieldName, object value);

		IDomain get_Domain(int subtypeCode, string fieldName);

		//void set_Domain(int SubtypeCode, string FieldName, IDomain Domain);

		string SubtypeFieldName { get; set; }

		int SubtypeFieldIndex { get; }

		string get_SubtypeName(int subtypeCode);

		IEnumerable<KeyValuePair<int, string>> Subtypes { get; }

		void AddSubtype(int subtypeCode, string subtypeName);

		void DeleteSubtype(int subtypeCode);
	}
}
