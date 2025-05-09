using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry
{
	/// <summary>
	/// Utility methods for working with spatial references.
	/// </summary>
	public static class SpatialReferenceUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Fields

		// NOTE: without [ThreadStatic] exceptions may occur in subsequent unit tests
		[ThreadStatic] private static ISpatialReferenceFactory3 _factory;

		#endregion

		/// <summary>
		/// Returns a spatial reference that is guaranteed to be high-precision with equal grid definition, 
		/// based on an input spatial reference. If the input spatial reference is already high-precision, 
		/// it is returned as is, otherwise a high-precision copy of the input spatial reference is returned.
		/// </summary>
		/// <param name="immutableSpatialReference">The immutable spatial reference.</param>
		/// <param name="highPrecisionSpatialReference">The high precision spatial reference.</param>
		/// <returns><c>true</c> if the returned spatial reference is a changed copy, <c>false</c> if it 
		/// is the unchanged input spatial refererence, which was already high-precision.</returns>
		public static bool EnsureHighPrecision(
			[NotNull] ISpatialReference immutableSpatialReference,
			[NotNull] out ISpatialReference highPrecisionSpatialReference)
		{
			Assert.ArgumentNotNull(immutableSpatialReference,
			                       nameof(immutableSpatialReference));

			if (((IControlPrecision2) immutableSpatialReference).IsHighPrecision)
			{
				highPrecisionSpatialReference = immutableSpatialReference;
				return false;
			}

			highPrecisionSpatialReference =
				Factory.ConstructHighPrecisionSpatialReference(
					immutableSpatialReference, 0, 0, 0);
			return true;
		}

		/// <summary>
		/// Creates a spatial reference based on a well known horizontal coordinate system.
		/// </summary>
		/// <param name="cs">The well known horizontal coordinate system.</param>
		/// <param name="setDefaultXyDomain">if set to <c>true</c>, the default xy domain 
		/// is assigned based on the coordinate system horizon</param>
		/// <returns></returns>
		[NotNull]
		public static ISpatialReference CreateSpatialReference(
			WellKnownHorizontalCS cs,
			bool setDefaultXyDomain = false)
		{
			return CreateSpatialReference((int) cs, setDefaultXyDomain);
		}

		/// <summary>
		/// Creates a spatial reference based on a well known horizontal 
		/// and vertical coordinate system.
		/// </summary>
		/// <param name="hcs">The well known horizontal coordinate system.</param>
		/// <param name="vcs">The well known vertical coordinate system.</param>
		/// <returns></returns>
		[NotNull]
		public static ISpatialReference CreateSpatialReference(WellKnownHorizontalCS hcs,
		                                                       WellKnownVerticalCS vcs)
		{
			return CreateSpatialReference((int) hcs, (int) vcs);
		}

		/// <summary>
		/// Creates a vertical coordinate system based on a well known vertical coordinate system.
		/// </summary>
		/// <param name="vcs">The well known vertical coordinate system.</param>
		[NotNull]
		public static IVerticalCoordinateSystem CreateVerticalCoordinateSystem(
			WellKnownVerticalCS vcs)
		{
			return CreateVerticalCoordinateSystem((int) vcs);
		}

		private static readonly object _srLock = new object();

		/// <summary>
		/// Creates a spatial reference based on a factory code.
		/// </summary>
		/// <param name="srId">The spatial reference factory code.</param>
		/// <param name="setDefaultXyDomain">if set to <c>true</c>, the default xy domain 
		/// is assigned based on the coordinate system horizon</param>
		/// <returns></returns>
		[NotNull]
		public static ISpatialReference CreateSpatialReference(int srId,
		                                                       bool setDefaultXyDomain = false)
		{
			ISpatialReference sref;

			// Experimental: Try preventing the following error by serialized access
			//System.ArgumentException: the input is not a geographic or projected coordinate system
			//	at ESRI.ArcGIS.Geometry.ISpatialReferenceFactory3.CreateSpatialReference(Int32 srID)
			// Unfortunately the error is not reproducible in a simple unit test
			// Repro: Run nightly work unit verification for several eligible work units
			//        and it happens in ca. 1 out of 2 cases.
			lock (_srLock)
			{
				sref = Factory.CreateSpatialReference(srId);
			}

			if (setDefaultXyDomain)
			{
				((ISpatialReferenceResolution) sref).ConstructFromHorizon();
			}

			((IControlPrecision2) sref).IsHighPrecision = true;

			return sref;
		}

		/// <summary>
		/// Creates a spatial reference based a spatial reference info string.
		/// </summary>
		/// <param name="spatialReferenceInfo">The spatial reference info string.</param>
		/// <param name="setDefaultXyDomain">if set to <c>true</c>, the default xy domain 
		/// is assigned based on the coordinate system horizon</param>
		/// <returns></returns>
		[NotNull]
		public static ISpatialReference CreateSpatialReference(
			string spatialReferenceInfo,
			bool setDefaultXyDomain)
		{
			ISpatialReference sref;
			Factory.CreateESRISpatialReference(spatialReferenceInfo, out sref, out int _);

			if (setDefaultXyDomain)
			{
				((ISpatialReferenceResolution) sref).ConstructFromHorizon();
			}

			((IControlPrecision2) sref).IsHighPrecision = true;

			return sref;
		}

		/// <summary>
		/// Creates a spatial reference based on factory codes for the
		/// horizontal and vertical coordinate systems
		/// </summary>
		/// <param name="srId">The spatial reference factory code.</param>
		/// <param name="vcsId">The vertical coordinate system factory code.</param>
		/// <param name="setDefaultXyDomain">if set to <c>true</c>, the default xy domain 
		/// is assigned based on the coordinate system horizon</param>
		/// <returns></returns>
		[NotNull]
		public static ISpatialReference CreateSpatialReference(
			int srId, int vcsId,
			bool setDefaultXyDomain = false)
		{
			var sref =
				(ISpatialReference3) CreateSpatialReference(srId, setDefaultXyDomain);

			sref.VerticalCoordinateSystem = CreateVerticalCoordinateSystem(vcsId);

			return sref;
		}

		[NotNull]
		public static ISpatialReference CreateSpatialReferenceWithMinimumTolerance(
			[NotNull] ISpatialReference spatialReference)
		{
			const double resolutionFactor = 1;
			return CreateSpatialReferenceWithMinimumTolerance(spatialReference,
			                                                  resolutionFactor);
		}

		[NotNull]
		public static ISpatialReference CreateSpatialReferenceWithMinimumTolerance(
			[NotNull] ISpatialReference spatialReference,
			double resolutionFactor)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));
			Assert.ArgumentCondition(resolutionFactor >= 1,
			                         "resolution factor must be >= 1");

			var srefTol = spatialReference as ISpatialReferenceTolerance;
			var srefRes = spatialReference as ISpatialReferenceResolution;

			var clone = (ISpatialReference) ((IClone) spatialReference).Clone();

			if (srefTol == null || srefRes == null)
			{
				return clone;
			}

			const bool standardUnits = true;
			double resolution = srefRes.XYResolution[standardUnits];

			if (resolutionFactor > 1)
			{
				resolution = resolution / resolutionFactor;
				((ISpatialReferenceResolution) clone).set_XYResolution(
					standardUnits, resolution);
			}

			((ISpatialReferenceTolerance) clone).XYTolerance = resolution * 2;

			return clone;
		}

		/// <summary>
		/// Creates a vertical coordinate system based on a factory code.
		/// </summary>
		/// <param name="vcsId">The factory code for the vertical coordinate system.</param>
		[NotNull]
		public static IVerticalCoordinateSystem CreateVerticalCoordinateSystem(int vcsId)
		{
			return Factory.CreateVerticalCoordinateSystem(vcsId);
		}

		public static bool AreEqual([NotNull] ISpatialReference sref1,
		                            [NotNull] ISpatialReference sref2,
		                            out bool coordinateSystemDifferent,
		                            out bool verticalCoordinateSystemDifferent,
		                            out bool xyPrecisionDifferent,
		                            out bool zPrecisionDifferent,
		                            out bool mPrecisionDifferent,
		                            out bool xyToleranceDifferent,
		                            out bool zToleranceDifferent,
		                            out bool mToleranceDifferent)
		{
			Assert.ArgumentNotNull(sref1, nameof(sref1));
			Assert.ArgumentNotNull(sref2, nameof(sref2));

			var sref12 = (ISpatialReference2) sref1;
			var sref1Tol = (ISpatialReferenceTolerance) sref1;

			var compare = (ICompareCoordinateSystems) sref1;
			coordinateSystemDifferent = ! compare.IsEqualNoVCS(sref2);
			verticalCoordinateSystemDifferent =
				! AreEqual(GetVerticalCoordinateSystem(sref1),
				           GetVerticalCoordinateSystem(sref2));
			xyPrecisionDifferent = ! sref12.IsXYPrecisionEqual(sref2);
			zPrecisionDifferent = ! sref12.IsZPrecisionEqual(sref2);
			mPrecisionDifferent = ! sref12.IsMPrecisionEqual(sref2);
			xyToleranceDifferent = ! sref1Tol.IsXYToleranceEqual(sref2);
			zToleranceDifferent = ! sref1Tol.IsZToleranceEqual(sref2);
			mToleranceDifferent = ! sref1Tol.IsMToleranceEqual(sref2);

			bool anyDifferent = coordinateSystemDifferent ||
			                    verticalCoordinateSystemDifferent ||
			                    xyPrecisionDifferent ||
			                    zPrecisionDifferent ||
			                    mPrecisionDifferent ||
			                    xyToleranceDifferent ||
			                    mToleranceDifferent ||
			                    zToleranceDifferent;

			return ! anyDifferent;
		}

		[CanBeNull]
		public static IVerticalCoordinateSystem GetVerticalCoordinateSystem(
			[NotNull] ISpatialReference sref)
		{
			Assert.ArgumentNotNull(sref, nameof(sref));

			var sref3 = sref as ISpatialReference3;
			return sref3?.VerticalCoordinateSystem;
		}

		/// <summary>
		/// Returns a value indicating if two spatial references are equal with regard to
		/// their factory codes.
		/// </summary>
		/// <param name="sref1">The first spatial reference.</param>
		/// <param name="sref2">The second spatial reference.</param>
		/// <returns><c>true</c> if the factory codes are equal, <c>false</c> otherwise.</returns>
		public static bool AreEqual([CanBeNull] ISpatialReference sref1,
		                            [CanBeNull] ISpatialReference sref2)
		{
			return AreEqual(sref1, sref2, false, false);
		}

		public static bool AreEqualXY([CanBeNull] ISpatialReference sref1,
		                              [CanBeNull] ISpatialReference sref2,
		                              bool compareTolerance)
		{
			const bool compareXyPrecision = true;
			const bool compareZPrecision = false;
			const bool compareMPrecision = false;
			const bool compareVerticalCoordinateSystems = false;

			return AreEqual(sref1, sref2,
			                compareXyPrecision, compareZPrecision,
			                compareMPrecision, compareTolerance,
			                compareVerticalCoordinateSystems);
		}

		public static bool AreEqualXYZ([CanBeNull] ISpatialReference sref1,
		                               [CanBeNull] ISpatialReference sref2,
		                               bool compareTolerances)
		{
			const bool compareXyPrecision = true;
			const bool compareZPrecision = true;
			const bool compareMPrecision = false;
			const bool compareVerticalCoordinateSystems = true;

			return AreEqual(sref1, sref2,
			                compareXyPrecision, compareZPrecision,
			                compareMPrecision, compareTolerances,
			                compareVerticalCoordinateSystems);
		}

		public static bool AreEqualXYM([CanBeNull] ISpatialReference sref1,
		                               [CanBeNull] ISpatialReference sref2,
		                               bool compareTolerances)
		{
			const bool compareXyPrecision = true;
			const bool compareZPrecision = false;
			const bool compareMPrecision = true;
			const bool compareVerticalCoordinateSystems = false;

			return AreEqual(sref1, sref2,
			                compareXyPrecision, compareZPrecision,
			                compareMPrecision, compareTolerances,
			                compareVerticalCoordinateSystems);
		}

		public static bool AreEqualXYZM([CanBeNull] ISpatialReference sref1,
		                                [CanBeNull] ISpatialReference sref2,
		                                bool compareTolerances)
		{
			const bool compareXyPrecision = true;
			const bool compareZPrecision = true;
			const bool compareMPrecision = true;
			const bool compareVerticalCoordinateSystems = true;

			return AreEqual(sref1, sref2, compareXyPrecision, compareZPrecision,
			                compareMPrecision, compareTolerances,
			                compareVerticalCoordinateSystems);
		}

		public static bool AreEqual([CanBeNull] ISpatialReference sref1,
		                            [CanBeNull] ISpatialReference sref2,
		                            bool compareXYPrecision,
		                            bool compareZPrecision,
		                            bool compareMPrecision,
		                            bool compareTolerances,
		                            bool compareVerticalCoordinateSystems)
		{
			if (sref1 == null && sref2 == null)
			{
				// both null -> equal
				return true;
			}

			if (sref1 == null || sref2 == null)
			{
				// null / not null combination -> not equal
				return false;
			}

			// both not null -> check
			if (compareVerticalCoordinateSystems)
			{
				if (! ((IClone) sref1).IsEqual((IClone) sref2))
				{
					// TODO TEST VCS COMPARISON
					// coordinate system different (also if only vcs is different)
					return false;
				}
			}
			else
			{
				if (! ((ICompareCoordinateSystems) sref1).IsEqualNoVCS(sref2))
				{
					// coordinate system different (ignoring precision/tolerance) -> not equal
					return false;
				}
			}

			var sref12 = (ISpatialReference2) sref1;
			var sref1Tol = (ISpatialReferenceTolerance) sref1;

			if (compareXYPrecision && compareZPrecision && compareMPrecision &&
			    compareTolerances)
			{
				// NOTE contrary to the documentation, this does NOT compare tolerances (at least Z tol)
				bool isPrecisionEqual;
				sref1.IsPrecisionEqual(sref2, out isPrecisionEqual);
				if (! isPrecisionEqual)
				{
					return false;
				}
			}

			if (compareXYPrecision)
			{
				// if the precision grid is compatible, this returns true (also if domains are different)
				if (! sref12.IsXYPrecisionEqual(sref2))
				{
					return false;
				}

				if (compareTolerances && ! sref1Tol.IsXYToleranceEqual(sref2))
				{
					return false;
				}
			}

			if (compareZPrecision)
			{
				if (! sref12.IsZPrecisionEqual(sref2))
				{
					return false;
				}

				if (compareTolerances && ! sref1Tol.IsZToleranceEqual(sref2))
				{
					return false;
				}
			}

			if (compareMPrecision)
			{
				if (! sref12.IsMPrecisionEqual(sref2))
				{
					return false;
				}

				if (compareTolerances && ! sref1Tol.IsMToleranceEqual(sref2))
				{
					return false;
				}
			}

			// phew. all equal.
			return true;
		}

		/// <summary>
		/// Returns a value indicating if two spatial references are equal with regard to
		/// their factory codes and optionally also the spatial domain properties and 
		/// vertical coordinate systems.
		/// </summary>
		/// <param name="sref1">The first spatial reference.</param>
		/// <param name="sref2">The second spatial reference.</param>
		/// <param name="comparePrecisionAndTolerance">Indicates if precision and tolerance 
		/// values should be compared also.</param>
		/// <param name="compareVerticalCoordinateSystems">Indicates if the vertical coordinate 
		/// systems of the spatial references should be compared also.</param>
		/// <returns><c>true</c> if the spatial references are equal, <c>false</c> otherwise.</returns>
		/// <remarks>M precision/tolerance not yet properly dealt with</remarks>
		public static bool AreEqual([CanBeNull] ISpatialReference sref1,
		                            [CanBeNull] ISpatialReference sref2,
		                            bool comparePrecisionAndTolerance,
		                            bool compareVerticalCoordinateSystems)
		{
			// TODO add support for comparing M settings

			if (sref1 == null && sref2 == null)
			{
				// both null -> equal
				return true;
			}

			if (sref1 == null || sref2 == null)
			{
				// null / not null combination -> not equal
				return false;
			}

			if (ReferenceEquals(sref1, sref2))
			{
				// same instance -> equal
				return true;
			}

			// IClone.IsEqual does NOT compare tolerances, can't use

			var compareSref1 = sref1 as ICompareCoordinateSystems;

			// both not null -> check
			if (compareSref1 == null || ! compareSref1.IsEqualNoVCS(sref2))
			{
				// coordinate system different (ignoring precision/tolerance) -> not equal
				return false;
			}

			if (comparePrecisionAndTolerance)
			{
				// compare precision first
				bool compareOnlyXYPrecision = ! compareVerticalCoordinateSystems;
				if (! IsPrecisionEqual(sref1, sref2, compareOnlyXYPrecision))
				{
					return false;
				}

				// if precision equal, compare relevant tolerances also
				var sref1Tol = (ISpatialReferenceTolerance) sref1;

				if (! sref1Tol.IsXYToleranceEqual(sref2))
				{
					return false;
				}

				if (compareVerticalCoordinateSystems &&
				    ! sref1Tol.IsZToleranceEqual(sref2))
				{
					return false;
				}
			}

			return ! compareVerticalCoordinateSystems ||
			       compareSref1.IsEqualLeftLongitude(sref2, true);
		}

		/// <summary>
		/// Returns a value indicating if two vertical coordinate systems are equal with regard to
		/// their factory codes.
		/// </summary>
		/// <param name="vcs1">The first vertical coordinate system.</param>
		/// <param name="vcs2">The second vertical coordinate system.</param>
		/// <returns><c>true</c> if the factory codes and names are equal, <c>false</c> otherwise.</returns>
		public static bool AreEqual([CanBeNull] IVerticalCoordinateSystem vcs1,
		                            [CanBeNull] IVerticalCoordinateSystem vcs2)
		{
			if (vcs1 == null && vcs2 == null)
			{
				// both null -> equal
				return true;
			}

			if (vcs1 == null || vcs2 == null)
			{
				return false;
			}

			return ((IClone) vcs1).IsEqual((IClone) vcs2);
		}

		public static bool IsHorizontalCoordinateSystemEqual(
			[NotNull] ISpatialReference sref1,
			[NotNull] ISpatialReference sref2)
		{
			Assert.ArgumentNotNull(sref1, nameof(sref1));
			Assert.ArgumentNotNull(sref2, nameof(sref2));

			return ((ICompareCoordinateSystems) sref1).IsEqualNoVCS(sref2);
		}

		[NotNull]
		public static string ExportToString([NotNull] ISpatialReference sref)
		{
			Assert.ArgumentNotNull(sref, nameof(sref));

			var esriSref = (IESRISpatialReferenceGEN) sref;

			string srString;
			esriSref.ExportToESRISpatialReference(out srString, out int _);

			Assert.NotNull(srString, "output string is null");

			return srString;
		}

		/// <summary>
		/// Exports a spatial reference to an xml string
		/// </summary>
		/// <param name="spatialReference">The spatial reference.</param>
		/// <returns></returns>
		[NotNull]
		public static string ToXmlString([NotNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			var serializer = new XMLSerializerClass();
			try
			{
				string result = serializer.SaveToString(spatialReference, null, null);
				Assert.NotNull(result, "result string is null");
				return result;
			}
			finally
			{
				Marshal.ReleaseComObject(serializer);
			}
		}

		/// <summary>
		/// Creates a spatial reference based on an xml string containing a spatial reference.
		/// </summary>
		/// <param name="xmlSpatialReferenceString">The XML spatial reference string.</param>
		/// <returns></returns>
		[NotNull]
		public static ISpatialReference FromXmlString(
			[NotNull] string xmlSpatialReferenceString)
		{
			Assert.ArgumentNotNull(xmlSpatialReferenceString,
			                       nameof(xmlSpatialReferenceString));

			IXMLSerializer serializer = new XMLSerializerClass();
			try
			{
				var result = (ISpatialReference)
					serializer.LoadFromString(xmlSpatialReferenceString,
					                          null, null);
				Assert.NotNull(result, "spatial refrence is null is null");
				return result;
			}
			finally
			{
				Marshal.ReleaseComObject(serializer);
			}
		}

		/// <summary>
		/// Configures the Z domain properties of a spatial reference.
		/// </summary>
		/// <param name="sref">The spatial reference to configure.</param>
		/// <param name="zmin">The minimum z value of the domain.</param>
		/// <param name="zmax">The maximum z value of the domain.</param>
		/// <param name="zResolution">The z resolution.</param>
		/// <param name="zTolerance">The z tolerance.</param>
		/// <returns>The modified spatial reference (same instance as input)</returns>
		[NotNull]
		public static ISpatialReference SetZDomain([NotNull] ISpatialReference sref,
		                                           double zmin, double zmax,
		                                           double zResolution,
		                                           double zTolerance)
		{
			Assert.ArgumentNotNull(sref, nameof(sref));

			// Alternative: sref.SetZFalseOriginAndUnits(zmin, 1 / zResolution);
			sref.SetZDomain(zmin, zmax);

			var resolution = (ISpatialReferenceResolution) sref;

			resolution.set_ZResolution(true, zResolution);

			var tol = (ISpatialReferenceTolerance) sref;
			tol.ZTolerance = zTolerance;

			return sref;
		}

		[NotNull]
		public static ISpatialReference SetMDomain([NotNull] ISpatialReference sref,
		                                           double mmin, double mmax,
		                                           double mResolution,
		                                           double mTolerance)
		{
			Assert.ArgumentNotNull(sref, nameof(sref));

			sref.SetMDomain(mmin, mmax);

			var resolution = (ISpatialReferenceResolution) sref;

			sref.SetMDomain(mmin, mmax);

			resolution.MResolution = mResolution;

			var tol = (ISpatialReferenceTolerance) sref;
			tol.MTolerance = mTolerance;

			return sref;
		}

		/// <summary>
		/// Configures the XY domain properties of a spatial reference.
		/// </summary>
		/// <param name="sref">The spatial reference to configure.</param>
		/// <param name="xmin">The minimum x value of the domain.</param>
		/// <param name="ymin">The minimum y value of the domain.</param>
		/// <param name="xmax">The maximum x value of the domain.</param>
		/// <param name="ymax">The maximum x value of the domain.</param>
		/// <param name="xyResolution">The xy resolution.</param>
		/// <param name="xyTolerance">The xy tolerance.</param>
		/// <returns>The modified spatial reference (same instance as input)</returns>
		[NotNull]
		public static ISpatialReference SetXYDomain([NotNull] ISpatialReference sref,
		                                            double xmin, double ymin,
		                                            double xmax, double ymax,
		                                            double xyResolution,
		                                            double xyTolerance)
		{
			Assert.ArgumentNotNull(sref, nameof(sref));

			sref.SetDomain(xmin, xmax, ymin, ymax);

			var res = (ISpatialReferenceResolution) sref;

			res.set_XYResolution(true, xyResolution);

			var tol = (ISpatialReferenceTolerance) sref;
			tol.XYTolerance = xyTolerance;

			return sref;
		}

		/// <summary>
		/// Returns a <see cref="string"></see> that represents the given <see cref="ISpatialReference"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="string"></see> that represents the current <see cref="ISpatialReference"></see>.
		/// </returns>
		[NotNull]
		public static string ToString([CanBeNull] ISpatialReference sref)
		{
			try
			{
				var sb = new StringBuilder();

				if (sref == null)
				{
					sb.AppendLine("Spatial reference is null");
				}
				else
				{
					// ISpatialReference2GEN srefGen = (ISpatialReference2GEN) sref;

					sb.AppendFormat("Spatial Reference: {0}", sref.Name);
					sb.AppendLine();
					sb.AppendFormat("Remarks: {0}", sref.Remarks);
					sb.AppendLine();

					sb.AppendLine(ExportToString(sref));
					var res = sref as ISpatialReferenceResolution;
					if (res != null)
					{
						sb.AppendFormat("- XY resolution: {0}", res.XYResolution[true]);
						sb.AppendLine();
						sb.AppendFormat("- Z resolution: {0}", res.ZResolution[true]);
						sb.AppendLine();
						sb.AppendFormat("- M resolution: {0}", res.MResolution);
						sb.AppendLine();
					}

					if (sref.HasXYPrecision())
					{
						double xmin;
						double ymin;
						double xmax;
						double ymax;
						sref.GetDomain(out xmin, out xmax, out ymin, out ymax);
						sb.AppendFormat(
							"- XY Domain: XMin {0:N5} YMin {1:N5} XMax {2:N5} YMax {3:N5}",
							xmin, ymin, xmax, ymax);
						sb.AppendLine();
					}
					else
					{
						sb.AppendLine("- No XY precision defined");
					}

					if (sref.HasZPrecision())
					{
						try
						{
							double zMin;
							double zMax;
							sref.GetZDomain(out zMin, out zMax);
							sb.AppendFormat("- Z Domain: ZMin {0:N5} ZMax {1:N5}", zMin,
							                zMax);
							sb.AppendLine();
						}
						catch (Exception e)
						{
							sb.AppendFormat("ERROR getting Z domain: {0}", e.Message);
							sb.AppendLine();
						}
					}
					else
					{
						sb.AppendLine("- No Z precision defined");
					}

					if (sref.HasMPrecision())
					{
						try
						{
							double mMin;
							double mMax;
							sref.GetMDomain(out mMin, out mMax);
							sb.AppendFormat("- M Domain: MMin {0:N5} MMax {1:N5}", mMin,
							                mMax);
							sb.AppendLine();
						}
						catch (Exception e)
						{
							sb.AppendFormat("ERROR getting M domain: {0}", e.Message);
							sb.AppendLine();
						}
					}
					else
					{
						sb.AppendLine("- No M precision defined");
					}

					var tol = sref as ISpatialReferenceTolerance;
					if (tol != null)
					{
						sb.AppendFormat("- XY tolerance: {0}", tol.XYTolerance);
						sb.AppendLine();
						sb.AppendFormat("- Z tolerance: {0}", tol.ZTolerance);
						sb.AppendLine();
						sb.AppendFormat("- M tolerance: {0}", tol.MTolerance);
						sb.AppendLine();
					}

					var prec = sref as IControlPrecision2;
					if (prec != null)
					{
						sb.AppendFormat("- High precision: {0}", prec.IsHighPrecision);
						sb.AppendLine();
						sb.AppendFormat("- Use Precision: {0}", prec.UsePrecision);
						sb.AppendLine();
					}

					var sref3 = sref as ISpatialReference3;
					if (sref3 != null)
					{
						IVerticalCoordinateSystem vcs = sref3.VerticalCoordinateSystem;
						if (vcs != null)
						{
							sb.AppendFormat("- Vertical coordinate system: {0}",
							                vcs.Name);
							sb.AppendLine();
							if (vcs.Datum != null)
							{
								var vDatumInfo = (ISpatialReferenceInfo) vcs.Datum;
								sb.AppendFormat("  - Vertical datum: {0}",
								                vDatumInfo.Name);
								sb.AppendLine();
							}
							else
							{
								sb.AppendLine("  - No vertical datum defined");
							}

							sb.AppendFormat("  - VCS Z coordinate unit: {0}",
							                vcs.CoordinateUnit == null
								                ? "<null>"
								                : vcs.CoordinateUnit.Name);
							sb.AppendLine();
						}
						else
						{
							sb.AppendLine("No vertical coordinate system defined");
						}
					}

					if (sref.ZCoordinateUnit != null)
					{
						sb.AppendFormat("- Z coordinate unit: {0}",
						                sref.ZCoordinateUnit.Name);
						sb.AppendLine();
					}
					else
					{
						sb.AppendLine("- No Z coordinate unit defined");
					}
				}

				return sb.ToString();
			}
			catch (Exception e)
			{
				return HandleToStringException(e);
			}
		}

		/// <summary>
		/// Gets the ESRI string representation of a given spatial reference component.
		/// </summary>
		/// <param name="spatialReference">The spatial reference.</param>
		/// <returns></returns>
		[NotNull]
		public static string ExportToESRISpatialReference(
			[NotNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			var esriSref = spatialReference as IESRISpatialReferenceGEN2;

			if (esriSref == null)
			{
				return string.Empty;
			}

			string result;
			esriSref.ExportToESRISpatialReference2(out result, out int _);

			return result;
		}

		[NotNull]
		public static ISpatialReference ImportFromESRISpatialReference(
			[NotNull] string esriSpatialReferenceString)
		{
			Assert.ArgumentNotNull(esriSpatialReferenceString,
			                       nameof(esriSpatialReferenceString));

			IESRISpatialReferenceGEN2 result = new ProjectedCoordinateSystemClass();
			result.ImportFromESRISpatialReference(esriSpatialReferenceString, out int _);

			return (ISpatialReference) result;
		}

		[NotNull]
		public static IEnumerable<IGeoTransformation> GetPredefinedGeoTransformations()
		{
			ISet set = Factory.GetPredefinedGeographicTransformations();

			set.Reset();

			var geoTransformation = (IGeoTransformation) set.Next();

			while (geoTransformation != null)
			{
				yield return geoTransformation;

				geoTransformation = (IGeoTransformation) set.Next();
			}
		}

		[NotNull]
		public static IGeoTransformationOperationSet GetGeoTransformationDefaults()
		{
			return Factory.GeoTransformationDefaults;
		}

		[NotNull]
		public static IEnumerable<KeyValuePair<IGeoTransformation, esriTransformDirection>
			>
			GetGeoTransformations([NotNull] IGeoTransformationOperationSet set)
		{
			Assert.ArgumentNotNull(set, nameof(set));

			set.Reset();

			IGeoTransformation geoTransformation;
			esriTransformDirection direction;
			set.Next(out direction, out geoTransformation);

			while (geoTransformation != null)
			{
				yield return new KeyValuePair<IGeoTransformation, esriTransformDirection>(
					geoTransformation, direction);

				set.Next(out direction, out geoTransformation);
			}
		}

		[NotNull]
		public static IList<KeyValuePair<IGeoTransformation, esriTransformDirection>>
			GetPredefinedGeoTransformations(
				[NotNull] ISpatialReference fromSpatialReference,
				[NotNull] ISpatialReference toSpatialReference)
		{
			ISpatialReference fromGCS =
				GetGeographicCoordinateSystem(fromSpatialReference);
			ISpatialReference toGCS =
				GetGeographicCoordinateSystem(toSpatialReference);

			var result =
				new List<KeyValuePair<IGeoTransformation, esriTransformDirection>>();

			foreach (IGeoTransformation geoTransformation in
			         GetPredefinedGeoTransformations())
			{
				esriTransformDirection? direction = GetTransformationDirection(
					geoTransformation,
					fromGCS, toGCS);
				if (direction == null)
				{
					continue;
				}

				result.Add(new KeyValuePair<IGeoTransformation, esriTransformDirection>(
					           geoTransformation, direction.Value));
			}

			return result;
		}

		[NotNull]
		public static IList<IGeoTransformation> GetGeoTransformations(
			[NotNull] IGeoTransformationOperationSet set,
			[NotNull] ISpatialReference fromSpatialReference,
			[NotNull] ISpatialReference toSpatialReference)
		{
			ISpatialReference fromGCS =
				GetGeographicCoordinateSystem(fromSpatialReference);
			ISpatialReference toGCS =
				GetGeographicCoordinateSystem(toSpatialReference);

			var result = new List<IGeoTransformation>();

			foreach (
				KeyValuePair<IGeoTransformation, esriTransformDirection> pair in
				GetGeoTransformations(set))
			{
				IGeoTransformation geoTransformation = pair.Key;
				esriTransformDirection direction = pair.Value;

				ISpatialReference transformationSource;
				ISpatialReference transformationTarget;
				geoTransformation.GetSpatialReferences(out transformationSource,
				                                       out transformationTarget);

				ISpatialReference gcs1 =
					direction == esriTransformDirection.esriTransformForward
						? fromGCS
						: toGCS;
				ISpatialReference gcs2 =
					direction == esriTransformDirection.esriTransformForward
						? toGCS
						: fromGCS;

				if (IsHorizontalCoordinateSystemEqual(gcs1, transformationSource) &&
				    IsHorizontalCoordinateSystemEqual(gcs2, transformationTarget))
				{
					result.Add(geoTransformation);
				}
			}

			return result;
		}

		public static void EnsureGeoTransformations(
			[NotNull] IGeoTransformationOperationSet geoTransformationSet,
			bool throwOnError,
			[NotNull] IEnumerable<esriSRGeoTransformationType> transformationTypes)
		{
			Assert.ArgumentNotNull(geoTransformationSet, nameof(geoTransformationSet));
			Assert.ArgumentNotNull(transformationTypes, nameof(transformationTypes));

			EnsureGeoTransformations(geoTransformationSet, throwOnError,
			                         transformationTypes.Select(CreateGeoTransformation)
			                                            .ToList());
		}

		public static void EnsureGeoTransformations(
			[NotNull] IGeoTransformationOperationSet geoTransformationSet,
			bool throwOnError,
			[NotNull] IEnumerable<esriSRGeoTransformation2Type> transformationTypes)
		{
			Assert.ArgumentNotNull(geoTransformationSet, nameof(geoTransformationSet));
			Assert.ArgumentNotNull(transformationTypes, nameof(transformationTypes));

			EnsureGeoTransformations(geoTransformationSet, throwOnError,
			                         transformationTypes.Select(CreateGeoTransformation)
			                                            .ToList());
		}

		public static void EnsureGeoTransformations(
			[NotNull] IGeoTransformationOperationSet geoTransformationSet,
			bool throwOnError,
			[NotNull] IEnumerable<esriSRGeoTransformation3Type> transformationTypes)
		{
			Assert.ArgumentNotNull(geoTransformationSet, nameof(geoTransformationSet));
			Assert.ArgumentNotNull(transformationTypes, nameof(transformationTypes));

			EnsureGeoTransformations(geoTransformationSet, throwOnError,
			                         transformationTypes.Select(CreateGeoTransformation)
			                                            .ToList());
		}

		public static bool EnsureGeoTransformation(
			esriSRGeoTransformationType transformationType,
			esriTransformDirection direction)
		{
			return EnsureGeoTransformation(
				CreateGeoTransformation(transformationType),
				direction);
		}

		public static bool EnsureGeoTransformation(
			esriSRGeoTransformation2Type transformationType,
			esriTransformDirection direction)
		{
			return EnsureGeoTransformation(
				CreateGeoTransformation(transformationType),
				direction);
		}

		public static bool EnsureGeoTransformation(
			esriSRGeoTransformation3Type transformationType,
			esriTransformDirection direction)
		{
			return EnsureGeoTransformation(
				CreateGeoTransformation(transformationType),
				direction);
		}

		public static bool EnsureGeoTransformation(
			[NotNull] IGeoTransformation transformation,
			esriTransformDirection direction)
		{
			Assert.ArgumentNotNull(transformation, nameof(transformation));

			return EnsureGeoTransformation(transformation, direction,
			                               Factory.GeoTransformationDefaults);
		}

		public static bool EnsureGeoTransformation(
			[NotNull] IGeoTransformation transformation,
			esriTransformDirection direction,
			[NotNull] IGeoTransformationOperationSet set)
		{
			Assert.ArgumentNotNull(transformation, nameof(transformation));
			Assert.ArgumentNotNull(set, nameof(set));

			var changed = false;
			if (! set.Find(direction, transformation))
			{
				set.Set(direction, transformation);
				changed = true;
			}

			return changed;
		}

		[NotNull]
		public static IGeoTransformation CreateGeoTransformation(
			esriSRGeoTransformationType transformationType)
		{
			return CreateGeoTransformation((int) transformationType);
		}

		[NotNull]
		public static IGeoTransformation CreateGeoTransformation(
			esriSRGeoTransformation2Type transformationType)
		{
			return CreateGeoTransformation((int) transformationType);
		}

		[NotNull]
		public static IGeoTransformation CreateGeoTransformation(
			esriSRGeoTransformation3Type transformationType)
		{
			return CreateGeoTransformation((int) transformationType);
		}

		[NotNull]
		public static IGeoTransformation CreateGeoTransformation(int transformationType)
		{
			try
			{
				return (IGeoTransformation) Assert.NotNull(
					Factory.CreateGeoTransformation(transformationType));
			}
			catch (Exception e)
			{
				throw new ArgumentException(
					string.Format("Error creating geo transformation for type {0}",
					              transformationType),
					nameof(transformationType), e);
			}
		}

		[NotNull]
		public static ISpatialReference GetUniqueSpatialReference(
			[NotNull] IEnumerable<ISpatialReference> spatialReferences)
		{
			const bool comparePrecisionAndTolerance = false;
			const bool compareVerticalCoordinateSystems = false;
			return GetUniqueSpatialReference(spatialReferences,
			                                 comparePrecisionAndTolerance,
			                                 compareVerticalCoordinateSystems);
		}

		[NotNull]
		public static ISpatialReference GetUniqueSpatialReference(
			[NotNull] IEnumerable<ISpatialReference> spatialReferences,
			bool comparePrecisionAndTolerance,
			bool compareVerticalCoordinateSystems)
		{
			Assert.ArgumentNotNull(spatialReferences, nameof(spatialReferences));

			ISpatialReference result = null;

			foreach (ISpatialReference spatialReference in spatialReferences)
			{
				if (spatialReference == null)
				{
					continue;
				}

				if (result == null)
				{
					result = spatialReference;
				}
				else
				{
					if (! AreEqual(result, spatialReference,
					               comparePrecisionAndTolerance,
					               compareVerticalCoordinateSystems))
					{
						throw new InvalidOperationException(
							"Spatial references are not identical");
					}
				}
			}

			return Assert.NotNull(result, "result");
		}

		[NotNull]
		public static ISpatialReference GetGeographicCoordinateSystem(
			[NotNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));
			ISpatialReference sr = spatialReference;

			if (sr is IProjectedCoordinateSystem pcs)
				return pcs.GeographicCoordinateSystem;

			if (sr is IGeographicCoordinateSystem gcs)
				return gcs;

			throw new ArgumentException(
				$"Spatial reference '{sr.Name}' uses neither a projected nor a geographic coordinate system",
				nameof(spatialReference));
		}

		public static esriTransformDirection? GetTransformationDirection(
			[NotNull] IGeoTransformation geoTransformation,
			[NotNull] ISpatialReference fromSpatialReference,
			[NotNull] ISpatialReference toSpatialReference)
		{
			ISpatialReference transformationSource;
			ISpatialReference transformationTarget;
			geoTransformation.GetSpatialReferences(out transformationSource,
			                                       out transformationTarget);

			ISpatialReference fromGCS =
				GetGeographicCoordinateSystem(fromSpatialReference);
			ISpatialReference toGCS =
				GetGeographicCoordinateSystem(toSpatialReference);

			if (IsHorizontalCoordinateSystemEqual(fromGCS, transformationSource) &&
			    IsHorizontalCoordinateSystemEqual(toGCS, transformationTarget))
			{
				return esriTransformDirection.esriTransformForward;
			}

			if (IsHorizontalCoordinateSystemEqual(fromGCS, transformationTarget) &&
			    IsHorizontalCoordinateSystemEqual(toGCS, transformationSource))
			{
				return esriTransformDirection.esriTransformReverse;
			}

			return null;
		}

		/// <summary>
		/// Returns the name of the specified unit.
		/// </summary>
		/// <param name="distanceUnits"></param>
		/// <param name="caseAppearance"></param>
		/// <param name="plural"></param>
		/// <returns></returns>
		[NotNull]
		public static string GetName(esriUnits distanceUnits,
		                             esriCaseAppearance caseAppearance, bool plural)
		{
			IUnitConverter unitConverter = new UnitConverterClass();

			return unitConverter.EsriUnitsAsString(distanceUnits, caseAppearance, plural);
		}

		public static T ProjectEx<T>(
			[NotNull] T geometry,
			[NotNull] ISpatialReference targetSr,
			[CanBeNull] string transformation = null,
			bool noNewInstance = false)
			where T : IGeometry
		{
			ISpatialReference sourceSr = geometry.SpatialReference;

			if (sourceSr == null)
			{
				_msg.Warn($"no spatial reference for {GeometryUtils.ToString(geometry)}");
				T result = noNewInstance ? geometry : SysUtils.Clone(geometry);
				result.Project(targetSr);
				return result;
			}

			if (sourceSr.FactoryCode == targetSr.FactoryCode)
			{
				return noNewInstance ? geometry : SysUtils.Clone(geometry);
			}

			GeoTrans trans =
				GetGeoTrans(sourceSr.FactoryCode, targetSr.FactoryCode, transformation);

			IGeometry2 geom = noNewInstance == false
				                  ? (IGeometry2) SysUtils.Clone(geometry)
				                  : (IGeometry2) geometry;

			geom.ProjectEx(targetSr, trans.Dir, trans.GeogrTrans, false, 0, 0);

			return (T) geom;
		}

		public static void GetGeoTrans(
			[NotNull] ISpatialReference sourceSr, [NotNull] ISpatialReference targetSr,
			out IGeoTransformation geoTransformation, out esriTransformDirection dir,
			string transformation = null)
		{
			GeoTrans trans =
				GetGeoTrans(sourceSr.FactoryCode, targetSr.FactoryCode, transformation);

			geoTransformation = trans.GeogrTrans;
			dir = trans.Dir;
		}

		private class GeoTrans
		{
			public ISpatialReference FromSr { get; set; }
			public ISpatialReference ToSr { get; set; }
			public IGeoTransformation GeogrTrans { get; set; }
			public esriTransformDirection Dir { get; set; }
		}

		private class KeyComparer : IEqualityComparer<int[]>
		{
			public bool Equals(int[] x, int[] y)
			{
				if (x == y) return true;
				if (x == null || y == null) return false;

				return x[0] == y[0] && x[1] == y[1];
			}

			public int GetHashCode(int[] o)
			{
				return o[0] + 29 * o[1];
			}
		}

		private static readonly ThreadLocal<Dictionary<int[], IList<GeoTrans>>> _geoTransCache =
			new ThreadLocal<Dictionary<int[], IList<GeoTrans>>>(
				() => new Dictionary<int[], IList<GeoTrans>>(new KeyComparer()));

		private static readonly ThreadLocal<ISpatialReferenceFactory2> _spatialReferenceFactory =
			new ThreadLocal<ISpatialReferenceFactory2>(
				() => ComUtils
					.Create<SpatialReferenceEnvironmentClass, ISpatialReferenceFactory2>());

		private static IList<GeoTrans> GetTransList(int fromSr, int toSr)
		{
			return GetTransList(GetPrj(fromSr), GetPrj(toSr));
		}

		private static IList<GeoTrans> GetTransList(ISpatialReference fromSr,
		                                            ISpatialReference toSr)
		{
			List<IGeoTransformation> transList = GetTransformations(fromSr, toSr);
			List<GeoTrans> geoTransList = new List<GeoTrans>();
			foreach (var trans in transList)
			{
				geoTransList.Add(new GeoTrans
				                 {
					                 FromSr = fromSr, ToSr = toSr, GeogrTrans = trans,
					                 Dir = GetDir(trans, fromSr, toSr)
				                 });
			}

			return geoTransList;
		}

		private static ISpatialReference GetPrj(int srId)
		{
			ISpatialReference sr = _spatialReferenceFactory.Value.CreateSpatialReference(srId);
			return sr;
		}

		private static esriTransformDirection GetDir(IGeoTransformation geoTrans,
		                                             ISpatialReference sr1, ISpatialReference sr2)
		{
			int code1 = GetGeographicCoordinateSystem(sr1).FactoryCode;
			int code2 = GetGeographicCoordinateSystem(sr2).FactoryCode;
			ISpatialReference fromSR;
			ISpatialReference toSR;
			geoTrans.GetSpatialReferences(out fromSR, out toSR);
			if (fromSR.FactoryCode == code1 && toSR.FactoryCode == code2)
				return esriTransformDirection.esriTransformForward;
			else if (fromSR.FactoryCode == code2 && toSR.FactoryCode == code1)
				return esriTransformDirection.esriTransformReverse;
			else
				throw new Exception(string.Format("{0} does not support going between {1} and {2}",
				                                  geoTrans.Name, sr1.Name, sr2.Name));
		}

		private static List<IGeoTransformation> GetTransformations(
			ISpatialReference fromSR, ISpatialReference toSR)
		{
			int fromFactcode = GetGeographicCoordinateSystem(fromSR).FactoryCode;
			int toFactcode = GetGeographicCoordinateSystem(toSR).FactoryCode;

			var outList = new List<IGeoTransformation>();
			// Use activator to instantiate arcobjects singletons ...
			//var type = Type.GetTypeFromProgID("esriGeometry.SpatialReferenceEnvironment");
			var srf = _spatialReferenceFactory.Value;

			var gtSet = srf.CreatePredefinedGeographicTransformations();
			gtSet.Reset();
			for (int i = 0; i < gtSet.Count; i++)
			{
				var geoTrans = (IGeoTransformation) gtSet.Next();
				geoTrans.GetSpatialReferences(out ISpatialReference fromGcsSR,
				                              out ISpatialReference toGcsSR);
				if ((fromGcsSR.FactoryCode == fromFactcode && toGcsSR.FactoryCode == toFactcode) ||
				    (fromGcsSR.FactoryCode == toFactcode && toGcsSR.FactoryCode == fromFactcode))
				{
					outList.Add(geoTrans);
				}
			}

			return outList;
		}

		private static IGeographicCoordinateSystem GetGCSFactoryCode(ISpatialReference sr)
		{
			if (sr is IProjectedCoordinateSystem pcs)
				return pcs.GeographicCoordinateSystem;
			else if (sr is IGeographicCoordinateSystem gcs)
				return gcs;
			else
				throw new InvalidOperationException($"unsupported spatialref type of {sr.Name}");
		}

		private static GeoTrans GetGeoTrans(int fromSr, int toSr, string transformation)
		{
			int[] key = new[] { fromSr, toSr };
			IList<GeoTrans> transList;
			if (! _geoTransCache.Value.TryGetValue(key, out transList))
			{
				transList = GetTransList(fromSr, toSr);
				Assert.True((transList?.Count ?? 0) > 0,
				            $"No transformation found between SrIds {fromSr} and {toSr}");
				_geoTransCache.Value.Add(key, transList);
			}

			return string.IsNullOrWhiteSpace(transformation)
				       ? transList[0]
				       : transList.First(x => string.Equals(x.GeogrTrans.Name, transformation,
				                                            StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Returns the abbreviation of the specified unit.
		/// </summary>
		/// <param name="distanceUnits"></param>
		/// <returns></returns>
		[NotNull]
		public static string GetAbbreviation(esriUnits distanceUnits)
		{
			// consider creating IUnit and try abbreviation

			switch (distanceUnits)
			{
				case esriUnits.esriCentimeters:
					return "cm";

				case esriUnits.esriDecimalDegrees:
					return "DD";

				case esriUnits.esriDecimeters:
					return "dm";

				case esriUnits.esriFeet:
					return "ft";

				case esriUnits.esriInches:
					return "in";

				case esriUnits.esriKilometers:
					return "km";

				case esriUnits.esriMeters:
					return "m";

				case esriUnits.esriMiles:
					return "mi";

				case esriUnits.esriMillimeters:
					return "mm";

				case esriUnits.esriNauticalMiles:
					return "nm";

				case esriUnits.esriPoints:
					return "pt";

				case esriUnits.esriUnknownUnits:
					return "<unknown unit>";

				case esriUnits.esriYards:
					return "yd";

				default:
					throw new InvalidEnumArgumentException(
						$@"Unknown unit: {distanceUnits}");
			}
		}

		public static double GetXyResolution([NotNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			// TODO revise: bStandardUnits should be false, probably
			return ((ISpatialReferenceResolution) spatialReference).XYResolution[true];
		}

		public static double GetZResolution([NotNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			return ((ISpatialReferenceResolution) spatialReference).ZResolution[false];
		}

		public static double GetMResolution([NotNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			return ((ISpatialReferenceResolution) spatialReference).MResolution;
		}

		public static double GetXyTolerance([NotNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			return ((ISpatialReferenceTolerance) spatialReference).XYTolerance;
		}

		public static double GetZTolerance([NotNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			return ((ISpatialReferenceTolerance) spatialReference).ZTolerance;
		}

		#region Non-public methods

		[NotNull]
		private static ISpatialReferenceFactory3 Factory
			=> _factory ?? (_factory = CreateSpatialReferenceFactory());

		private static void EnsureGeoTransformations(
			[NotNull] IGeoTransformationOperationSet geoTransformationSet,
			bool throwOnError,
			[NotNull] IEnumerable<IGeoTransformation> transformations)
		{
			Assert.ArgumentNotNull(geoTransformationSet,
			                       nameof(geoTransformationSet));
			Assert.ArgumentNotNull(transformations, nameof(transformations));

			var directions =
				new[]
				{
					esriTransformDirection.esriTransformForward,
					esriTransformDirection.esriTransformReverse
				};

			foreach (IGeoTransformation transformation in transformations)
			{
				try
				{
					foreach (esriTransformDirection direction in directions)
					{
						EnsureGeoTransformation(
							transformation, direction, geoTransformationSet);
					}
				}
				catch (Exception e)
				{
					if (throwOnError)
					{
						throw;
					}

					_msg.Warn(string.Format(
						          "Error setting up default geographic transformation {0}: {1}",
						          transformation.Name, e.Message),
					          e);
				}
			}
		}

		private static bool IsPrecisionEqual([NotNull] ISpatialReference sref1,
		                                     [NotNull] ISpatialReference sref2,
		                                     bool onlyXYPrecision)
		{
			Assert.ArgumentNotNull(sref1, nameof(sref1));
			Assert.ArgumentNotNull(sref2, nameof(sref2));

			bool precisionEqual;
			if (onlyXYPrecision)
			{
				precisionEqual = ((ISpatialReference2) sref1).IsXYPrecisionEqual(sref2);
			}
			else
			{
				// xy, z, AND m domains/resolutions AND tolerances

				// TODO NO the documentation is lying: tolerances are NOT compared
				sref1.IsPrecisionEqual(sref2, out precisionEqual);
			}

			return precisionEqual;
		}

		[NotNull]
		private static ISpatialReferenceFactory3 CreateSpatialReferenceFactory()
		{
			// the hard way, workaround for singleton interop issue

			// NOTE: Getting the type from the ProgID fails starting from 11.0
			//       Therefore get the type directly:
			Assembly geometryAssembly = typeof(ISpatialReferenceFactory3).Assembly;
			Type srFactoryType =
				geometryAssembly.GetType("ESRI.ArcGIS.Geometry.SpatialReferenceEnvironmentClass");

			return ComUtils.Create<SpatialReferenceEnvironmentClass, ISpatialReferenceFactory3>();
			return ComUtils.CreateObject<ISpatialReferenceFactory3>(
				"esriGeometry.SpatialReferenceEnvironment");
		}

		[NotNull]
		private static string HandleToStringException(Exception e)
		{
			string msg = string.Format("Error converting to string: {0}",
			                           e.Message);
			_msg.Debug(msg, e);
			return msg;
		}

		#endregion
	}
}
