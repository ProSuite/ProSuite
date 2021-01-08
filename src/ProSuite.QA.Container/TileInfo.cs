using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public class TileInfo
	{
		private IEnvelope _processedEnvelope;

		[CLSCompliant(false)]
		public TileInfo(TileState state,
		                [CanBeNull] IEnvelope currentEnvelope,
		                [CanBeNull] IEnvelope allBox)
		{
			State = state;
			CurrentEnvelope = currentEnvelope;
			AllBox = allBox;
		}

		[CanBeNull]
		[CLSCompliant(false)]
		public IEnvelope CurrentEnvelope { get; }

		[CanBeNull]
		[CLSCompliant(false)]
		public IEnvelope AllBox { get; }

		public TileState State { get; }

		[CLSCompliant(false)]
		[CanBeNull]
		public IEnvelope ProcessedEnvelope
		{
			get
			{
				if (_processedEnvelope == null && State == TileState.Progressing &&
				    AllBox != null)
				{
					var currentEnvelope = Assert.NotNull(CurrentEnvelope);

					_processedEnvelope = new EnvelopeClass();
					_processedEnvelope.PutCoords(AllBox.XMin, AllBox.YMin,
					                             currentEnvelope.XMax,
					                             currentEnvelope.YMax);
				}

				return _processedEnvelope;
			}
		}
	}
}
