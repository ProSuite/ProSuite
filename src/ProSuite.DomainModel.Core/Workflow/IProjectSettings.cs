namespace ProSuite.DomainModel.Core.Workflow
{
	/// <summary>
	/// Narrow focus interface for project settings to be implemented both by dedicated classes such as
	/// <see cref="ProjectSettings"/> but also higher-level project representations.
	/// </summary>
	public interface IProjectSettings
	{
		/// <summary>
		/// The project ID for equality comparisons.
		/// </summary>
		int Id { get; }

		/// <summary>
		/// The project's short name (for display purposes).
		/// </summary>
		string ShortName { get; }

		/// <summary>
		/// The project's name (for display purposes).
		/// </summary>
		string Name { get; }

		/// <summary>
		/// XMin coordinate of the full extent of the project's data area or double.NaN if not set.
		/// </summary>
		double? FullExtentXMin { get; set; }

		/// <summary>
		/// YMin coordinate of the full extent of the project's data area or double.NaN if not set.
		/// </summary>
		double? FullExtentYMin { get; set; }

		/// <summary>
		/// XMax coordinate of the full extent of the project's data area or double.NaN if not set.
		/// </summary>
		double? FullExtentXMax { get; set; }

		/// <summary>
		/// YMax coordinate of the full extent of the project's data area or double.NaN if not set.
		/// </summary>
		double? FullExtentYMax { get; set; }

		/// <summary>
		/// The minimum scale denominator that is still useful for map displays showing the
		/// project's data.
		/// </summary>
		double MinimumScaleDenominator { get; set; }

		/// <summary>
		/// The configuration directory for work list settings.
		/// </summary>
		string WorkListConfigDirectory { get; set; }

		/// <summary>
		/// The configuration directory for attribute editor settings and layout files.
		/// </summary>
		string AttributeEditorConfigDirectory { get; set; }

		/// <summary>
		/// The directory of the central tool defaults.
		/// </summary>
		string ToolConfigDirectory { get; set; }
	}
}
