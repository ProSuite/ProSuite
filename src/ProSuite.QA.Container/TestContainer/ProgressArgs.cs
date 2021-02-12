using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	public class ProgressArgs : EventArgs
	{
		private IEnvelope _processedEnvelope;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ProgressArgs"/> class.
		/// </summary>
		/// <param name="step">The step.</param>
		/// <param name="current">The current.</param>
		/// <param name="total">The total.</param>
		/// <param name="message">The message.</param>
		public ProgressArgs(Step step, int current, int total, string message)
		{
			CurrentStep = step;
			Message = message;

			Current = current;
			Total = total;

			IsInfoOnly = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ProgressArgs"/> class.
		/// </summary>
		/// <param name="step">The step.</param>
		/// <param name="current">The current.</param>
		/// <param name="total">The total.</param>
		/// <param name="tag">The tag.</param>
		public ProgressArgs(Step step, int current, int total, object tag)
		{
			CurrentStep = step;
			Tag = tag;

			Current = current;
			Total = total;

			IsInfoOnly = true;
		}

		public long BruttoTicks { get; set; }
		public long NettoTicks { get; set; }

		public ProgressArgs(Step step, int current, int total, IEnvelope currentEnvelope,
		                    [CanBeNull] IEnvelope allBox)
		{
			CurrentStep = step;

			Current = current;
			Total = total;
			CurrentEnvelope = currentEnvelope;
			AllBox = allBox;

			IsInfoOnly = false;
		}

		#endregion

		public Step CurrentStep { get; }

		public object Tag { get; }

		public bool IsInfoOnly { get; }

		public string Message { get; }

		public int Current { get; }

		public int Total { get; }

		public IEnvelope CurrentEnvelope { get; }

		[CanBeNull]
		public IEnvelope ProcessedEnvelope
		{
			get
			{
				if (_processedEnvelope == null && Current < Total && AllBox != null)
				{
					_processedEnvelope = new EnvelopeClass();
					_processedEnvelope.PutCoords(AllBox.XMin, AllBox.YMin,
					                             CurrentEnvelope.XMax,
					                             CurrentEnvelope.YMax);
				}

				return _processedEnvelope;
			}
		}

		[CanBeNull]
		public IEnvelope AllBox { get; }

		public bool IsFinal
		{
			get { return (Current == Total); }
		}

		public bool IsPreprocess
		{
			get
			{
				if (Current > 0)
				{
					return false;
				}

				if (CurrentEnvelope == null || CurrentEnvelope.Width == 0)
				{
					return true;
				}

				return false;
			}
		}
	}
}
