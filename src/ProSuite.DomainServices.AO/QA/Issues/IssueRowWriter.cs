using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class IssueRowWriter : IssueWriter
	{
		[NotNull] private readonly IObjectClass _objectClass;

		public IssueRowWriter([NotNull] IObjectClass objectClass,
		                      [NotNull] IIssueAttributeWriter issueAttributeWriter)
			: base(objectClass, issueAttributeWriter)
		{
			_objectClass = objectClass;
		}

		#region Overrides of IssueWriter

		protected override IRowBuffer CreateRowBuffer()
		{
			return ((ITable) _objectClass).CreateRowBuffer();
		}

		#endregion
	}
}
