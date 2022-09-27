
using NUnit.Framework;
using DotSpatial.Projections;

namespace DotSpatial.Projections.Tests.Projected
{
    /// <summary>
    /// This class contains all the tests for the WorldSpheroid category of Projected coordinate systems
    /// </summary>
    [TestFixture]
    public class WorldSpheroid
    {
        /// <summary>
        /// Creates a new instance of the Africa Class
        /// </summary>
        [OneTimeSetUp]
        public void Initialize()
        {
            
        }

        [Test]
        [Ignore("ignored")]
        public void Aitoffsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.Aitoffsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        [Ignore("ignored")]
        public void Behrmannsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.Behrmannsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        public void Bonnesphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.Bonnesphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        [Ignore("ignored")]
        public void CrasterParabolicsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.CrasterParabolicsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        public void CylindricalEqualAreasphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.CylindricalEqualAreasphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        [Ignore("ignored")]
        public void EckertIIIsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.EckertIIIsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        [Ignore("ignored")]
        public void EckertIIsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.EckertIIsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        [Ignore("ignored")]
        public void EckertIsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.EckertIsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        public void EckertIVsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.EckertIVsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        [Ignore("ignored")]
        public void EckertVIsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.EckertVIsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        [Ignore("ignored")]
        public void EckertVsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.EckertVsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        public void EquidistantConicsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.EquidistantConicsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        public void EquidistantCylindricalsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.EquidistantCylindricalsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        [Ignore("ignored")]
        public void FlatPolarQuarticsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.FlatPolarQuarticsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        public void GallStereographicsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.GallStereographicsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        [Ignore("ignored")]
        public void HammerAitoffsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.HammerAitoffsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        [Ignore("ignored")]
        public void Loximuthalsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.Loximuthalsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        public void Mercatorsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.Mercatorsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        public void MillerCylindricalsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.MillerCylindricalsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        public void Mollweidesphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.Mollweidesphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        [Ignore("ignored")]
        public void PlateCarreesphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.PlateCarreesphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        public void Polyconicsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.Polyconicsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        [Ignore("ignored")]
        public void QuarticAuthalicsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.QuarticAuthalicsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        public void Robinsonsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.Robinsonsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        public void Sinusoidalsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.Sinusoidalsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        [Ignore("ignored")]
        public void Timessphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.Timessphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        public void VanderGrintenIsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.VanderGrintenIsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        [Ignore("ignored")]
        public void VerticalPerspectivesphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.VerticalPerspectivesphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        [Ignore("ignored")]
        public void WinkelIIsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.WinkelIIsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        [Ignore("ignored")]
        public void WinkelIsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.WinkelIsphere;
            Tester.TestProjection(pStart);
        }


        [Test]
        [Ignore("ignored")]
        public void WinkelTripelNGSsphere()
        {
            ProjectionInfo pStart = KnownCoordinateSystems.Projected.WorldSpheroid.WinkelTripelNGSsphere;
            Tester.TestProjection(pStart);
        }
    }
}