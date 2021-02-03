using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Exceptions;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public class XmlExceptionConfiguration
	{
		private const ShapeMatchCriterion _defaultDefaultShapeMatchCriterion =
			ShapeMatchCriterion.EqualEnvelope;

		private const ExceptionObjectStatus _defaultExceptionObjectStatus =
			ExceptionObjectStatus.Active;

		private const bool _defaultExportExceptions = true;

		[XmlAttribute("defaultShapeMatchCriterion")]
		[DefaultValue(_defaultDefaultShapeMatchCriterion)]
		public ShapeMatchCriterion DefaultShapeMatchCriterion { get; set; } =
			_defaultDefaultShapeMatchCriterion;

		[XmlAttribute("defaultExceptionObjectStatus")]
		[DefaultValue(_defaultExceptionObjectStatus)]
		public ExceptionObjectStatus DefaultExceptionObjectStatus { get; set; } =
			_defaultExceptionObjectStatus;

		[XmlAttribute("exportExceptions")]
		[DefaultValue(_defaultExportExceptions)]
		public bool ExportExceptions { get; set; } = _defaultExportExceptions;

		[XmlElement("DataSource")]
		[CanBeNull]
		public string DataSource { get; set; }

		[XmlElement("InvolvedObjectsMatchCriteria")]
		[CanBeNull]
		public XmlInvolvedObjectsMatchCriteria InvolvedObjectsMatchCriteria { get; set; }
	}
}