using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.QA.Test;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Test implementation for external service. At some point the features (current tile) could be streamed.
	/// Currently the workspace has to be accessible (and probably known) by the external service.
	///  TODO:
	/// - Naming?
	/// - Cancelling? -> generally not implemented
	/// 
	/// - Timeout? server error -> throw exception -> special error type 
	/// 
	/// </summary>
	public class QaExternalService : QaExternalServiceBase
	{
		private readonly string _parameters;

		public QaExternalService([NotNull] IList<ITable> tables,
		                         string connectionUrl,
		                         string parameters) : base(tables, connectionUrl)
		{
			_parameters = parameters;
		}

		protected override void AddRequestParameters(ExecuteTestRequest request)
		{
			request.Parameter.Add(_parameters);
		}
	}
}
