namespace GirderArrangements.Core.Geometry
{
    /// <summary>
    /// Repère orthonormé (X, Y, Z) avec une origine — équivalent neutre d'un CSYS NX ou de la position
    /// d'un composant (origin + matrice d'orientation). Porté de CheckDistances / X-Section.
    /// </summary>
    public readonly struct Triad
    {
        public Vec3 Origin { get; }
        public Vec3 X { get; }
        public Vec3 Y { get; }
        public Vec3 Z { get; }

        public Triad(Vec3 origin, Vec3 x, Vec3 y, Vec3 z)
        {
            Origin = origin; X = x; Y = y; Z = z;
        }
    }
}
