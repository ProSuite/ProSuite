using System.Collections.Generic;

namespace ProSuite.Commons.Geom
{
    public interface ICoordinatesCollection<out T> where T : ICoordinates
    {
        IEnumerable<T> GetCoordinates();

        IEnumerable<T> GetCoordinates(IBoundedXY withinEnvelope);

        IEnumerable<T> GetCoordinates(MultiPolycurve withinPolygon);
    }
}
