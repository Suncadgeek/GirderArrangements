using NXOpen;
using GirderArrangements.Core.Geometry;

namespace GirderArrangements.Nx
{
    /// <summary>Conversions entre les primitives géométriques pures du Core et les types NXOpen.</summary>
    internal static class Conv
    {
        public static Vec3 ToVec(Point3d p) => new Vec3(p.X, p.Y, p.Z);
        public static Vec3 ToVec(Vector3d v) => new Vec3(v.X, v.Y, v.Z);
        public static Vec3 ToVec(double[] a) => new Vec3(a[0], a[1], a[2]);

        public static Point3d ToPoint(Vec3 v) => new Point3d(v.X, v.Y, v.Z);
        public static Vector3d ToVector(Vec3 v) => new Vector3d(v.X, v.Y, v.Z);

        /// <summary>Repère du Core depuis une origine et la matrice d'orientation d'un composant/CSYS NX.</summary>
        public static Triad ToTriad(Point3d origin, Matrix3x3 m)
        {
            return new Triad(
                ToVec(origin),
                new Vec3(m.Xx, m.Xy, m.Xz),
                new Vec3(m.Yx, m.Yy, m.Yz),
                new Vec3(m.Zx, m.Zy, m.Zz));
        }
    }
}
