namespace ProSuite.DomainModel.Core.Workflow
{
	/// <summary>
	/// Encapsulates the settings of a project without the actual data model (which might or might
	/// not be provided by implementations).
	/// </summary>
	public class ProjectSettings : IProjectSettings
	{
		public ProjectSettings(int id, string shortName, string name)
		{
			Id = id;
			ShortName = shortName;
			Name = name;
		}

		#region Implementation of IProjectSettings

		/// <summary>
		/// The data dictionary project ID.
		/// </summary>
		public int Id { get; }

		/// <summary>
		/// The short name of the project, e.g. "TLM".
		/// </summary>
		public string ShortName { get; }

		/// <summary>
		/// The name of the project, e.g. "Topographic Landscape Model".
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// XMin coordinate of the full extent of the project's data area or double.NaN if not set.
		/// </summary>
		public double? FullExtentXMin { get; set; }

		/// <summary>
		/// YMin coordinate of the full extent of the project's data area or double.NaN if not set.
		/// </summary>
		public double? FullExtentYMin { get; set; }

		/// <summary>
		/// XMax coordinate of the full extent of the project's data area or double.NaN if not set.
		/// </summary>
		public double? FullExtentXMax { get; set; }

		/// <summary>
		/// YMax coordinate of the full extent of the project's data area or double.NaN if not set.
		/// </summary>
		public double? FullExtentYMax { get; set; }

		/// <summary>
		/// The minimum scale denominator that is still useful for map displays showing the
		/// project's data.
		/// </summary>
		public double MinimumScaleDenominator { get; set; }

		/// <summary>
		/// The configuration directory for work list settings.
		/// </summary>
		public string WorkListConfigDirectory { get; set; }

		/// <summary>
		/// The configuration directory for attribute editor settings and layout files.
		/// </summary>
		public string AttributeEditorConfigDirectory { get; set; }

		/// <summary>
		/// The directory of the central tool defaults.
		/// </summary>
		public string ToolConfigDirectory { get; set; }

		#endregion

		/// <summary>
		/// Sets the full extent coordinates, ensuring they form a valid extent. If the extent is
		/// not valid (area smaller or equal 0), all extent values will be set to null.
		/// </summary>
		/// <param name="xMin">The minimum X coordinate</param>
		/// <param name="yMin">The minimum Y coordinate</param>
		/// <param name="xMax">The maximum X coordinate</param>
		/// <param name="yMax">The maximum Y coordinate</param>
		public void SetFullExtent(double xMin, double yMin, double xMax, double yMax)
		{
			bool isValidExtent = xMin <= xMax && yMin <= yMax;

			if (isValidExtent)
			{
				FullExtentXMin = xMin;
				FullExtentYMin = yMin;
				FullExtentXMax = xMax;
				FullExtentYMax = yMax;
			}
			else
			{
				FullExtentXMin = null;
				FullExtentYMin = null;
				FullExtentXMax = null;
				FullExtentYMax = null;
			}
		}
	}
}
