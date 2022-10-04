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

		// for Standalone messages -> remove once Standalone Verification provides proper events
		public VerificationProgressEventArgs(string message)
		{
			ProgressType = VerificationProgressType.Undefined;
			Tag = message;
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

		public IEnvelope CurrentBox { get; private set; }

		[CanBeNull]
		public IEnvelope TotalBox { get; }

		public object Tag { get; set; }

		public IGeometry ErrorGeometry { get; }

		public VerificationProgressType ProgressType { get; }

		public Step ProgressStep { get; }

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
