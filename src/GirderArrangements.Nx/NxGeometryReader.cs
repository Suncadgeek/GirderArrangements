using System;
using System.Collections.Generic;
using NXOpen;
using GirderArrangements.Core;
using GirderArrangements.Core.Geometry;
using Assemblies = NXOpen.Assemblies;

namespace GirderArrangements.Nx
{
    /// <summary>
    /// Géométrie d'appoint : boîte englobante combinée d'un composant (centre + AABB absolue) via
    /// UF_MODL_AskBoundingBox, et repère local d'un composant via Component.GetPosition. Repris du
    /// patron NxCollisionFinder (CheckDistances).
    /// </summary>
    public sealed class NxGeometryReader
    {
        private readonly NxContext _ctx;
        private readonly IBuildLog _log;

        public NxGeometryReader(NxContext ctx, IBuildLog log)
        {
            _ctx = ctx;
            _log = log ?? NullBuildLog.Instance;
        }

        /// <summary>
        /// AABB absolue combinée des corps du composant (et de ses descendants).
        /// box = { xMin, yMin, zMin, xMax, yMax, zMax }. False si aucun corps.
        /// </summary>
        public bool TryBoundingBox(Assemblies.Component comp, out double[] box)
        {
            box = null;
            var bodies = new List<Body>();
            CollectBodies(comp, bodies);
            if (bodies.Count == 0) return false;
            box = CombinedBox(bodies);
            return true;
        }

        /// <summary>Centre (centre de l'AABB combinée) du composant. False si aucun corps.</summary>
        public bool TryCenter(Assemblies.Component comp, out Vec3 center)
        {
            center = Vec3.Zero;
            if (!TryBoundingBox(comp, out var box)) return false;
            center = Aabb.Center(box);
            return true;
        }

        /// <summary>
        /// Repère local du composant (origine + axes) tel que positionné dans l'assemblage.
        /// </summary>
        public Triad LocalFrame(Assemblies.Component comp)
        {
            Point3d origin;
            Matrix3x3 orient;
            comp.GetPosition(out origin, out orient);
            return Conv.ToTriad(origin, orient);
        }

        // ---- corps + boîtes ----

        private void CollectBodies(Assemblies.Component comp, List<Body> acc)
        {
            if (comp == null) return;
            var proto = comp.Prototype as Part;
            if (proto != null)
            {
                foreach (Body pb in proto.Bodies)
                {
                    try
                    {
                        var occ = comp.FindOccurrence(pb) as Body;
                        if (occ != null) acc.Add(occ);
                    }
                    catch { /* corps non instanciable → ignoré */ }
                }
            }
            foreach (var ch in comp.GetChildren()) CollectBodies(ch, acc);
        }

        private double[] CombinedBox(List<Body> bodies)
        {
            double[] acc = null;
            foreach (var b in bodies)
            {
                double[] box;
                try
                {
                    box = new double[6];
                    _ctx.Uf.Modl.AskBoundingBox(b.Tag, box);
                }
                catch { continue; }
                if (acc == null) { acc = (double[])box.Clone(); continue; }
                for (int i = 0; i < 3; i++) acc[i] = Math.Min(acc[i], box[i]);
                for (int i = 3; i < 6; i++) acc[i] = Math.Max(acc[i], box[i]);
            }
            return acc ?? new double[] { 0, 0, 0, 0, 0, 0 };
        }
    }
}
