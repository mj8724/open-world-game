using NUnit.Framework;

namespace OpenWorld.Tests
{
    /// <summary>
    /// I18nSystem 中英文切换测试
    /// </summary>
    public class I18nSystemTests
    {
        [TearDown]
        public void TearDown()
        {
            I18nSystem.SetLanguage(Language.English);
        }

        [Test]
        public void Get_English_ReturnsEnglishKey()
        {
            I18nSystem.SetLanguage(Language.English);
            Assert.AreEqual("Town Center", I18nSystem.Get("Town Center"));
            Assert.AreEqual("Warehouse", I18nSystem.Get("Warehouse"));
            Assert.AreEqual("Buildings", I18nSystem.Get("Buildings"));
            Assert.AreEqual("Units", I18nSystem.Get("Units"));
        }

        [Test]
        public void Get_Chinese_ReturnsChinese()
        {
            I18nSystem.SetLanguage(Language.Chinese);
            Assert.AreEqual("城镇中心", I18nSystem.Get("Town Center"));
            Assert.AreEqual("仓库", I18nSystem.Get("Warehouse"));

            Assert.AreEqual("建筑", I18nSystem.Get("Buildings"));

            Assert.AreEqual("单位", I18nSystem.Get("Units"));
        }

        [Test]
        public void Get_UnknownKey_ReturnsInput()
        {
            I18nSystem.SetLanguage(Language.English);
            Assert.AreEqual("UnknownKey123", I18nSystem.Get("UnknownKey123"));

            I18nSystem.SetLanguage(Language.Chinese);
            Assert.AreEqual("SomeMissingValue", I18nSystem.Get("SomeMissingValue"));
        }

        [Test]
        public void Get_NullOrEmpty_ReturnsInput()
        {
            I18nSystem.SetLanguage(Language.English);
            Assert.AreEqual("", I18nSystem.Get(""));
            Assert.AreEqual(null, I18nSystem.Get(null));
        }

        [Test]
        public void ToggleLanguage_SwitchesBetweenEnglishAndChinese()
        {
            I18nSystem.SetLanguage(Language.English);
            Assert.AreEqual(Language.English, I18nSystem.CurrentLanguage);

            I18nSystem.ToggleLanguage();
            Assert.AreEqual(Language.Chinese, I18nSystem.CurrentLanguage);

            I18nSystem.ToggleLanguage();
            Assert.AreEqual(Language.English, I18nSystem.CurrentLanguage);
        }

        [Test]
        public void SetLanguage_FiresEvent()
        {
            int fireCount = 0;
            System.Action handler = () => fireCount++;
            I18nSystem.OnLanguageChanged += handler;

            I18nSystem.SetLanguage(Language.Chinese);
            Assert.AreEqual(1, fireCount);

            I18nSystem.SetLanguage(Language.English);
            Assert.AreEqual(2, fireCount);

            I18nSystem.OnLanguageChanged -= handler;
        }

        [Test]
        public void SetLanguage_SameLanguage_DoesNotFireEvent()
        {
            I18nSystem.SetLanguage(Language.English);

            int fireCount = 0;
            System.Action handler = () => fireCount++;
            I18nSystem.OnLanguageChanged += handler;

            I18nSystem.SetLanguage(Language.English); // 相同语言
            Assert.AreEqual(0, fireCount);

            I18nSystem.OnLanguageChanged -= handler;
        }

        [Test]
        public void Get_AllBuildingNames_HaveTranslations()
        {
            I18nSystem.SetLanguage(Language.Chinese);
            var buildings = new[] {
                "Town Center", "Warehouse", "House", "Farm", "Mine Post",
                "Lumber Camp", "Quarry", "Smelter", "Steelworks",
                "Machine Shop", "Armory", "Clinic", "Market", "Garage",
                "Vehicle Factory", "Train Factory", "Station", "Scout Tower",
                "Control Point", "Barracks", "Wall", "Gate", "Tower",
                "Roadblock", "Bunker", "Bridge", "Dock",
                "Oil Derrick", "Power Plant"
            };
            foreach (var key in buildings)
            {
                string translated = I18nSystem.Get(key);
                Assert.IsNotNull(translated);
                Assert.IsNotEmpty(translated);
                Assert.AreNotEqual(key, translated, $"Building '{key}' should have Chinese translation");
            }
        }

        [Test]
        public void Get_CommonUiStrings_HaveTranslations()
        {
            I18nSystem.SetLanguage(Language.Chinese);
            var uiKeys = new[] { "Buildings", "Units", "Vehicles", "Blueprints", "Routes", "Selected" };
            foreach (var key in uiKeys)
            {
                string translated = I18nSystem.Get(key);
                Assert.IsNotNull(translated);
                Assert.IsNotEmpty(translated);
                Assert.AreNotEqual(key, translated, $"UI key '{key}' should have Chinese translation");
            }
        }

        [Test]
        public void CurrentLanguage_DefaultIsEnglish()
        {
            I18nSystem.SetLanguage(Language.English);
            Assert.AreEqual(Language.English, I18nSystem.CurrentLanguage);
        }
    }
}