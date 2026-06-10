using System;
using System.Collections.Generic;

namespace OpenWorld
{
    public enum Language
    {
        English,
        Chinese
    }

    public static class I18nSystem
    {
        public static Language CurrentLanguage { get; private set; } = Language.English;

        public static event Action OnLanguageChanged;

        private static readonly Dictionary<string, string> _zhCN = new Dictionary<string, string>
        {
            // Building Names
            { "Town Center", "城镇中心" },
            { "Warehouse", "仓库" },
            { "House", "房屋" },
            { "Farm", "农场" },
            { "Mine Post", "矿站" },
            { "Lumber Camp", "伐木场" },
            { "Quarry", "采石场" },
            { "Smelter", "冶炼厂" },
            { "Steelworks", "钢铁厂" },
            { "Machine Shop", "机械厂" },
            { "Armory", "兵工厂" },
            { "Clinic", "诊所" },
            { "Market", "市场" },
            { "Garage", "车库" },
            { "Vehicle Factory", "车辆厂" },
            { "Train Factory", "火车厂" },
            { "Station", "火车站" },
            { "Scout Tower", "侦察塔" },
            { "Control Point", "控制点" },
            { "Barracks", "兵营" },
            { "Wall", "城墙" },
            { "Gate", "城门" },
            { "Tower", "防御塔" },
            { "Roadblock", "路障" },
            { "Bunker", "碉堡" },
            { "Bridge", "桥梁" },
            { "Dock", "码头" },
            { "Oil Derrick", "油井" },
            { "Power Plant", "发电站" },

            // UI Categories & Labels
            { "Civilian", "民政" },
            { "Industry", "工业" },
            { "Logistics", "物流" },
            { "Military", "军事" },
            { "Infrastructure", "基础设施" },
            { "Road", "公路" },
            { "Rail", "铁路" },
            { "Dig", "挖坑" },
            { "Fill", "填土" },
            { "Flatten", "平整" },
            { "Trench", "战壕" },
            { "Mine", "地雷" },
            
            // Resources
            { "W:", "木:" },
            { "S:", "石:" },
            { "Ore:", "矿:" },
            { "F:", "粮:" },
            { "D:", "土:" },
            { "Fe:", "铁:" },
            { "St:", "钢:" },
            { "P:", "零:" },
            
            // Status HUD Labels
            { "Buildings", "建筑" },
            { "Units", "单位" },
            { "Vehicles", "车辆" },
            { "Blueprints", "蓝图" },
            { "Routes", "路线" },
            { "Selected", "选中" },
            { "brush", "笔刷大小" },
            { "build", "建造" },
            { "vehicle", "车辆" },
            { "Pop", "人口" },
            { "Workers", "工人" },
            { "Soldiers", "士兵" },
            { "Wounded", "伤员" },
            { "Morale", "士气" },
            { "Era", "时代" },
            { "Research", "研究" },
            { "Production", "生产" },
            { "Explored", "已探索" },
            { "Visible", "可见" },
            { "Unification", "统一进度" },
            { "Pressure", "压力" },
            { "Diplomacy", "外交" },
            { "Stable", "稳定" },
            { "Production idle", "生产闲置" }
        };

        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;

            if (CurrentLanguage == Language.Chinese)
            {
                if (_zhCN.TryGetValue(key, out string translated))
                {
                    return translated;
                }
            }

            return key; // Default fallback to original string (English)
        }

        public static void SetLanguage(Language lang)
        {
            if (CurrentLanguage != lang)
            {
                CurrentLanguage = lang;
                OnLanguageChanged?.Invoke();
            }
        }

        public static void ToggleLanguage()
        {
            SetLanguage(CurrentLanguage == Language.English ? Language.Chinese : Language.English);
        }
    }
}
