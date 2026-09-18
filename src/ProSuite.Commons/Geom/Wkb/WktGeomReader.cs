using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom.Wkb
{
	/// <summary>
	/// Reads geometries from their Well-known Text representation, producing the same
	/// <see cref="ProSuite.Commons.Geom"/> types as <see cref="WkbGeomReader"/> does from
	/// Well-known Binary.
	/// </summary>
	/// <remarks>
	/// The input is a <see cref="string"/> rather than a <see cref="System.IO.TextReader"/>: WKT
	/// carries no length or framing, so where a geometry ends is only known once it has been
	/// parsed, and every source of it - a file, a command line argument, a database column - hands
	/// over the whole text anyway.
	/// <para>
	/// Measured and M-aware geometries are recognised by their tag and their ordinates are read,
	/// but building the points is left to <see cref="GeomBuilder"/>, which supports XY and XYZ
	/// only - exactly as on the WKB side, and for the same reason: <see cref="Pnt3D"/> carries no
	/// measure.
	/// </para>
	/// </remarks>
	public class WktGeomReader
	{
		private readonly bool _assumeWktPolygonsClockwise;

		/// <summary>
		/// Initializes a new instance of the <see cref="WktGeomReader"/> class.
		/// </summary>
		/// <param name="assumeWktPolygonsClockwise">Whether the text is expected to wind a
		/// polygon's exterior ring clockwise, against the OGC convention of a counter-clockwise
		/// exterior ring. This decides the direction of the resulting rings only; which ring is
		/// the exterior one is decided by position, the first ring of a polygon, as the
		/// specification says.</param>
		public WktGeomReader(bool assumeWktPolygonsClockwise = false)
		{
			_assumeWktPolygonsClockwise = assumeWktPolygonsClockwise;
		}

		/// <summary>
		/// Reads whichever geometry the text is tagged as. Supports the same set as
		/// <see cref="WkbGeomReader.ReadGeometry"/>; use <see cref="ReadPoint"/> and
		/// <see cref="ReadMultiPolycurve"/> for a point or a linestring.
		/// </summary>
		[CanBeNull]
		public IBoundedXY ReadGeometry([NotNull] string wkt, out WkbGeometryType geometryType)
		{
			var cursor = new Cursor(wkt);
			geometryType = ReadTag(cursor, out Ordinates ordinates);

			IBoundedXY result;

			switch (geometryType)
			{
				case WkbGeometryType.PolyhedralSurface:
				case WkbGeometryType.Tin:
					result = ReadPolyhedronCore(cursor, ordinates) ?? Polyhedron.CreateEmpty();
					break;

				case WkbGeometryType.MultiPoint:
					result = ReadMultipointCore(cursor, ordinates);
					break;

				case WkbGeometryType.MultiPolygon:
					result = new MultiPolycurve(ReadMultiPolygonCore(cursor, ordinates));
					break;

				case WkbGeometryType.MultiLineString:
					result = ReadMultiPolycurveCore(cursor, geometryType, ordinates);
					break;

				case WkbGeometryType.Polygon:
					result = ReadPolygonCore(cursor, ordinates);
					break;

				default:
					throw new NotImplementedException(
						$"Geometry type {geometryType} not yet supported for Wkt deserialization");
			}

			cursor.ExpectEnd();

			return result;
		}

		[CanBeNull]
		public IPnt ReadPoint([NotNull] string wkt)
		{
			var cursor = new Cursor(wkt);
			WkbGeometryType geometryType = ReadTag(cursor, out Ordinates ordinates);

			if (geometryType != WkbGeometryType.Point)
			{
				throw new NotSupportedException($"Cannot read {geometryType} as point.");
			}

			IPnt result = cursor.TryTakeEmpty()
				              ? null
				              : ReadParenthesizedPoint(cursor, ordinates);

			cursor.ExpectEnd();

			return result;
		}

		[NotNull]
		public Multipoint<IPnt> ReadMultiPoint([NotNull] string wkt)
		{
			var cursor = new Cursor(wkt);
			WkbGeometryType geometryType = ReadTag(cursor, out Ordinates ordinates);

			if (geometryType != WkbGeometryType.MultiPoint)
			{
				throw new NotSupportedException($"Cannot read {geometryType} as multipoint.");
			}

			Multipoint<IPnt> result = ReadMultipointCore(cursor, ordinates);

			cursor.ExpectEnd();

			return result;
		}

		/// <summary>
		/// Reads a <c>LINESTRING</c> or a <c>MULTILINESTRING</c>.
		/// </summary>
		[NotNull]
		public MultiPolycurve ReadMultiPolycurve([NotNull] string wkt)
		{
			var cursor = new Cursor(wkt);
			WkbGeometryType geometryType = ReadTag(cursor, out Ordinates ordinates);

			if (geometryType != WkbGeometryType.LineString &&
			    geometryType != WkbGeometryType.MultiLineString)
			{
				throw new NotSupportedException(
					$"Cannot read {geometryType} as multi-polycurve.");
			}

			MultiPolycurve result = ReadMultiPolycurveCore(cursor, geometryType, ordinates);

			cursor.ExpectEnd();

			return result;
		}

		/// <summary>
		/// Reads a <c>POLYGON</c>. Returns null for <c>POLYGON EMPTY</c>, as
		/// <see cref="WkbGeomReader.ReadPolygon"/> does for a polygon with no rings.
		/// </summary>
		[CanBeNull]
		public RingGroup ReadPolygon([NotNull] string wkt)
		{
			var cursor = new Cursor(wkt);
			WkbGeometryType geometryType = ReadTag(cursor, out Ordinates ordinates);

			if (geometryType != WkbGeometryType.Polygon)
			{
				throw new NotSupportedException($"Cannot read {geometryType} as Polygon.");
			}

			RingGroup result = ReadPolygonCore(cursor, ordinates);

			cursor.ExpectEnd();

			return result;
		}

		/// <summary>
		/// Reads a <c>MULTIPOLYGON</c> or a single <c>POLYGON</c>, as
		/// <see cref="WkbGeomReader.ReadMultiPolygon"/> does.
		/// </summary>
		[NotNull]
		public IList<RingGroup> ReadMultiPolygon([NotNull] string wkt)
		{
			var cursor = new Cursor(wkt);
			WkbGeometryType geometryType = ReadTag(cursor, out Ordinates ordinates);

			IList<RingGroup> result;

			if (geometryType == WkbGeometryType.Polygon)
			{
				RingGroup ringGroup = ReadPolygonCore(cursor, ordinates);

				result = ringGroup == null
					         ? new List<RingGroup>()
					         : new List<RingGroup> { ringGroup };
			}
			else if (geometryType == WkbGeometryType.MultiPolygon)
			{
				result = ReadMultiPolygonCore(cursor, ordinates);
			}
			else
			{
				throw new NotSupportedException(
					$"Cannot read {geometryType} as MultiPolygon.");
			}

			cursor.ExpectEnd();

			return result;
		}

		/// <summary>
		/// Reads a <c>POLYHEDRALSURFACE</c> or a <c>TIN</c>.
		/// </summary>
		[NotNull]
		public IList<Polyhedron> ReadMultiSurface([NotNull] string wkt)
		{
			var cursor = new Cursor(wkt);
			WkbGeometryType geometryType = ReadTag(cursor, out Ordinates ordinates);

			if (geometryType != WkbGeometryType.PolyhedralSurface &&
			    geometryType != WkbGeometryType.Tin)
			{
				throw new NotSupportedException(
					$"Cannot read {geometryType} as multi-surface.");
			}

			Polyhedron polyhedron = ReadPolyhedronCore(cursor, ordinates);

			cursor.ExpectEnd();

			return polyhedron == null
				       ? new List<Polyhedron>()
				       : new List<Polyhedron> { polyhedron };
		}

		#region Geometry bodies

		[NotNull]
		private Multipoint<IPnt> ReadMultipointCore([NotNull] Cursor cursor, Ordinates ordinates)
		{
			var builder = new GeomBuilder(false);

			if (cursor.TryTakeEmpty())
			{
				return builder.CreateMultipoint(new List<IPnt>(), 0);
			}

			IPointFactory<IPnt> pointFactory = builder.GetPointFactory(ordinates);
			var points = new List<IPnt>();

			cursor.Expect('(');
			do
			{
				// Both spellings are in use: MULTIPOINT (1 2, 3 4) and MULTIPOINT ((1 2), (3 4)).
				points.Add(cursor.Peek() == '('
					           ? ReadParenthesizedPoint(cursor, ordinates, pointFactory)
					           : ReadPoint(cursor, ordinates, pointFactory));
			} while (cursor.TryTake(','));

			cursor.Expect(')');

			return builder.CreateMultipoint(points, points.Count);
		}

		[NotNull]
		private MultiPolycurve ReadMultiPolycurveCore([NotNull] Cursor cursor,
		                                              WkbGeometryType geometryType,
		                                              Ordinates ordinates)
		{
			// A linestring has no inside, so its direction is the text's own and is never turned.
			var builder = new GeomBuilder(false);

			if (cursor.TryTakeEmpty())
			{
				return new MultiPolycurve(new List<Linestring>());
			}

			var linestrings = new List<Linestring>();

			if (geometryType == WkbGeometryType.LineString)
			{
				linestrings.Add(ReadLinestring(cursor, ordinates, builder));
			}
			else
			{
				cursor.Expect('(');
				do
				{
					linestrings.Add(ReadLinestring(cursor, ordinates, builder));
				} while (cursor.TryTake(','));

				cursor.Expect(')');
			}

			return new MultiPolycurve(linestrings);
		}

		[CanBeNull]
		private RingGroup ReadPolygonCore([NotNull] Cursor cursor, Ordinates ordinates)
		{
			if (cursor.TryTakeEmpty())
			{
				return null;
			}

			// Which ring is the exterior one is decided by position, not by winding; the flag only
			// says which way the text winds them, and hence whether they must be turned around.
			var builder = new GeomBuilder(! _assumeWktPolygonsClockwise);

			var rings = new List<Linestring>();

			cursor.Expect('(');
			do
			{
				rings.Add(ReadLinestring(cursor, ordinates, builder));
			} while (cursor.TryTake(','));

			cursor.Expect(')');

			if (rings.Count == 0)
			{
				return null;
			}

			return new RingGroup(rings[0], rings.GetRange(1, rings.Count - 1));
		}

		[NotNull]
		private List<RingGroup> ReadMultiPolygonCore([NotNull] Cursor cursor, Ordinates ordinates)
		{
			var result = new List<RingGroup>();

			if (cursor.TryTakeEmpty())
			{
				return result;
			}

			cursor.Expect('(');
			do
			{
				RingGroup ringGroup = ReadPolygonCore(cursor, ordinates);

				if (ringGroup != null)
				{
					result.Add(ringGroup);
				}
			} while (cursor.TryTake(','));

			cursor.Expect(')');

			return result;
		}

		/// <summary>
		/// The body of a <c>POLYHEDRALSURFACE</c> or a <c>TIN</c>, which is a list of polygons.
		/// </summary>
		[CanBeNull]
		private Polyhedron ReadPolyhedronCore([NotNull] Cursor cursor, Ordinates ordinates)
		{
			List<RingGroup> ringGroups = ReadMultiPolygonCore(cursor, ordinates);

			return ringGroups.Count == 0 ? null : new Polyhedron(ringGroups);
		}

		[NotNull]
		private static Linestring ReadLinestring([NotNull] Cursor cursor, Ordinates ordinates,
		                                         [NotNull] GeomBuilder builder)
		{
			if (cursor.TryTakeEmpty())
			{
				return builder.CreateLinestring(new List<IPnt>(), 0);
			}

			IPointFactory<IPnt> pointFactory = builder.GetPointFactory(ordinates);
			var points = new List<IPnt>();

			cursor.Expect('(');
			do
			{
				points.Add(ReadPoint(cursor, ordinates, pointFactory));
			} while (cursor.TryTake(','));

			cursor.Expect(')');

			return builder.CreateLinestring(points, points.Count);
		}

		#endregion

		#region Points and tags

		[NotNull]
		private static IPnt ReadParenthesizedPoint([NotNull] Cursor cursor, Ordinates ordinates,
		                                           [CanBeNull] IPointFactory<IPnt> pointFactory =
			                                           null)
		{
			pointFactory = pointFactory ?? new GeomBuilder(false).GetPointFactory(ordinates);

			cursor.Expect('(');
			IPnt point = ReadPoint(cursor, ordinates, pointFactory);
			cursor.Expect(')');

			return point;
		}

		[NotNull]
		private static IPnt ReadPoint([NotNull] Cursor cursor, Ordinates ordinates,
		                              [NotNull] IPointFactory<IPnt> pointFactory)
		{
			double x = cursor.ReadNumber();
			double y = cursor.ReadNumber();

			switch (ordinates)
			{
				case Ordinates.Xy:
					return pointFactory.CreatePointXy(x, y);

				case Ordinates.Xyz:
					return pointFactory.CreatePointXyz(x, y, cursor.ReadNumber());

				case Ordinates.Xym:
					return pointFactory.CreatePointXym(x, y, cursor.ReadNumber());

				case Ordinates.Xyzm:
					return pointFactory.CreatePointXyzm(x, y, cursor.ReadNumber(),
					                                    cursor.ReadNumber());

				default:
					throw new NotSupportedException($"Unsupported ordinates type: {ordinates}");
			}
		}

		/// <summary>
		/// Reads the leading type tag and the dimensionality that follows it. An untagged body
		/// with three ordinates per point is XYZ, which is how most producers write it.
		/// </summary>
		private static WkbGeometryType ReadTag([NotNull] Cursor cursor, out Ordinates ordinates)
		{
			string tag = cursor.ReadWord();

			WkbGeometryType geometryType;

			switch (tag.ToUpperInvariant())
			{
				case "POINT":
					geometryType = WkbGeometryType.Point;
					break;
				case "LINESTRING":
					geometryType = WkbGeometryType.LineString;
					break;
				case "POLYGON":
					geometryType = WkbGeometryType.Polygon;
					break;
				case "MULTIPOINT":
					geometryType = WkbGeometryType.MultiPoint;
					break;
				case "MULTILINESTRING":
					geometryType = WkbGeometryType.MultiLineString;
					break;
				case "MULTIPOLYGON":
					geometryType = WkbGeometryType.MultiPolygon;
					break;
				case "POLYHEDRALSURFACE":
					geometryType = WkbGeometryType.PolyhedralSurface;
					break;
				case "TIN":
					geometryType = WkbGeometryType.Tin;
					break;
				case "TRIANGLE":
					geometryType = WkbGeometryType.Triangle;
					break;
				default:
					throw cursor.Fail($"'{tag}' is not a known geometry type");
			}

			ordinates = ReadOrdinates(cursor);

			return geometryType;
		}

		private static Ordinates ReadOrdinates([NotNull] Cursor cursor)
		{
			string dimension = cursor.PeekWord();

			switch (dimension.ToUpperInvariant())
			{
				case "Z":
					cursor.ReadWord();
					return Ordinates.Xyz;
				case "M":
					cursor.ReadWord();
					return Ordinates.Xym;
				case "ZM":
					cursor.ReadWord();
					return Ordinates.Xyzm;
				default:
					// Untagged: the ordinate count of the first point decides.
					return cursor.CountOrdinatesOfFirstPoint() == 2
						       ? Ordinates.Xy
						       : Ordinates.Xyz;
			}
		}

		#endregion

		/// <summary>
		/// A position in the text, and the reading primitives the grammar is expressed in. Every
		/// failure says where in the text it gave up.
		/// </summary>
		private sealed class Cursor
		{
			private readonly string _text;
			private int _position;

			public Cursor([NotNull] string text)
			{
				_text = text ?? throw new ArgumentNullException(nameof(text));
			}

			public char Peek()
			{
				SkipWhitespace();

				return _position < _text.Length ? _text[_position] : '\0';
			}

			public void Expect(char expected)
			{
				if (Peek() != expected)
				{
					throw Fail($"expected '{expected}'");
				}

				_position++;
			}

			public bool TryTake(char candidate)
			{
				if (Peek() != candidate)
				{
					return false;
				}

				_position++;

				return true;
			}

			/// <summary>
			/// Consumes the <c>EMPTY</c> keyword if that is what comes next.
			/// </summary>
			public bool TryTakeEmpty()
			{
				int before = _position;

				if (string.Equals(PeekWord(), "EMPTY", StringComparison.OrdinalIgnoreCase))
				{
					ReadWord();

					return true;
				}

				_position = before;

				return false;
			}

			[NotNull]
			public string ReadWord()
			{
				SkipWhitespace();

				int start = _position;
				while (_position < _text.Length && char.IsLetter(_text[_position]))
				{
					_position++;
				}

				if (_position == start)
				{
					throw Fail("expected a word");
				}

				return _text.Substring(start, _position - start);
			}

			[NotNull]
			public string PeekWord()
			{
				int before = _position;
				SkipWhitespace();

				int start = _position;
				while (_position < _text.Length && char.IsLetter(_text[_position]))
				{
					_position++;
				}

				string word = _text.Substring(start, _position - start);
				_position = before;

				return word;
			}

			public double ReadNumber()
			{
				SkipWhitespace();

				int start = _position;

				if (_position < _text.Length &&
				    (_text[_position] == '+' || _text[_position] == '-'))
				{
					_position++;
				}

				while (_position < _text.Length && IsNumberChar(_text[_position]))
				{
					// An exponent's sign belongs to the number, a coordinate separator's does not.
					if ((_text[_position] == 'e' || _text[_position] == 'E') &&
					    _position + 1 < _text.Length &&
					    (_text[_position + 1] == '+' || _text[_position + 1] == '-'))
					{
						_position++;
					}

					_position++;
				}

				string token = _text.Substring(start, _position - start);

				if (! double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture,
				                      out double value))
				{
					_position = start;

					throw Fail("expected a number");
				}

				return value;
			}

			/// <summary>
			/// How many ordinates the next point carries, without consuming anything. Used to tell
			/// an untagged XY body from an untagged XYZ one.
			/// </summary>
			public int CountOrdinatesOfFirstPoint()
			{
				int before = _position;

				try
				{
					// Down to the first number, through however many opening brackets there are.
					while (Peek() == '(')
					{
						_position++;
					}

					var count = 0;
					while (true)
					{
						SkipWhitespace();

						if (_position >= _text.Length || ! IsNumberStart(_text[_position]))
						{
							return count;
						}

						ReadNumber();
						count++;
					}
				}
				catch (FormatException)
				{
					// Not a coordinate list at all: let the real parse produce the message.
					return 2;
				}
				finally
				{
					_position = before;
				}
			}

			public void ExpectEnd()
			{
				SkipWhitespace();

				if (_position < _text.Length)
				{
					throw Fail("expected the end of the text");
				}
			}

			[NotNull]
			public FormatException Fail([NotNull] string what)
			{
				return new FormatException(
					$"Invalid WKT at position {_position}: {what}, but found " +
					$"'{Remainder()}'.");
			}

			[NotNull]
			private string Remainder()
			{
				if (_position >= _text.Length)
				{
					return "the end of the text";
				}

				const int shown = 20;
				int length = Math.Min(shown, _text.Length - _position);

				return new StringBuilder(_text.Substring(_position, length))
				       .Append(_position + length < _text.Length ? "..." : string.Empty)
				       .ToString();
			}

			private void SkipWhitespace()
			{
				while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
				{
					_position++;
				}
			}

			private static bool IsNumberStart(char c)
			{
				return char.IsDigit(c) || c == '+' || c == '-' || c == '.';
			}

			private static bool IsNumberChar(char c)
			{
				return char.IsDigit(c) || c == '.' || c == 'e' || c == 'E';
			}
		}
	}
}
