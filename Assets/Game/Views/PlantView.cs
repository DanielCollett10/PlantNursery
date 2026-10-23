using PlantNursery.Sim.Plants;
using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// Placeholder plant: a stem and three leaves built from primitives. Scale follows the stage, colour the
    /// leafColour phenotype; wilting plants droop and desaturate, dead ones turn brown-grey.
    /// Reads a <see cref="Plant"/>; holds no rules.
    /// </summary>
    public sealed class PlantView : MonoBehaviour
    {
        private const float FullHeight = 0.3f;

        private Plant _plant;
        private Transform _pivot;
        private Material _stemMat;
        private Material _leafMat;
        private Color _leafBase;

        /// <summary>The plant shown.</summary>
        public Plant Plant => _plant;

        /// <summary>Build the visual for a plant and apply its current state.</summary>
        public void Bind(Plant plant)
        {
            _plant = plant;
            var allele = plant.Genome != null ? plant.Genome.MendelianPhenotype("leafColour") : null;
            _leafBase = WorldLayout.LeafColour(allele);
            Build();
            Refresh();
        }

        /// <summary>Re-apply scale, droop and tint from the plant's current state.</summary>
        public void Refresh()
        {
            if (_plant == null) return;
            var wilt = _plant.IsDead ? 1f : WorldLayout.WiltAmount(_plant.Moisture);
            _pivot.localScale = Vector3.one * WorldLayout.PlantScale(_plant.Stage);
            _pivot.localRotation = Quaternion.Euler(wilt * 55f, 0f, wilt * 20f);
            SetColour(_stemMat, WorldLayout.Tinted(WorldLayout.StemGreen, wilt, _plant.IsDead));
            SetColour(_leafMat, WorldLayout.Tinted(_leafBase, wilt, _plant.IsDead));
        }

        private void Build()
        {
            var pivotGo = new GameObject("Pivot");
            _pivot = pivotGo.transform;
            _pivot.SetParent(transform, false);

            _stemMat = NewMaterial();
            _leafMat = NewMaterial();

            var stem = Primitive(PrimitiveType.Cylinder, "Stem", _stemMat);
            stem.localScale = new Vector3(0.012f, FullHeight / 2f, 0.012f);
            stem.localPosition = new Vector3(0f, FullHeight / 2f, 0f);

            for (var i = 0; i < 3; i++)
            {
                var leaf = Primitive(PrimitiveType.Sphere, "Leaf" + i, _leafMat);
                leaf.localScale = new Vector3(0.07f, 0.012f, 0.04f);
                var yaw = i * 120f;
                leaf.localRotation = Quaternion.Euler(0f, yaw, 0f);
                leaf.localPosition = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.04f, FullHeight * (0.6f + 0.15f * i), 0f);
            }
        }

        private Transform Primitive(PrimitiveType type, string name, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.transform.SetParent(_pivot, false);
            return go.transform;
        }

        private static Material NewMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            return new Material(shader);
        }

        private static void SetColour(Material m, Color c)
        {
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        }

        private void OnDestroy()
        {
            if (_stemMat != null) Destroy(_stemMat);
            if (_leafMat != null) Destroy(_leafMat);
        }
    }
}
