using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace PlantNursery.Sim.Content
{
    /// <summary>
    /// Parses and validates content JSON into a <see cref="ContentDb"/>.
    /// Takes JSON strings keyed by file name so the sim does no file IO (decision 0004).
    /// </summary>
    public static class ContentLoader
    {
        /// <summary>File names the loader requires.</summary>
        public const string TimeFile = "time.json", TimeDemoFile = "time.demo.json", ItemsFile = "items.json",
            ContainersFile = "containers.json", VarietiesFile = "varieties.json", GenesFile = "genes.json",
            EconomyFile = "economy.json", BuildingFile = "building.json";

        /// <summary>Loads and validates all content. time.demo.json is optional and overrides time.json values.</summary>
        /// <exception cref="ContentException">On malformed JSON, missing files/fields, unknown ids or negative values.</exception>
        public static ContentDb Load(IDictionary<string, string> filesByName)
        {
            if (filesByName == null) throw new ArgumentNullException(nameof(filesByName));

            var time = LoadTime(filesByName);
            var items = LoadItems(Root(filesByName, ItemsFile));
            var genes = LoadGenes(Root(filesByName, GenesFile));
            var containers = LoadContainers(Root(filesByName, ContainersFile), items);
            var varieties = LoadVarieties(Root(filesByName, VarietiesFile), genes);
            var economy = LoadEconomy(Root(filesByName, EconomyFile), items, varieties);
            var building = LoadBuilding(Root(filesByName, BuildingFile));
            return new ContentDb(time, items, containers, varieties, genes, economy, building);
        }

        private static ContentNode Root(IDictionary<string, string> files, string name)
        {
            if (!files.TryGetValue(name, out var json))
                throw new ContentException(name, "", "required content file is missing");
            return ContentNode.ParseRoot(name, json);
        }

        private static TimeConfig LoadTime(IDictionary<string, string> files)
        {
            var node = Root(files, TimeFile);
            ContentNode effective = node;
            if (files.TryGetValue(TimeDemoFile, out var demoJson))
            {
                var demo = ContentNode.ParseRoot(TimeDemoFile, demoJson);
                var merged = (JObject)node.Raw.DeepClone();
                foreach (var key in demo.Keys())
                {
                    if (!merged.ContainsKey(key))
                        throw demo.Error(key, "unknown time field (not present in time.json)");
                    merged[key] = demo.Raw[key].DeepClone();
                }
                // Validate the demo file's own fields first so errors name time.demo.json.
                ValidateTime(ContentNode.FromObject(TimeDemoFile, merged), onlyKeys: new HashSet<string>(demo.Keys()));
                effective = ContentNode.FromObject(TimeFile, merged);
            }
            return ValidateTime(effective, null);
        }

        private static TimeConfig ValidateTime(ContentNode n, HashSet<string> onlyKeys)
        {
            var t = new TimeConfig();
            bool Check(string k) => onlyKeys == null || onlyKeys.Contains(k);
            // Reading every field validates it; for the demo pre-pass only demo keys are read.
            if (Check("dayStartMinute")) t.DayStartMinute = n.Int("dayStartMinute");
            if (Check("nightStartMinute")) t.NightStartMinute = n.Int("nightStartMinute");
            if (Check("realSecondsDaylight")) t.RealSecondsDaylight = n.Float("realSecondsDaylight");
            if (Check("realSecondsNight")) t.RealSecondsNight = n.Float("realSecondsNight");
            if (Check("seasonLengthDays")) t.SeasonLengthDays = n.Int("seasonLengthDays");
            if (Check("shopOpenMinute")) t.ShopOpenMinute = n.Int("shopOpenMinute");
            if (Check("shopCloseMinute")) t.ShopCloseMinute = n.Int("shopCloseMinute");
            if (onlyKeys != null) return t;

            if (t.DayStartMinute > 1439) throw n.Error("dayStartMinute", "must be below 1440");
            if (t.NightStartMinute > 1439) throw n.Error("nightStartMinute", "must be below 1440");
            if (t.NightStartMinute <= t.DayStartMinute + 300)
                throw n.Error("nightStartMinute", "must be more than 300 minutes after dayStartMinute");
            if (t.RealSecondsDaylight <= 0f) throw n.Error("realSecondsDaylight", "must be greater than 0");
            if (t.RealSecondsNight <= 0f) throw n.Error("realSecondsNight", "must be greater than 0");
            if (t.SeasonLengthDays < 1) throw n.Error("seasonLengthDays", "must be at least 1");
            if (t.ShopOpenMinute > 1439) throw n.Error("shopOpenMinute", "must be below 1440");
            if (t.ShopCloseMinute > 1439) throw n.Error("shopCloseMinute", "must be below 1440");
            return t;
        }

        private static Dictionary<string, ItemDef> LoadItems(ContentNode root)
        {
            var result = new Dictionary<string, ItemDef>();
            foreach (var n in root.ObjArray("items"))
            {
                var def = new ItemDef
                {
                    Id = n.Str("id"),
                    Name = n.Str("name"),
                    Layer = n.Enum<ItemLayer>("layer"),
                    ProvidesSurface = n.Bool("providesSurface"),
                    Price = n.Int("price"),
                };
                if (def.Layer == ItemLayer.Surface)
                    def.SurfaceFootprint = n.Size("surfaceFootprint");
                else
                    def.FloorFootprint = n.Size("floorFootprint");
                if (def.ProvidesSurface && def.Layer != ItemLayer.Floor)
                    throw n.Error("providesSurface", "only Floor items can provide a surface");
                if (result.ContainsKey(def.Id)) throw n.Error("id", "duplicate id '" + def.Id + "'");
                result[def.Id] = def;
            }
            return result;
        }

        private static Dictionary<string, ContainerDef> LoadContainers(ContentNode root, Dictionary<string, ItemDef> items)
        {
            var result = new Dictionary<string, ContainerDef>();
            foreach (var n in root.ObjArray("containers"))
            {
                var def = new ContainerDef
                {
                    Id = n.Str("id"),
                    ItemId = n.Str("itemId"),
                    CellCount = n.Int("cellCount", 1),
                    MaxStage = n.Enum<PlantStage>("maxStage"),
                    AcceptsFromStage = n.Enum<PlantStage>("acceptsFromStage"),
                    DryRatePerHour = n.Float("dryRatePerHour"),
                };
                if (!items.ContainsKey(def.ItemId))
                    throw n.Error("itemId", "unknown item id '" + def.ItemId + "'");
                if (def.AcceptsFromStage > def.MaxStage)
                    throw n.Error("acceptsFromStage", "must not be above maxStage");
                if (result.ContainsKey(def.Id)) throw n.Error("id", "duplicate id '" + def.Id + "'");
                result[def.Id] = def;
            }
            return result;
        }

        private static Dictionary<string, GeneDef> LoadGenes(ContentNode root)
        {
            var result = new Dictionary<string, GeneDef>();
            foreach (var n in root.ObjArray("genes"))
            {
                var def = new GeneDef { Id = n.Str("id"), Kind = n.Enum<GeneKind>("kind") };
                if (def.Kind == GeneKind.Mendelian)
                {
                    var alleles = new List<string>();
                    var arr = n.Array("alleles");
                    for (int i = 0; i < arr.Count; i++)
                    {
                        if (arr[i].Type != JTokenType.String || ((string)arr[i]).Length == 0)
                            throw n.Error("alleles[" + i + "]", "must be a non-empty string");
                        var a = (string)arr[i];
                        if (alleles.Contains(a)) throw n.Error("alleles[" + i + "]", "duplicate allele '" + a + "'");
                        alleles.Add(a);
                    }
                    if (alleles.Count < 2) throw n.Error("alleles", "needs at least 2 alleles");
                    def.Alleles = alleles;
                }
                else
                {
                    def.Alleles = new List<string>();
                    def.Min = n.Float("min", float.MinValue);
                    def.Max = n.Float("max", float.MinValue);
                    if (def.Max < def.Min) throw n.Error("max", "must not be below min");
                }
                if (result.ContainsKey(def.Id)) throw n.Error("id", "duplicate id '" + def.Id + "'");
                result[def.Id] = def;
            }
            return result;
        }

        private static Dictionary<string, VarietyDef> LoadVarieties(ContentNode root, Dictionary<string, GeneDef> genes)
        {
            var result = new Dictionary<string, VarietyDef>();
            foreach (var n in root.ObjArray("varieties"))
            {
                var def = new VarietyDef
                {
                    Id = n.Str("id"),
                    Name = n.Str("name"),
                    GrowthPerHour = n.Float("growthPerHour"),
                };

                var th = n.Obj("stageThresholds");
                foreach (var k in th.Keys())
                    if (!System.Enum.IsDefined(typeof(PlantStage), k))
                        throw th.Error(k, "unknown stage");
                var thresholds = new float[6];
                float prev = 0f;
                for (int i = 0; i < thresholds.Length; i++)
                {
                    var name = ((PlantStage)i).ToString();
                    thresholds[i] = th.Float(name);
                    if (thresholds[i] < prev) throw th.Error(name, "must not be below the previous stage's threshold");
                    prev = thresholds[i];
                }
                def.StageThresholds = thresholds;

                var sg = n.Obj("seasonGrowth");
                foreach (var k in sg.Keys())
                    if (!System.Enum.IsDefined(typeof(Season), k))
                        throw sg.Error(k, "unknown season");
                var seasons = new float[3];
                for (int i = 0; i < seasons.Length; i++)
                    seasons[i] = sg.Float(((Season)i).ToString());
                def.SeasonGrowth = seasons;

                var gn = n.Obj("genes");
                var pairs = new Dictionary<string, AllelePair>();
                foreach (var gid in gn.Keys())
                {
                    if (!genes.ContainsKey(gid)) throw gn.Error(gid, "unknown gene id '" + gid + "'");
                    pairs[gid] = ReadPair(gn, gid, genes[gid]);
                }
                foreach (var gid in genes.Keys)
                    if (!pairs.ContainsKey(gid)) throw gn.Error(gid, "missing required field");
                def.Genes = pairs;

                if (result.ContainsKey(def.Id)) throw n.Error("id", "duplicate id '" + def.Id + "'");
                result[def.Id] = def;
            }
            return result;
        }

        private static AllelePair ReadPair(ContentNode genesNode, string gid, GeneDef gene)
        {
            var arr = genesNode.Array(gid);
            if (arr.Count != 2) throw genesNode.Error(gid, "must have exactly 2 alleles");
            var pair = new AllelePair();
            if (gene.Kind == GeneKind.Mendelian)
            {
                var vals = new string[2];
                for (int i = 0; i < 2; i++)
                {
                    if (arr[i].Type != JTokenType.String) throw genesNode.Error(gid + "[" + i + "]", "must be a string");
                    vals[i] = (string)arr[i];
                    bool known = false;
                    foreach (var a in gene.Alleles) if (a == vals[i]) known = true;
                    if (!known) throw genesNode.Error(gid + "[" + i + "]", "unknown allele '" + vals[i] + "' for gene '" + gid + "'");
                }
                pair.A = vals[0];
                pair.B = vals[1];
            }
            else
            {
                var vals = new float[2];
                for (int i = 0; i < 2; i++)
                {
                    vals[i] = genesNode.FloatAt(arr[i], gid + "[" + i + "]", gene.Min);
                    if (vals[i] > gene.Max) throw genesNode.Error(gid + "[" + i + "]", "must be at most " + gene.Max);
                }
                pair.ValueA = vals[0];
                pair.ValueB = vals[1];
            }
            return pair;
        }

        private static EconomyConfig LoadEconomy(ContentNode root, Dictionary<string, ItemDef> items,
            Dictionary<string, VarietyDef> varieties)
        {
            var cfg = new EconomyConfig
            {
                StartingMoney = root.Int("startingMoney"),
                SellBackFraction = root.Float("sellBackFraction"),
            };
            if (cfg.SellBackFraction > 1f) throw root.Error("sellBackFraction", "must be at most 1");

            var g = root.Obj("softLockGuard");
            var guard = new SoftLockGuardConfig { Enabled = g.Bool("enabled"), GiftVarietyId = g.OptStr("giftVarietyId") };
            if (guard.GiftVarietyId != null && !varieties.ContainsKey(guard.GiftVarietyId))
                throw g.Error("giftVarietyId", "unknown variety id '" + guard.GiftVarietyId + "'");
            cfg.SoftLockGuard = guard;

            var inv = root.Obj("starterInventory");
            var counts = new Dictionary<string, int>();
            foreach (var id in inv.Keys())
            {
                if (!items.ContainsKey(id)) throw inv.Error(id, "unknown item id '" + id + "'");
                counts[id] = inv.Int(id);
            }
            cfg.StarterInventory = counts;
            return cfg;
        }

        private static BuildingConfig LoadBuilding(ContentNode root)
        {
            var plot = root.Obj("plot");
            return new BuildingConfig
            {
                PlotWidth = plot.Int("w", 1),
                PlotDepth = plot.Int("d", 1),
                NudgeRadius = root.Int("nudgeRadius"),
            };
        }
    }
}
