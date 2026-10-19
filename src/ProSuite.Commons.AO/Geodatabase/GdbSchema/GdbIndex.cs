using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class GdbIndex : IIndex
	{
		private readonly GdbFields _fields = new GdbFields();

		public GdbIndex([NotNull] string name, bool isUnique = false, bool isAscending = true)
		{
			Name = name;
			IsUnique = isUnique;
			IsAscending = isAscending;
		}

		public void AddField([NotNull] IField field)
		{
			_fields.AddField(field);
		}

		public string Name { get; set; }
		public IFields Fields => _fields;
		public bool IsUnique { get; set; }
		public bool IsAscending { get; set; }
	}
}
