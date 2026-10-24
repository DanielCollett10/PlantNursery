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
            EconomyFile = "economy.json", BuildingFile = "building.json", SpeciesFile = "species.json";

        /// <summary>Loads and validates all content. time.demo.json is optional and overrides time.json values.</summary>
        /// <exception cref="ContentException">On malformed JSON, missing files/fields, unknown ids or negative values.</exception>
        public static ContentDb Load(IDictionary<string, string> filesByName)
        {
            if (filesByName == null) throw new ArgumentNullException(nameof(filesByName));

            var time = LoadTime(filesByName);
            var items = LoadItems(Root(filesByName, ItemsFile));
            var allGenes = LoadGenes(Root(filesByName, GenesFile));
            // Old varieties only use the pre-pivot genes (no speciesId); species genes belong to species.json.
            var genes = new Dictionary<string, GeneDef>();
            foreach (var g in allGenes.Values) if (g.SpeciesId == null) genes[g.Id] = g;
            var containers = LoadContainers(Root(filesByName, ContainersFile), items);
            var varieties = LoadVarieties(Root(filesByName, VarietiesFile), genes);
            var economy = LoadEconomy(Root(filesByName, EconomyFile), items, varieties);
            var building = LoadBuilding(Root(filesByName, BuildingFile));
            var species = SpeciesLoader.Load(Root(filesByName, SpeciesFile), allGenes, containers, varieties);
            ValidateSeedItems(Root(filesByName, ItemsFile), items, species);
            BenchLoader.ValidateAcceptedIds(Root(filesByName, ItemsFile).ObjArray("items"), items);
            return new ContentDb(time, items, containers, varieties, genes, economy, building, species, allGenes);
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
            if (Check("morningEndMinute")) t.MorningEndMinute = n.Int("morningEndMinute");
            if (Check("eveningStartMinute")) t.EveningStartMinute = n.Int("eveningStartMinute");
            if (Check("realSecondsDay")) t.RealSecondsDay = n.Float("realSecondsDay");
            if (Check("nightRateMultiplier")) t.NightRateMultiplier = n.Float("nightRateMultiplier");
            if (Check("seasonLengthDays")) t.SeasonLengthDays = n.Int("seasonLengthDays");
            if (Check("shopOpenMinute")) t.ShopOpenMinute = n.Int("shopOpenMinute");
            if (Check("shopCloseMinute")) t.ShopCloseMinute = n.Int("shopCloseMinute");
            if (onlyKeys != null) return t;

            if (t.DayStartMinute > 1439) throw n.Error("dayStartMinute", "must be below 1440");
            if (t.NightStartMinute > 1439) throw n.Error("nightStartMinute", "must be below 1440");
            if (t.NightStartMinute <= t.DayStartMinute + 300)
                throw n.Error("nightStartMinute", "must be more than 300 minutes after dayStartMinute");
            if (t.MorningEndMinute <= t.DayStartMinute)
                throw n.Error("morningEndMinute", "must be after dayStartMinute");
            if (t.EveningStartMinute <= t.MorningEndMinute)
                throw n.Error("eveningStartMinute", "must be after morningEndMinute");
            if (t.NightStartMinute <= t.EveningStartMinute)
                throw n.Error("nightStartMinute", "must be after eveningStartMinute");
            if (t.RealSecondsDay <= 0f) throw n.Error("realSecondsDay", "must be greater than 0");
            if (t.NightRateMultiplier < 0f || t.NightRateMultiplier > 1f)
                throw n.Error("nightRateMultiplier", "must be between 0 and 1");
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
                    WaterCapacity = n.Has("waterCapacity") ? n.Int("waterCapacity") : 0,
                    SeedCount = n.Has("seedCount") ? n.Int("seedCount") : 0,
                    SpeciesId = n.OptStr("speciesId"),
                    VarietyId = n.OptStr("varietyId"),
                    PacketSeeds = n.Has("packetSeeds") ? n.Int("packetSeeds", 1) : 0,
                    Capacity = n.Has("capacity") ? n.Int("capacity", 1) : 0,
                };
                if (def.SpeciesId != null || def.VarietyId != null || def.PacketSeeds > 0)
                {
                    if (def.SpeciesId == null) throw n.Error("speciesId", "missing required field for a seed item");
                    if (def.VarietyId == null) throw n.Error("varietyId", "missing required field for a seed item");
                    if (def.PacketSeeds < 1) throw n.Error("packetSeeds", "missing required field for a seed item");
                }
                if (def.Layer == ItemLayer.Surface)
                    def.SurfaceFootprint = n.Size("surfaceFootprint");
                else
                    def.FloorFootprint = n.Size("floorFootprint");
                if (def.ProvidesSurface && def.Layer != ItemLayer.Floor)
                    throw n.Error("providesSurface", "only Floor items can provide a surface");
                if (n.Has("planting"))
                {
                    if (def.Layer != ItemLayer.Floor) throw n.Error("planting", "only Floor items can be benches");
                    def.Bench = BenchLoader.Load(n);
                }
                else if (n.Has("workStrip"))
                {
                    throw n.Error("workStrip", "only benches have a working strip (planting is missing)");
                }
                if (result.ContainsKey(def.Id)) throw n.Error("id", "duplicate id '" + def.Id + "'");
                result[def.Id] = def;
            }
            return result;
        }

        private static void ValidateSeedItems(ContentNode root, Dictionary<string, ItemDef> items, Dictionary<string, SpeciesDef> species)
        {
            var nodes = root.ObjArray("items");
            for (int i = 0; i < nodes.Count; i++)
            {
                var def = items[nodes[i].Str("id")];
                if (def.SpeciesId == null) continue;
                if (!species.TryGetValue(def.SpeciesId, out var sp))
                    throw nodes[i].Error("speciesId", "unknown species '" + def.SpeciesId + "'");
                if (!sp.Varieties.ContainsKey(def.VarietyId))
                    throw nodes[i].Error("varietyId", "unknown variety '" + def.VarietyId + "' for species '" + def.SpeciesId + "'");
            }
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
                    CellCount = n.Int("cells", 1),
                    AlongUnits = n.Int("alongUnits", 1),
                    AcrossUnits = n.Int("acrossUnits", 1),
                    DrainPerHour = n.Float("drainPerHour"),
                    WateringCost = n.Int("wateringCost", 1),
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
                var def = new GeneDef { Id = n.Str("id"), Kind = n.Enum<GeneKind>("kind"), SpeciesId = n.OptStr("speciesId") };
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

        internal static AllelePair ReadPair(ContentNode genesNode, string gid, GeneDef gene)
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
                InteractReach = root.Has("interactReach") ? root.Float("interactReach", 0.1f) : 3.0f,
            };
        }
    }
}
