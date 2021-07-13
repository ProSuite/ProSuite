using System.Globalization;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Microservices.Definitions.QA.Test;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
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
			request.Parameter.Add(_layerFile);
			request.Parameter.Add(_conflictLayerFile);
			request.Parameter.Add(_conflictDistance);
			request.Parameter.Add(_lineConnectionAllowance);
			request.Parameter.Add(_referenceScale.ToString(CultureInfo.InvariantCulture));
		}
	}
}
