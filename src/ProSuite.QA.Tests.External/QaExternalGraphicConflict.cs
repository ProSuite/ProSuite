using System.Globalization;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Microservices.Definitions.QA.Test;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.External
{
	public class QaExternalGraphicConflict : QaExternalServiceBase
	{
		private readonly string _layerFile;
		private readonly string _conflictLayerFile;
		private readonly string _conflictDistance;
		private readonly string _lineConnectionAllowance;
		private readonly double _referenceScale;

		[Doc(nameof(DocStrings.QaGraphicConflict_0))]
		public QaExternalGraphicConflict(
			[Doc(nameof(DocStrings.QaGraphicConflict_featureClass))]
			IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaGraphicConflict_layerRepresentation))]
			string layerFile,
			[Doc(nameof(DocStrings.QaGraphicConflict_conflictClass))]
			IFeatureClass conflictClass,
			[Doc(nameof(DocStrings.QaGraphicConflict_conflictLayer))]
			string conflictLayerFile,
			[Doc(nameof(DocStrings.QaGraphicConflict_conflictDistance))]
			string conflictDistance,
			[Doc(nameof(DocStrings.QaGraphicConflict_lineConnectionAllowance))]
			string lineConnectionAllowance,
			[Doc(nameof(DocStrings.QaGraphicConflict_referenceScale))]
			double referenceScale,
			string connectionUrl)
			: base(new[] {(ITable) featureClass, (ITable) conflictClass},
			       connectionUrl)
		{
			_layerFile = layerFile;
			_conflictLayerFile = conflictLayerFile;
			_conflictDistance = conflictDistance;
			_lineConnectionAllowance = lineConnectionAllowance;
			_referenceScale = referenceScale;
		}

		protected override void AddRequestParameters(ExecuteTestRequest request)
		{
			request.Parameters.Add(_layerFile);
			request.Parameters.Add(_conflictLayerFile);
			request.Parameters.Add(_conflictDistance);
			request.Parameters.Add(_lineConnectionAllowance);
			request.Parameters.Add(_referenceScale.ToString(CultureInfo.InvariantCulture));
		}
	}
}
