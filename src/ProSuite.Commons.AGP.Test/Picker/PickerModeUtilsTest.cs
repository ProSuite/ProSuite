using NUnit.Framework;
using ProSuite.Commons.AGP.Picker;

namespace ProSuite.Commons.AGP.Test.Picker
{
	[TestFixture]
	public class PickerModeUtilsTest
	{
		// Signature under test:
		// DeterminePickerMode(control, alt, isPointClick, noMultiselection,
		//                     candidateCount, lowestDimensionCount)

		[Test]
		public void ControlAlwaysShowsPicker()
		{
			// CTRL wins over everything else.
			Assert.AreEqual(PickerMode.ShowPicker,
			                Determine(control: true, alt: false, point: true,
			                          noMulti: false, count: 1, lowestDim: 1));

			Assert.AreEqual(PickerMode.ShowPicker,
			                Determine(control: true, alt: true, point: false,
			                          noMulti: true, count: 5, lowestDim: 3));
		}

		#region No multiselection allowed (count > 1)

		[Test]
		public void NoMultiselection_AreaSelect_ShowsPicker()
		{
			Assert.AreEqual(PickerMode.ShowPicker,
			                Determine(control: false, alt: false, point: false,
			                          noMulti: true, count: 3, lowestDim: 1));
		}

		[Test]
		public void NoMultiselection_PointClick_MultipleLowestDim_ShowsPicker()
		{
			Assert.AreEqual(PickerMode.ShowPicker,
			                Determine(control: false, alt: false, point: true,
			                          noMulti: true, count: 3, lowestDim: 2));
		}

		[Test]
		public void NoMultiselection_PointClick_SingleLowestDim_PicksBest()
		{
			Assert.AreEqual(PickerMode.PickBest,
			                Determine(control: false, alt: false, point: true,
			                          noMulti: true, count: 3, lowestDim: 1));
		}

		[Test]
		public void NoMultiselection_IgnoresAlt()
		{
			// In no-multiselection mode, ALT does not trigger PickAll.
			Assert.AreEqual(PickerMode.PickBest,
			                Determine(control: false, alt: true, point: true,
			                          noMulti: true, count: 3, lowestDim: 1));
		}

		[Test]
		public void NoMultiselection_SingleCandidate_PicksBest()
		{
			// count == 1: the no-multiselection guard does not apply.
			Assert.AreEqual(PickerMode.PickBest,
			                Determine(control: false, alt: false, point: true,
			                          noMulti: true, count: 1, lowestDim: 1));
		}

		#endregion

		#region Multiselection allowed

		[Test]
		public void Multiselection_AltPointClick_PicksAll()
		{
			Assert.AreEqual(PickerMode.PickAll,
			                Determine(control: false, alt: true, point: true,
			                          noMulti: false, count: 3, lowestDim: 2));
		}

		[Test]
		public void Multiselection_AreaSelect_PicksAll()
		{
			Assert.AreEqual(PickerMode.PickAll,
			                Determine(control: false, alt: false, point: false,
			                          noMulti: false, count: 3, lowestDim: 2));
		}

		/// <summary>
		/// GOTOP-1273: with multiselection allowed, a plain point click on several
		/// candidates that share the lowest geometry dimension must show the picker.
		/// </summary>
		[Test]
		public void Multiselection_PointClick_MultipleLowestDim_ShowsPicker()
		{
			Assert.AreEqual(PickerMode.ShowPicker,
			                Determine(control: false, alt: false, point: true,
			                          noMulti: false, count: 2, lowestDim: 2));
		}

		[Test]
		public void Multiselection_PointClick_SingleLowestDim_PicksBest()
		{
			Assert.AreEqual(PickerMode.PickBest,
			                Determine(control: false, alt: false, point: true,
			                          noMulti: false, count: 2, lowestDim: 1));
		}

		#endregion

		private static PickerMode Determine(bool control, bool alt, bool point, bool noMulti,
		                                    int count, int lowestDim)
		{
			return PickerModeUtils.DeterminePickerMode(control, alt, point, noMulti, count,
			                                           lowestDim);
		}
	}
}
