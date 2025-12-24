using System.Collections.Generic;
using System.Windows.Media;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts;

public interface IAttributeReader
{
	T GetValue<T>(Row row, Attributes attribute);

	void ReadAttributes(Row fromRow, IWorkItem forItem, ISourceClass source);

	/// <summary>
	/// Parse an involved objects string, such as the QA involved rows.
	/// </summary>
	/// <param name="involvedString"></param>
	/// <param name="hasGeometry"></param>
	/// <returns></returns>
	IList<InvolvedTable> ParseInvolved(string involvedString, bool hasGeometry);

	/// <summary>
	/// Gets the name of the attribute.
	/// </summary>
	/// <param name="attribute"></param>
	/// <returns></returns>
	[CanBeNull]
	string GetName(Attributes attribute);

	public Brush GetSeverityBackColor(string severity);

	string GetSeverity(Row fromRow);
}
