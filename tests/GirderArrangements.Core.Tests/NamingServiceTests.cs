using GirderArrangements.Core;
using Xunit;

namespace GirderArrangements.Core.Tests
{
    public class NamingServiceTests
    {
        private readonly NamingService _n = new NamingService();

        [Theory]
        [InlineData("ARC12_POUTRE MOYENNE_01", 1)]
        [InlineData("ARC12_POUTRE COURTE_02", 2)]
        [InlineData("ARC12_POUTRE MOYENNE_03", 3)]
        [InlineData("ARC07_POUTRE LONGUE_12", 12)]
        public void Extracts_poutre_number(string name, int expected)
        {
            Assert.True(_n.IsPoutre(name));
            Assert.True(_n.TryExtractPoutreNumber(name, out var n));
            Assert.Equal(expected, n);
        }

        [Theory]
        [InlineData(1, "POUTRE 01")]
        [InlineData(3, "POUTRE 03")]
        [InlineData(12, "POUTRE 12")]
        public void Builds_arrangement_name(int number, string expected)
            => Assert.Equal(expected, _n.ArrangementName(number));

        [Fact]
        public void Builds_arrangement_name_from_poutre_name()
            => Assert.Equal("POUTRE 03", _n.ArrangementNameFor("ARC12_POUTRE MOYENNE_03"));

        [Fact]
        public void Poutre_without_number_has_no_arrangement_name()
        {
            Assert.False(_n.TryExtractPoutreNumber("ARC12_POUTRE MOYENNE", out _));
            Assert.Null(_n.ArrangementNameFor("ARC12_POUTRE MOYENNE"));
        }

        [Fact]
        public void Classifies_arc_components()
        {
            Assert.True(_n.IsSkeleton("V3631_ARC12_SQL"));
            Assert.True(_n.IsMagnetEnsemble("V3631_ARC12_AIMANTS"));
            Assert.True(_n.IsVacuumChamber("V3631_C12_ARC_CAV_EQUIPEE"));
            Assert.False(_n.IsPoutre("V3631_ARC12_AIMANTS"));
            Assert.False(_n.IsSkeleton("ARC12_POUTRE MOYENNE_01"));
        }
    }
}
