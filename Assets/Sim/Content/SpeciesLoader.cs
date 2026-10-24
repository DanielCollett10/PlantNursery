using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PlantNursery.Sim.Content
{
    /// <summary>Parses and validates species.json (decision 0011: runs beside the old varieties.json).</summary>
    internal static class SpeciesLoader
    {
        public static Dictionary<string, SpeciesDef> Load(ContentNode root, IReadOnlyDictionary<string, GeneDef> allGenes,
            IReadOnlyDictionary<string, ContainerDef> containers, IReadOnlyDictionary<string, VarietyDef> legacyVarieties)
        {
            var result = new Dictionary<string, SpeciesDef>();
            var varietyIds = new HashSet<string>(legacyVarieties.Keys);
            foreach (var n in root.ObjArray("species"))
            {
                var def = new SpeciesDef { Id = n.Str("id"), Name = n.Str("name"), RegrowDays = n.Int("regrowDays") };
                if (result.ContainsKey(def.Id)) throw n.Error("id", "duplicate id '" + def.Id + "'");
                def.Stages = LoadStages(n, containers);
                def.SeasonMultipliers = LoadSeasons(n.Obj("seasonMultipliers"));
                def.GeneIds = LoadGeneIds(n, def.Id, allGenes);
                def.Modules = StringList(n, "modules", allowEmpty: true);
                def.Sowing = LoadSowing(n.Obj("sowing"), containers, def.Stages[0]);
                def.Varieties = LoadVarieties(n, def, allGenes, varietyIds);
                result[def.Id] = def;
            }
            foreach (var g in allGenes.Values)
                if (g.SpeciesId != null && !result.ContainsKey(g.SpeciesId))
                    throw new ContentException(ContentLoader.GenesFile, "genes." + g.Id + ".speciesId",
                        "unknown species id '" + g.SpeciesId + "'");
            return result;
        }

        private static List<string> StringList(ContentNode n, string name, bool allowEmpty)
        {
            var arr = n.Array(name);
            if (arr.Count == 0 && !allowEmpty) throw n.Error(name, "must not be empty");
            var list = new List<string>();
            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i].Type != JTokenType.String || ((string)arr[i]).Length == 0)
                    throw n.Error(name + "[" + i + "]", "must be a non-empty string");
                var s = (string)arr[i];
                if (list.Contains(s)) throw n.Error(name + "[" + i + "]", "duplicate '" + s + "'");
                list.Add(s);
            }
            return list;
        }

        private static void CheckContainers(ContentNode n, string field, List<string> ids,
            IReadOnlyDictionary<string, ContainerDef> containers)
        {
            for (int i = 0; i < ids.Count; i++)
                if (!containers.ContainsKey(ids[i]))
                    throw n.Error(field + "[" + i + "]", "unknown container id '" + ids[i] + "'");
        }

        private static List<StageDef> LoadStages(ContentNode n, IReadOnlyDictionary<string, ContainerDef> containers)
        {
            var stages = new List<StageDef>();
            var names = new HashSet<string>();
            foreach (var s in n.ObjArray("stages"))
            {
                var st = new StageDef { Name = s.Str("name"), Days = s.Int("days") };
                var allowed = StringList(s, "allowedContainers", allowEmpty: false);
                CheckContainers(s, "allowedContainers", allowed, containers);
                st.AllowedContainers = allowed;
                if (!names.Add(st.Name)) throw s.Error("name", "duplicate stage '" + st.Name + "'");
                stages.Add(st);
            }
            if (stages.Count == 0) throw n.Error("stages", "needs at least one stage");
            for (int i = 0; i < stages.Count - 1; i++)
                if (stages[i].Name == "Specimen")
                    throw n.Error("stages[" + i + "].name", "Specimen must be the last stage");
            return stages;
        }

        private static float[] LoadSeasons(ContentNode sm)
        {
            foreach (var k in sm.Keys())
                if (!System.Enum.IsDefined(typeof(Season), k)) throw sm.Error(k, "unknown season");
            var seasons = new float[3];
            for (int i = 0; i < seasons.Length; i++) seasons[i] = sm.Float(((Season)i).ToString());
            return seasons;
        }

        private static List<string> LoadGeneIds(ContentNode n, string speciesId, IReadOnlyDictionary<string, GeneDef> allGenes)
        {
            var ids = StringList(n, "geneIds", allowEmpty: false);
            for (int i = 0; i < ids.Count; i++)
            {
                if (!allGenes.TryGetValue(ids[i], out var g))
                    throw n.Error("geneIds[" + i + "]", "unknown gene id '" + ids[i] + "'");
                if (g.SpeciesId != speciesId)
                    throw n.Error("geneIds[" + i + "]", "gene '" + ids[i] + "' does not belong to species '" + speciesId + "'");
            }
            return ids;
        }

        private static SowingDef LoadSowing(ContentNode s, IReadOnlyDictionary<string, ContainerDef> containers, StageDef first)
        {
            var into = StringList(s, "sowInto", allowEmpty: false);
            CheckContainers(s, "sowInto", into, containers);
            for (int i = 0; i < into.Count; i++)
                if (!first.AllowedContainers.Contains(into[i]))
                    throw s.Error("sowInto[" + i + "]",
                        "first stage '" + first.Name + "' does not allow container '" + into[i] + "'");
            var mode = s.Str("trayMode");
            if (mode != "wholeTray" && mode != "perCell")
                throw s.Error("trayMode", "unknown value '" + mode + "' (expected one of wholeTray, perCell)");
            return new SowingDef { SowInto = into, TrayMode = mode == "wholeTray" ? TrayMode.WholeTray : TrayMode.PerCell };
        }

        private static Dictionary<string, SpeciesVarietyDef> LoadVarieties(ContentNode n, SpeciesDef species,
            IReadOnlyDictionary<string, GeneDef> allGenes, HashSet<string> varietyIds)
        {
            var result = new Dictionary<string, SpeciesVarietyDef>();
            foreach (var v in n.ObjArray("varieties"))
            {
                var def = new SpeciesVarietyDef { Id = v.Str("id"), Name = v.Str("name"), SpeciesId = species.Id };
                if (!varietyIds.Add(def.Id)) throw v.Error("id", "duplicate variety id '" + def.Id + "'");
                var gn = v.Obj("genes");
                var pairs = new Dictionary<string, AllelePair>();
                foreach (var gid in gn.Keys())
                {
                    if (!species.GeneIds.Contains(gid))
                        throw gn.Error(gid, "unknown gene id '" + gid + "' for species '" + species.Id + "'");
                    pairs[gid] = ContentLoader.ReadPair(gn, gid, allGenes[gid]);
                }
                foreach (var gid in species.GeneIds)
                    if (!pairs.ContainsKey(gid)) throw gn.Error(gid, "missing required field");
                def.Genes = pairs;
                result[def.Id] = def;
            }
            if (result.Count == 0) throw n.Error("varieties", "needs at least one variety");
            return result;
        }
    }
}
