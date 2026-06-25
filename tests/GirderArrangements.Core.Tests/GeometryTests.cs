using GirderArrangements.Core.Geometry;
using Xunit;

namespace GirderArrangements.Core.Tests
{
    public class GeometryTests
    {
        private static Triad AxisAligned()
            => new Triad(Vec3.Zero, new Vec3(1, 0, 0), new Vec3(0, 1, 0), new Vec3(0, 0, 1));

        // Emprise d'une poutre : AABB [-100,-50,-10 ; 100,50,10].
        private static double[] BeamBox => new double[] { -100, -50, -10, 100, 50, 10 };

        [Fact]
        public void Aabb_center_and_corners()
        {
            Assert.Equal(new Vec3(0, 0, 0), Aabb.Center(BeamBox));
            Assert.Equal(8, new System.Collections.Generic.List<Vec3>(Aabb.Corners(BeamBox)).Count);
        }

        [Fact]
        public void Box_expands_xy_by_margin_keeps_z()
        {
            var box = SelectionBox.FromWorldExtent(AxisAligned(), Aabb.Corners(BeamBox), marginXY: 700, marginZ: 0);

            Assert.True(box.Contains(new Vec3(0, 0, 5)));      // dans la poutre
            Assert.True(box.Contains(new Vec3(700, 600, 0)));  // dans le débordement X/Y (≤ 100+700, ≤ 50+700)
            Assert.False(box.Contains(new Vec3(900, 0, 0)));   // hors X (> 100+700)
            Assert.False(box.Contains(new Vec3(0, 0, 50)));    // hors Z (marge Z = 0)
        }

        [Fact]
        public void Box_respects_local_frame_orientation()
        {
            // Repère tourné de 90° autour de Z : X local = Y monde, Y local = -X monde.
            var frame = new Triad(Vec3.Zero, new Vec3(0, 1, 0), new Vec3(-1, 0, 0), new Vec3(0, 0, 1));
            var box = SelectionBox.FromWorldExtent(frame, Aabb.Corners(BeamBox), marginXY: 50, marginZ: 0);

            // Emprise locale : X local ∈ [-50,50] (=Y monde), Y local ∈ [-100,100] (=-X monde), +50 de marge.
            Assert.True(box.Contains(new Vec3(0, 100, 0)));   // Y monde 100 → X local 100 ≤ 50+50
            Assert.False(box.Contains(new Vec3(0, 150, 0)));  // Y monde 150 → X local 150 > 100
        }
    }
}
