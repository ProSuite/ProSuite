using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.DomainModel.AO.QA
{
	public class VerificationProgressEventArgs : EventArgs
	{
		public VerificationProgressEventArgs(VerificationProgressType type,
		                                     int current, int total)
		{
			ProgressType = type;
			Current = current;
			Total = total;
		}

		// for errors
		[CLSCompliant(false)]
		public VerificationProgressEventArgs(VerificationProgressType type,
		                                     int current, IGeometry errorGeometry,
		                                     bool isAllowable)
		{
			ProgressType = type;

			Current = current;
			ErrorGeometry = errorGeometry;

			Tag = isAllowable;
			Total = -1;
		}

		public VerificationProgressEventArgs(
			VerificationProgressType type, ProgressArgs args)
		{
			ProgressType = type;
			ProgressStep = args.CurrentStep;

			Current = args.Current;
			Total = args.Total;
			Tag = args.Tag;
		}

		public VerificationProgressEventArgs(VerificationProgressType type,
		                                     Step step, int current, int total)
		{
			ProgressType = type;
			ProgressStep = step;

			Current = current;
			Total = total;
		}

		[CLSCompliant(false)]
		public VerificationProgressEventArgs(Step step, int currentTile, int totalTile,
		                                     IEnvelope currentBox,
		                                     [CanBeNull] IEnvelope totalBox)
		{
			Current = currentTile;
			Total = totalTile;
			CurrentBox = currentBox;
			TotalBox = totalBox;

			ProgressType = VerificationProgressType.ProcessContainer;
			ProgressStep = step;
		}

		public int Current { get; }

		public int Total { get; }

		[CLSCompliant(false)]
		public IEnvelope CurrentBox { get; private set; }

		[CLSCompliant(false)]
		[CanBeNull]
		public IEnvelope TotalBox { get; }

		public object Tag { get; set; }

		[CLSCompliant(false)]
		public IGeometry ErrorGeometry { get; }

		public VerificationProgressType ProgressType { get; }

		public Step ProgressStep { get; }

		[CLSCompliant(false)]
		public void SetSpatialReference(ISpatialReference spatialReference)
		{
			if (CurrentBox == null)
			{
				return;
			}

			IEnvelope srBox;
			GeometryUtils.EnsureSpatialReference(
				CurrentBox, spatialReference, true, out srBox);
			CurrentBox = srBox;
		}
	}
}