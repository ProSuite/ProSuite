using System;
using System.ComponentModel;
using System.Globalization;

namespace ProSuite.Processing.Domain
{
	[TypeConverter(typeof(MaskOutlineTypeConverter))]
	public enum MaskOutlineType
	{
		//Exact, ExactSimplified, ConvexHull, BoundingBox
		SymbolExact, SymbolConvexHull, SymbolBoundingBox, ShapeBuffer
	}

	// Old FeatureMasks OutlineMethod: ShapeBuffer, RepresentationBuffer, RepresentationOffset
	// Old AnnoMasks OutlineMethod: AnnoExact, AnnoConvexHull, AnnoBox

	public class MaskOutlineTypeConverter : TypeConverter
	{
		public override object ConvertFrom(ITypeDescriptorContext context,
		                                   CultureInfo culture, object value)
		{
			if (value is MaskOutlineType maskOutlineType)
			{
				return maskOutlineType;
			}

			var text = Convert.ToString(value);
			if (string.IsNullOrEmpty(text))
			{
				return default;
			}

			if (Enum.TryParse(text, true, out MaskOutlineType result))
			{
				return result;
			}

			switch (text.ToLowerInvariant())
			{
				case @"exact":
				case @"annoexact":
					return MaskOutlineType.SymbolExact;
				case @"convexhull":
				case @"annoconvexhull":
					return MaskOutlineType.SymbolConvexHull;
				case @"boundingbox":
				case @"annobox":
					return MaskOutlineType.SymbolBoundingBox;
				case @"representationbuffer":
				case @"representationoffset":
					return MaskOutlineType.SymbolExact;
			}

			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context,
		                                 CultureInfo culture, object value,
		                                 Type destinationType)
		{
			if (destinationType == typeof(string))
			{
				return value.ToString();
			}

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}
