using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace OpenWorld
{
    public enum Language
    {
        English,
        Chinese
    }

    public static class I18nSystem
    {
        public static Language CurrentLanguage { get; private set; } = Language.Chinese;

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
            { "Tool", "工具" },

            // UI Categories & Labels
            { "Civilian", "民政" },
            { "Industry", "工业" },
            { "Logistics", "物流" },
            { "Military", "军事" },
            { "Infrastructure", "基建" },
            { "Blueprints", "蓝图" },
            { "Road", "公路" },
            { "Rail", "铁路" },
            { "Dig", "挖坑" },
            { "Fill", "填土" },
            { "Flatten", "平整" },
            { "Trench", "战壕" },
            { "Mine", "采矿" },
            
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
            { "Production idle", "生产闲置" },

            // Pause Menu
            { "PAUSED", "已暂停" },
            { "Resume Game", "继续游戏" },
            { "Exit Game", "退出游戏" },
            { "Language / 语言", "语言" },

            // Resource Bar (top HUD)
            { "Supply", "补给" },
            { "War Stock", "战备" },
            { "Food", "粮食" },
            { "Wood", "木材" },
            { "Stone", "石材" },
            { "Ore", "矿石" },
            { "Coal", "煤炭" },
            { "Iron", "生铁" },
            { "Steel", "钢材" },
            { "Parts", "零件" },
            { "Powder", "火药" },
            { "Fuel", "燃料" },
            { "Ammo", "弹药" },

            // Ops Tabs
            { "Overview", "总览" },
            { "Transport", "运输" },
            { "Geology", "地质" },
            { "Population", "人口" },

            // Overview KPI / Stat Names
            { "FPS", "帧率" },
            { "Map", "地图" },
            { "Entities", "实体" },
            { "Current Command", "当前指令" },
            { "City Systems", "城市系统" },
            { "Field Operations", "野战行动" },
            { "Strategic Goal", "战略目标" },

            // Command Network panel
            { "Command Network", "指挥网络" },
            { "Open World Surface", "开放世界地表" },
            { "Save", "保存" },
            { "Cancel All", "全部取消" },
            { "Cancel All Blueprints", "取消全部蓝图" },
            { "Blueprint Queue", "蓝图队列" },

            // Operational panel titles
            { "Geology & Mining", "地质与采矿" },
            { "Population & Medical", "人口与医疗" },
            { "Military & Intel", "军事与情报" },
            { "Diplomacy & Trade", "外交与贸易" },

            // Build deck
            { "Construction Command", "建造指挥" },
            { "Civilian deck: housing, food, storage, trade, and command", "民政区：住房、食物、仓储、贸易与指挥" },
            { "Industry deck: extraction, smelting, power, and manufacturing", "工业区：开采、冶炼、电力与制造" },
            { "Logistics deck: depots, vehicles, rail hubs, docks, and bridges", "物流区：仓库、车辆、铁路枢纽、码头与桥梁" },
            { "Military deck: defense lines, vision, barracks, and fortifications", "军事区：防线、视野、兵营与工事" },
            { "Infrastructure deck: terrain tools, roads, rails, bridges, and mines", "基建区：地形工具、公路、铁路、桥梁与矿场" },
            { "Establish food, housing, storage, and command capacity before heavy expansion.", "在大规模扩张前，先建立食物、住房、仓储与指挥能力。" },
            { "Prioritize upstream inputs first: ore, coal, smelting, then advanced factories.", "优先建设上游产能：矿石、煤炭、冶炼，再发展高级工厂。" },
            { "Build storage and vehicle infrastructure before scaling production throughput.", "在提升产能吞吐前，先建设仓储与车辆基础设施。" },
            { "Place defensive assets near routes, chokepoints, and exposed production chains.", "在路线、隘口与暴露的产业链附近布置防御设施。" },
            { "Issue terrain commands directly. Brush size and queue behavior follow current input modifiers.", "直接下达地形指令。笔刷大小与队列行为跟随当前输入修饰键。" },

            // Strategic Map
            { "Strategic Map", "战略地图" },
            { "Overlay", "图层" },
            { "Camera", "镜头" },
            { "Scout", "侦察" },
            { "Survey", "勘探" },
            { "Drill", "钻探" },

            // Long help / hotkey strings
            { "1-9 tools  B build  F1-F8 buildings  V vehicle  L/U load-unload  M map  O overlay  X+click cancel  +/- priority  Shift+click blueprint  F9 save", "1-9 工具  B 建造  F1-F8 建筑  V 车辆  L/U 装卸  M 地图  O 图层  X+点击 取消  +/- 优先级  Shift+点击 蓝图  F9 存档" },
            { "M toggle / O overlay / wheel zoom / drag pan / click jump", "M 开关 / O 图层 / 滚轮缩放 / 拖动平移 / 点击跳转" },
            { "M toggle / O overlay / wheel zoom / drag pan / click jump  T language", "M 开关 / O 图层 / 滚轮缩放 / 拖动平移 / 点击跳转  T 语言" },

            // Strategic Map dynamic words
            { "Zoom", "缩放" },
            { "Regions", "地区" },
            { "Sites", "据点" },

            // Strategic Map legends
            { "Legend: black unknown / dim explored / bright visible. White frame is camera view; icons mark known units, buildings, sites, routes.", "图例：黑=未知 / 暗=已探索 / 亮=可见。白框为镜头视野；图标标记已知单位、建筑、据点与路线。" },
            { "Legend: blue player / red enemy / yellow neutral / green ally. Click mode sets camera, scout, or blueprint marks.", "图例：蓝=玩家 / 红=敌方 / 黄=中立 / 绿=盟友。点击模式可设置镜头、侦察或蓝图标记。" },
            { "Legend: dim suspected / colored surveyed / bright drilled / gray exhausted. Survey, drill and mine modes create geology commands.", "图例：暗=疑似 / 彩=已勘探 / 亮=已钻探 / 灰=已枯竭。勘探、钻探与采矿模式可生成地质指令。" },
            { "Legend: orange iron / dark coal-oil / green food-wood / gray stone. Icons show known sites and units.", "图例：橙=铁矿 / 深色=煤油 / 绿=粮木 / 灰=石材。图标显示已知据点与单位。" },
            { "Legend: tan road / bright rail / brown bridge / yellow supply route. Vehicles use roads and rails faster.", "图例：棕褐=公路 / 亮=铁路 / 棕=桥梁 / 黄=补给线。车辆在公路与铁路上行进更快。" },
            { "Legend: route lines show active logistics; route status and bottlenecks are listed in HUD Logistics.", "图例：线路表示进行中的物流；线路状态与瓶颈在 HUD 物流面板中列出。" },
            { "Legend: route and vehicle icons identify no stock, no fuel, no idle vehicle, or no path bottlenecks.", "图例：线路与车辆图标标识缺货、缺燃料、无空闲车辆或无路径等瓶颈。" },
            { "Legend: red icons are visible or last-known enemy positions; fog hides unknown enemies.", "图例：红色图标为可见或最后已知的敌方位置；战争迷雾隐藏未知敌人。" },
            { "Legend: cyan active blueprint / yellow paused / map click can place road, rail, or bridge marks.", "图例：青=进行中蓝图 / 黄=已暂停。地图点击可放置公路、铁路或桥梁标记。" },
            { "Legend: green high morale / red pressure. HUD shows wounded, medicine, fatigue and morale.", "图例：绿=高士气 / 红=压力。HUD 显示伤员、药品、疲劳与士气。" }
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

        /// <summary>
        /// 遍历整棵 UI 树，把所有 TextElement（Label/Button 等）的静态文本本地化。
        /// 首次遇到某元素时记录其原始（英文）文本作为翻译 key，之后按当前语言翻译。
        /// 动态数值标签的原值查不到字典会原样返回，且随后会被各控制器的刷新逻辑覆盖，无副作用。
        /// 初始化与语言切换时各调用一次即可。
        /// </summary>
        public static void LocalizeTree(VisualElement root)
        {
            if (root == null) return;
            root.Query<TextElement>().ForEach(el =>
            {
                if (el.userData is not string key)
                {
                    key = el.text;
                    el.userData = key;
                }
                el.text = Get(key);
            });
        }
    }
}
