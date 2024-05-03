namespace ProSuite.QA.Tests.ParameterTypes
{
	/// <summary>
	/// Defines geometry components that can be addressed in tests
	/// </summary>
	public enum GeometryComponent
	{
		EntireGeometry = 0,
		Boundary = 1,
		Vertices = 2,
		LineEndPoints = 3,
		LineStartPoint = 4,
		LineEndPoint = 5,
		Centroid = 6,
		LabelPoint = 7,
		InteriorVertices = 8
	}
}
