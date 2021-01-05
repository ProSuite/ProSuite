using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class XmlQualityConditionExceptions
	{
		[XmlAttribute("name")]
		public string QualityConditionName { get; set; }

		[XmlAttribute("exceptionCount")]
		public int ExceptionCount { get; set; }

		[XmlAttribute("exceptionObjectCount")]
		public int ExceptionObjectCount { get; set; }

		[XmlAttribute("unusedExceptionObjectCount")]
		[DefaultValue(0)]
		public int UnusedExceptionObjectCount { get; set; }

		[XmlAttribute("exceptionObjectsUsedMultipleTimesCount")]
		[DefaultValue(0)]
		public int ExceptionObjectsUsedMultipleTimesCount { get; set; }

		[XmlAttribute("exceptionObjectsIgnoredDueToUnknownTableNameCount")]
		[DefaultValue(0)]
		public int ExceptionObjectsIgnoredDueToUnknownTableNameCount { get; set; }

		[XmlArray("UnknownTableNames")]
		[XmlArrayItem("UnknownTableName")]
		[UsedImplicitly]
		[CanBeNull]
		public List<XmlUnknownTableName> UnknownTableNames { get; set; }

		[XmlArray("UnusedExceptionObjects")]
		[XmlArrayItem("ExceptionObject")]
		[UsedImplicitly]
		[CanBeNull]
		public List<XmlExceptionObject> UnusedExceptionObjects { get; set; }

		[XmlArray("ExceptionObjectsUsedMultipleTimes")]
		[XmlArrayItem("ExceptionObject")]
		[UsedImplicitly]
		[CanBeNull]
		public List<XmlExceptionObject> ExceptionObjectsUsedMultipleTimes { get; set; }

		public void AddUnusedExceptionObject([NotNull] XmlExceptionObject exceptionObject)
		{
			if (UnusedExceptionObjects == null)
			{
				UnusedExceptionObjects = new List<XmlExceptionObject>();
			}

			UnusedExceptionObjects.Add(exceptionObject);
		}

		public void AddExceptionObjectUsedMultipleTimes(
			[NotNull] XmlExceptionObject exceptionObject)
		{
			if (ExceptionObjectsUsedMultipleTimes == null)
			{
				ExceptionObjectsUsedMultipleTimes = new List<XmlExceptionObject>();
			}

			ExceptionObjectsUsedMultipleTimes.Add(exceptionObject);
		}

		public void AddUnknownTableName([NotNull] XmlUnknownTableName unknownTable)
		{
			if (UnknownTableNames == null)
			{
				UnknownTableNames = new List<XmlUnknownTableName>();
			}

			UnknownTableNames.Add(unknownTable);
		}
	}
}
