using System.ComponentModel;
using System.Xml.Serialization;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	public class XmlClassDescriptor
	{
		private string _description;
		private const int _defaultConstructor = -1;
		private int _constructorId = _defaultConstructor;

		[XmlAttribute("type")]
		public string TypeName { get; set; }

		[XmlAttribute("assembly")]
		public string AssemblyName { get; set; }

		[XmlAttribute("constructorIndex")]
		[DefaultValue(_defaultConstructor)]
		public int ConstructorId
		{
			get { return _constructorId; }
			set { _constructorId = value; }
		}

		[XmlElement("Description")]
		[DefaultValue(null)]
		public string Description
		{
			get
			{
				return string.IsNullOrEmpty(_description)
					       ? null
					       : _description;
			}
			set { _description = value; }
		}
	}
}
