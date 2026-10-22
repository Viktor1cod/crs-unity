using System.Linq;
using CRS.Core.Cars;
using NUnit.Framework;

namespace CRS.Tests
{
    public class BrandCatalogTests
    {
        [Test]
        public void HasFiveBrandsWithUniqueIds()
        {
            Assert.AreEqual(5, BrandCatalog.All.Count);
            Assert.AreEqual(5, BrandCatalog.All.Select(b => b.Id).Distinct().Count());
        }

        [Test]
        public void EveryBrandHasAtLeastTwoModels()
        {
            foreach (var brand in BrandCatalog.All)
                Assert.GreaterOrEqual(brand.Models.Count, 2, brand.Id);
        }

        [Test]
        public void ModelIdsAreUniqueAcrossCatalog()
        {
            var ids = BrandCatalog.All.SelectMany(b => b.Models).Select(m => m.Id).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count());
        }

        [Test]
        public void EveryNameHasBothLanguages()
        {
            foreach (var brand in BrandCatalog.All)
            {
                Assert.IsNotEmpty(brand.NameRu, brand.Id);
                Assert.IsNotEmpty(brand.NameEn, brand.Id);
                foreach (var model in brand.Models)
                {
                    Assert.IsNotEmpty(model.NameRu, model.Id);
                    Assert.IsNotEmpty(model.NameEn, model.Id);
                }
            }
        }

        [Test]
        public void TraitsAreValidProbabilities()
        {
            foreach (var b in BrandCatalog.All)
            {
                Assert.That(b.Reliability, Is.InRange(0.0, 1.0), b.Id);
                Assert.That(b.Liquidity, Is.InRange(0.0, 1.0), b.Id);
                Assert.That(b.OdometerFraudChance, Is.InRange(0.0, 1.0), b.Id);
                Assert.That(b.HiddenDefectChance, Is.InRange(0.0, 1.0), b.Id);
                Assert.Greater(b.PartsCostMultiplier, 0, b.Id);
            }
        }

        [Test]
        public void BrandLookupById()
        {
            Assert.AreEqual("Tayoka", BrandCatalog.Brand("tayoka").NameEn);
        }
    }
}
