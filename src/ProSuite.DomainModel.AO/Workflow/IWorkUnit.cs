using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.Workflow
{
	public interface IWorkUnit : IWorkContext
	{
		int WorkUnitId { get; }

		/// <summary>
		/// Returns a copy of the perimeter, which can be freely modified by client code.
		/// </summary>
		/// <value>The copy of the perimeter geometry.</value>
		IPolygon PerimeterCopy { get; }

		DateTime? StartDate { get; }

		IPolygon Perimeter { get; }

		bool IsEditable([NotNull] ObjectCategory objectCategory);

		/// <summary>
		/// Determines whether the specified object category is editable in the work unit, in 
		/// the current state of the work unit (but independent of the privileges of the user).
		/// </summary>
		/// <param name="objectCategory">The object category.</param>
		/// <returns>
		/// 	<c>true</c> if the specified object category is generally editable in the
		///     current state of the work unit; otherwise, <c>false</c>.
		/// </returns>
		bool IsEditableInCurrentState([NotNull] ObjectCategory objectCategory);

		bool IsEditableInCurrentState([NotNull] IDdxDataset dataset,
		                              [CanBeNull] NotificationCollection notifications);
	}
}
