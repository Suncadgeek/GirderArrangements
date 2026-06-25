using System;
using System.Collections.Generic;

namespace GirderArrangements.Core.Geometry
{
    /// <summary>
    /// Boîte de sélection exprimée dans le REPÈRE LOCAL d'une poutre (X/Y transverses au faisceau,
    /// Z le long du faisceau). Construite à partir de l'emprise de la poutre (coins de son AABB projetés
    /// sur les axes locaux), élargie de <c>marginXY</c> de chaque côté en X et Y et de <c>marginZ</c> en Z.
    ///
    /// Un composant est « dans la boîte » si son centre approximatif (centre de bbox), transformé dans
    /// le repère local, tombe dans les bornes. Pur, testable (aucune dépendance NXOpen).
    /// </summary>
    public sealed class SelectionBox
    {
        private readonly Vec3 _origin;
        private readonly Vec3 _xu, _yu, _zu; // axes unitaires du repère local
        private readonly double _xMin, _xMax, _yMin, _yMax, _zMin, _zMax;

        private SelectionBox(Vec3 origin, Vec3 xu, Vec3 yu, Vec3 zu,
            double xMin, double xMax, double yMin, double yMax, double zMin, double zMax)
        {
            _origin = origin; _xu = xu; _yu = yu; _zu = zu;
            _xMin = xMin; _xMax = xMax; _yMin = yMin; _yMax = yMax; _zMin = zMin; _zMax = zMax;
        }

        /// <summary>Coordonnées d'un point absolu dans le repère local (projection sur les axes unitaires).</summary>
        public Vec3 ToLocal(Vec3 world)
        {
            var d = world - _origin;
            return new Vec3(d.Dot(_xu), d.Dot(_yu), d.Dot(_zu));
        }

        /// <summary>Vrai si le point absolu tombe dans la boîte locale (bornes déjà élargies des marges).</summary>
        public bool Contains(Vec3 world)
        {
            var l = ToLocal(world);
            return l.X >= _xMin && l.X <= _xMax
                && l.Y >= _yMin && l.Y <= _yMax
                && l.Z >= _zMin && l.Z <= _zMax;
        }

        /// <summary>
        /// Construit la boîte depuis un repère local (origine + axes, pas forcément unitaires) et les
        /// points d'emprise de la poutre (typiquement les 8 coins de son AABB absolue). Les bornes
        /// locales sont l'enveloppe des projections, élargies des marges.
        /// </summary>
        public static SelectionBox FromWorldExtent(Triad frame, IEnumerable<Vec3> extentPoints,
            double marginXY, double marginZ)
        {
            var xu = frame.X.Normalized();
            var yu = frame.Y.Normalized();
            var zu = frame.Z.Normalized();
            var o = frame.Origin;

            bool any = false;
            double xMin = 0, xMax = 0, yMin = 0, yMax = 0, zMin = 0, zMax = 0;
            foreach (var p in extentPoints)
            {
                var d = p - o;
                double lx = d.Dot(xu), ly = d.Dot(yu), lz = d.Dot(zu);
                if (!any)
                {
                    xMin = xMax = lx; yMin = yMax = ly; zMin = zMax = lz; any = true;
                }
                else
                {
                    if (lx < xMin) xMin = lx; if (lx > xMax) xMax = lx;
                    if (ly < yMin) yMin = ly; if (ly > yMax) yMax = ly;
                    if (lz < zMin) zMin = lz; if (lz > zMax) zMax = lz;
                }
            }

            double mXY = Math.Abs(marginXY);
            double mZ = Math.Abs(marginZ);
            return new SelectionBox(o, xu, yu, zu,
                xMin - mXY, xMax + mXY, yMin - mXY, yMax + mXY, zMin - mZ, zMax + mZ);
        }
    }
}
