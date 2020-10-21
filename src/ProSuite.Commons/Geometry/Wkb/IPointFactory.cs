namespace ProSuite.Commons.Geometry.Wkb
{
	public interface IPointFactory<out T>
	{
		T CreatePointXy(double x, double y);

		T CreatePointXyz(double x, double y, double z);

		T CreatePointXym(double x, double y, double m);

		T CreatePointXyzm(double x, double y, double z, double m);
	}
}
