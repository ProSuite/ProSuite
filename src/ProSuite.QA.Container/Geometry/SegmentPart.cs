using System;
using System.Collections.Generic;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.Geometry
{
	public class SegmentPart
	{
		public static bool VerifyComplete([NotNull] List<SegmentPart> coincidentParts)
		{
			coincidentParts.Sort(new SegmentPartComparer());
			double min = 0;
			foreach (SegmentPart coincidentPart in coincidentParts)
			{
				if (coincidentPart.MinFraction <= min)
				{
					min = Math.Max(min, coincidentPart.MaxFraction);
				}

				if (min >= 1)
				{
					return true;
				}
			}

			return false;
		}

		[CanBeNull] private readonly ISegmentProxy _segmentProxy;
		private readonly int _partIndex;
		private readonly int _segmentIndex;
		private bool _complete;

		private double _maxFraction;
		private double _minFraction;
		private bool _partiell;

		private SegmentProxy _nearSelf;

		[CLSCompliant(false)]
		public SegmentPart([NotNull] ISegmentProxy segmentProxy,
		                   double minFraction,
		                   double maxFraction,
		                   bool complete)
			: this(
				segmentProxy.PartIndex, segmentProxy.SegmentIndex, minFraction, maxFraction,
				complete)
		{
			_segmentProxy = segmentProxy;
		}

		public SegmentPart(int partIndex, int segmentIndex,
		                   double minFraction, double maxFraction,
		                   bool complete)
		{
			Assert.ArgumentCondition(minFraction <= maxFraction, "minFraction > maxFraction");

			_partIndex = partIndex;
			_segmentIndex = segmentIndex;

			_minFraction = Math.Max(minFraction, 0);
			_maxFraction = Math.Min(maxFraction, 1);

			Complete = complete;
		}

		[CLSCompliant(false)]
		[CanBeNull]
		public ISegmentProxy SegmentProxy
		{
			get { return _segmentProxy; }
		}

		public int PartIndex
		{
			get { return _partIndex; }
		}

		public int SegmentIndex
		{
			get { return _segmentIndex; }
		}

		public double MinFraction
		{
			get { return _minFraction; }
			set { _minFraction = Math.Max(0, value); }
		}

		public double FullMin
		{
			get { return MinFraction + SegmentIndex; }
		}

		public double MaxFraction
		{
			get { return _maxFraction; }
			set { _maxFraction = Math.Min(1, value); }
		}

		public double FullMax
		{
			get { return MaxFraction + SegmentIndex; }
		}

		public bool Complete
		{
			get { return _complete; }
			set
			{
				_complete = value;
				if (_complete)
				{
					_minFraction = 0;
					_maxFraction = 1;
				}
			}
		}

		public bool Partiell
		{
			get { return _partiell; }
			set
			{
				_partiell = value;
				if (_partiell)
				{
					_minFraction = 1;
					_maxFraction = 0;
				}
			}
		}

		[CLSCompliant(false)]
		public SegmentProxy NearSelf
		{
			get { return _nearSelf; }
			set { _nearSelf = value; }
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			AppendToString(sb, shortString: false);
			sb.Append(";");
			return sb.ToString();
		}

		public void AppendTo(StringBuilder sb, bool shortString)
		{
			AppendToString(sb, shortString);
		}

		protected virtual void AppendToString(StringBuilder sb, bool shortString)
		{
			if (! shortString)
			{
				sb.AppendFormat("P {0}, S {1}", _partIndex, _segmentIndex);
			}

			if (MinFraction > 0)
			{
				sb.AppendFormat(" [{0:N1} ", MinFraction);
			}
			else
			{
				sb.Append(" [0 ");
			}

			if (MaxFraction < 1)
			{
				sb.AppendFormat("{0:N1}] ", MaxFraction);
			}
			else
			{
				sb.Append("1]");
			}
		}
	}
}
