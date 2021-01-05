using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class XmlExceptionStatistics
	{
		[XmlAttribute("exceptionDataSource")]
		public string DataSource { get; set; }

		[XmlAttribute("exceptionCount")]
		public int ExceptionCount { get; set; }

		[XmlAttribute("exceptionObjectCount")]
		public int ExceptionObjectCount { get; set; }

		[XmlAttribute("inactiveExceptionObjectCount")]
		[DefaultValue(0)]
		public int InactiveExceptionObjectCount { get; set; }

		[XmlAttribute("unusedExceptionObjectCount")]
		[DefaultValue(0)]
		public int UnusedExceptionObjectCount { get; set; }

		[XmlAttribute("exceptionObjectsUsedMultipleTimesCount")]
		[DefaultValue(0)]
		public int ExceptionObjectsUsedMultipleTimesCount { get; set; }

		[XmlArray("TablesWithNonUniqueKeys")]
		[XmlArrayItem("Table")]
		[UsedImplicitly]
		[CanBeNull]
		public List<XmlTableWithNonUniqueKeys> TablesWithNonUniqueKeys { get; set; }

		[XmlArray("QualityConditionExceptions")]
		[XmlArrayItem("QualityCondition")]
		[UsedImplicitly]
		[CanBeNull]
		public List<XmlQualityConditionExceptions> QualityConditionExceptions { get; set; }

		public void AddQualityConditionExceptions(
			[NotNull] XmlQualityConditionExceptions exceptions)
		{
			if (QualityConditionExceptions == null)
			{
				QualityConditionExceptions = new List<XmlQualityConditionExceptions>();
			}

			QualityConditionExceptions.Add(exceptions);
		}

		public void AddTableWithNonUniqueKeys([NotNull] XmlTableWithNonUniqueKeys table)
		{
			if (TablesWithNonUniqueKeys == null)
			{
				TablesWithNonUniqueKeys = new List<XmlTableWithNonUniqueKeys>();
			}

			TablesWithNonUniqueKeys.Add(table);
		}
	}
}
