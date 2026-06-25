using System.Collections.Generic;

namespace GirderArrangements.Core.Geometry
{
    /// <summary>
    /// Boîte englobante alignée sur les axes absolus (AABB), telle que la renvoie
    /// UF_MODL_AskBoundingBox : double[6] = { xMin, yMin, zMin, xMax, yMax, zMax }. Helpers purs.
    /// </summary>
    public static class Aabb
    {
        /// <summary>Centre de l'AABB.</summary>
        public static Vec3 Center(double[] b)
            => new Vec3((b[0] + b[3]) / 2, (b[1] + b[4]) / 2, (b[2] + b[5]) / 2);

        /// <summary>Les 8 coins de l'AABB, en coordonnées absolues.</summary>
        public static IReadOnlyList<Vec3> Corners(double[] b)
        {
            return new[]
            {
                new Vec3(b[0], b[1], b[2]),
                new Vec3(b[3], b[1], b[2]),
                new Vec3(b[0], b[4], b[2]),
                new Vec3(b[3], b[4], b[2]),
                new Vec3(b[0], b[1], b[5]),
                new Vec3(b[3], b[1], b[5]),
                new Vec3(b[0], b[4], b[5]),
                new Vec3(b[3], b[4], b[5]),
            };
        }
    }
}
