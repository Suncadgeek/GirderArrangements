using System.Collections.Generic;
using GirderArrangements.Core.Geometry;
using GirderArrangements.Core.Partition;
using Xunit;

namespace GirderArrangements.Core.Tests
{
    public class PartitionTests
    {
        private static Triad AxisAligned()
            => new Triad(Vec3.Zero, new Vec3(1, 0, 0), new Vec3(0, 1, 0), new Vec3(0, 0, 1));

        private static BeamSlot Beam(int id, double[] aabb)
            => new BeamSlot
            {
                Id = id,
                Center = Aabb.Center(aabb),
                Box = SelectionBox.FromWorldExtent(AxisAligned(), Aabb.Corners(aabb), 700, 0)
            };

        private static LeafItem Leaf(int id, double z) => new LeafItem { Id = id, Center = new Vec3(0, 0, z) };

        [Fact]
        public void Assigns_each_leaf_to_containing_beam()
        {
            var beams = new List<BeamSlot>
            {
                Beam(0, new double[] { -10, -10, -100, 10, 10, 300 }), // Z [-100,300], centre z=100
                Beam(1, new double[] { -10, -10, 200, 10, 10, 600 }),  // Z [200,600], centre z=400
            };
            var leaves = new List<LeafItem>
            {
                Leaf(0, 50),    // beam0 seulement
                Leaf(1, 500),   // beam1 seulement
                Leaf(2, 1000),  // aucune
            };

            var res = BeamPartitioner.Assign(beams, leaves);

            Assert.Equal(0, res.LeafToBeam[0]);
            Assert.Equal(1, res.LeafToBeam[1]);
            Assert.Contains(2, res.Unassigned);
            Assert.Equal(new List<int> { 0 }, res.BeamToLeaves[0]);
            Assert.Equal(new List<int> { 1 }, res.BeamToLeaves[1]);
        }

        [Fact]
        public void Overlap_goes_to_nearest_beam_center()
        {
            var beams = new List<BeamSlot>
            {
                Beam(0, new double[] { -10, -10, -100, 10, 10, 300 }), // centre z=100
                Beam(1, new double[] { -10, -10, 200, 10, 10, 600 }),  // centre z=400
            };
            // z=260 est dans les deux boîtes ; plus proche de beam1 (|260-400|=140 < |260-100|=160).
            var res = BeamPartitioner.Assign(beams, new List<LeafItem> { Leaf(0, 260) });

            Assert.Equal(1, res.LeafToBeam[0]);
        }
    }
}
