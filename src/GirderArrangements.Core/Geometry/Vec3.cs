using System;

namespace GirderArrangements.Core.Geometry
{
    /// <summary>
    /// Vecteur 3D minimal, immuable et SANS dépendance NXOpen, pour que la logique de repère reste
    /// testable hors NX. La couche Nx convertit NXOpen.Point3d / Vector3d ↔ Vec3. Porté de CheckDistances.
    /// </summary>
    public readonly struct Vec3 : IEquatable<Vec3>
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Vec3(double x, double y, double z) { X = x; Y = y; Z = z; }

        public static readonly Vec3 Zero = new Vec3(0, 0, 0);

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, double s) => new Vec3(a.X * s, a.Y * s, a.Z * s);
        public static Vec3 operator *(double s, Vec3 a) => a * s;

        public double Dot(Vec3 o) => X * o.X + Y * o.Y + Z * o.Z;

        public Vec3 Cross(Vec3 o) =>
            new Vec3(Y * o.Z - Z * o.Y, Z * o.X - X * o.Z, X * o.Y - Y * o.X);

        public double Length => Math.Sqrt(Dot(this));

        /// <summary>Vecteur unitaire. Renvoie Zero si la norme est négligeable (évite la division par 0).</summary>
        public Vec3 Normalized()
        {
            var len = Length;
            return len < 1e-12 ? Zero : new Vec3(X / len, Y / len, Z / len);
        }

        public bool Equals(Vec3 o) => X == o.X && Y == o.Y && Z == o.Z;
        public override bool Equals(object obj) => obj is Vec3 o && Equals(o);
        public override int GetHashCode() => (X, Y, Z).GetHashCode();
        public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
    }
}
