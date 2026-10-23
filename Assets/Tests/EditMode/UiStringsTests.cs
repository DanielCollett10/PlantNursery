using NUnit.Framework;

namespace PlantNursery.Game.Tests
{
    public class UiStringsTests
    {
        [Test]
        public void ParseReadsEntries()
        {
            var map = UiStrings.Parse("{\"entries\":[{\"key\":\"a\",\"value\":\"x {0}\"}]}");
            Assert.AreEqual("x {0}", map["a"]);
        }

        [Test]
        public void TableHasNeighbourToastAndFormatsDay()
        {
            Assert.AreEqual("A neighbour left you some seeds.", UiStrings.Get("toast.neighbourGift"));
            Assert.AreEqual("Day 3", UiStrings.Get("hud.day", 3));
            Assert.AreEqual("missing.key", UiStrings.Get("missing.key"));
        }
    }
}
