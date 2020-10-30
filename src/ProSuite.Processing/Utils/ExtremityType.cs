using System;

namespace ProSuite.Processing.Utils
{
	/// <remarks>
	/// The values must agree with what MarkerPlacementAtExtremities
	/// expects for its esriGAAtExtremitiesType parameter.
	/// </remarks>
	public enum ExtremityType
	{
		Both = 0,
		JustBegin = 1,
		JustEnd = 2,
		None = 3
	}

	public static class ExtremityTypeExtensions
	{
		private const string InvalidExtremityTypeMessage = "invalid extremity type";

		public static ExtremityType? SetBegin(this ExtremityType? value, bool flag)
		{
			return value?.SetBegin(flag);
		}

		public static ExtremityType SetBegin(this ExtremityType value, bool flag)
		{
			switch (value)
			{
				case ExtremityType.Both:
					return flag ? ExtremityType.Both : ExtremityType.JustEnd;
				case ExtremityType.JustBegin:
					return flag ? ExtremityType.JustBegin : ExtremityType.None;
				case ExtremityType.JustEnd:
					return flag ? ExtremityType.Both : ExtremityType.JustEnd;
				case ExtremityType.None:
					return flag ? ExtremityType.JustBegin : ExtremityType.None;
				default:
					throw new ArgumentOutOfRangeException(nameof(value), value,
					                                      InvalidExtremityTypeMessage);
				// TODO return value instead?
			}
		}

		public static ExtremityType? SetEnd(this ExtremityType? value, bool flag)
		{
			return value?.SetEnd(flag);
		}

		public static ExtremityType SetEnd(this ExtremityType value, bool flag)
		{
			switch (value)
			{
				case ExtremityType.Both:
					return flag ? ExtremityType.Both : ExtremityType.JustBegin;
				case ExtremityType.JustBegin:
					return flag ? ExtremityType.Both : ExtremityType.JustBegin;
				case ExtremityType.JustEnd:
					return flag ? ExtremityType.JustEnd : ExtremityType.None;
				case ExtremityType.None:
					return flag ? ExtremityType.JustEnd : ExtremityType.None;
				default:
					throw new ArgumentOutOfRangeException(nameof(value), value,
					                                      InvalidExtremityTypeMessage);
				// TODO return value instead?
			}
		}

		public static ExtremityType? Invert(this ExtremityType? value)
		{
			return value?.Invert();
		}

		public static ExtremityType Invert(this ExtremityType value)
		{
			switch (value)
			{
				case ExtremityType.Both:
					return ExtremityType.None;
				case ExtremityType.JustBegin:
					return ExtremityType.JustEnd;
				case ExtremityType.JustEnd:
					return ExtremityType.JustBegin;
				case ExtremityType.None:
					return ExtremityType.Both;
				default:
					throw new ArgumentOutOfRangeException(nameof(value), value,
					                                      InvalidExtremityTypeMessage);
				// TODO return value instead?
			}
		}

		/// <summary>
		/// This is the same as <see cref="Recompute(ExtremityType,bool,bool,bool,bool)"/>
		/// but can cope with a current <paramref name="value"/> of <c>null</c>: if either
		/// or both ends are being processed, assume Both (the default value) instead of
		/// <c>null</c>; otherwise return <paramref name="value"/> unmodified.
		/// </summary>
		public static ExtremityType? Recompute(this ExtremityType? value,
		                                       bool processFrom, bool processTo,
		                                       bool matchFrom, bool matchTo)
		{
			if (processFrom || processTo)
			{
				ExtremityType extremity = value ?? default(ExtremityType);
				return Recompute(extremity, processFrom, processTo, matchFrom, matchTo);
			}

			return value;
		}

		/// <summary>
		/// Recompute the extremity type from its current <paramref name="value"/>
		/// and the given environmental flags: <paramref name="matchFrom"/> and
		/// <paramref name="matchTo"/> control the Begin and End portion of the
		/// extremity type, but only take effect if the corresponding
		/// <paramref name="processFrom"/> or <paramref name="processTo"/> flag
		/// is set.
		/// </summary>
		/// <returns>The computed new extremity type.</returns>
		public static ExtremityType Recompute(this ExtremityType value,
		                                      bool processFrom, bool processTo,
		                                      bool matchFrom, bool matchTo)
		{
			if (processFrom)
			{
				value = value.SetBegin(matchFrom);
			}

			if (processTo)
			{
				value = value.SetEnd(matchTo);
			}

			return value;
		}
	}
}
