using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// Small water gauge above a container: a bar that shrinks with the meter, plus a flat red disc when dry.
    /// Hidden at 30 and above. Applies a <see cref="ContainerLook"/>; holds no rules.
    /// </summary>
    public sealed class ContainerGaugeView : MonoBehaviour
    {
        private const float BarLength = 0.18f;
        private Transform _root;
        private Transform _bar;
        private Transform _dryDisc;
        private Material _barMat;
        private Material _discMat;
        private Material _backMat;

        /// <summary>True while the gauge is shown.</summary>
        public bool Visible => _root != null && _root.gameObject.activeSelf;
        /// <summary>True while the dry cue is shown.</summary>
        public bool DryCueVisible => _dryDisc != null && _dryDisc.gameObject.activeSelf;

        /// <summary>Apply a look; <paramref name="height"/> is the local height of the gauge.</summary>
        public void Apply(ContainerLook look, float height)
        {
            if (_root == null) Build();
            _root.localPosition = new Vector3(0f, height + 0.08f, 0f);
            _root.gameObject.SetActive(look.GaugeVisible);
            _dryDisc.gameObject.SetActive(look.Dry);
            var fill = Mathf.Max(look.GaugeFill, 0.04f);
            _bar.localScale = new Vector3(BarLength * fill, 0.02f, 0.03f);
            _bar.localPosition = new Vector3(-BarLength * (1f - fill) / 2f, 0f, 0f);
            Colour(_barMat, Color.Lerp(new Color(0.9f, 0.3f, 0.2f), new Color(0.2f, 0.5f, 0.9f), look.GaugeFill * 3f));
        }

        private void Build()
        {
            _root = new GameObject("WaterGauge").transform;
            _root.SetParent(transform, false);
            _barMat = NewMat();
            _discMat = NewMat();
            _backMat = NewMat();
            Colour(_discMat, new Color(0.85f, 0.1f, 0.1f));
            Colour(_backMat, new Color(0.1f, 0.1f, 0.1f));
            var back = Part(PrimitiveType.Cube, "Back", _backMat);
            back.localScale = new Vector3(BarLength + 0.02f, 0.015f, 0.05f);
            back.localPosition = new Vector3(0f, -0.012f, 0f);
            _bar = Part(PrimitiveType.Cube, "Bar", _barMat);
            _dryDisc = Part(PrimitiveType.Cylinder, "DryCue", _discMat);
            _dryDisc.localScale = new Vector3(0.12f, 0.004f, 0.12f);
            _dryDisc.localPosition = new Vector3(0f, -0.07f, 0f);
        }

        private Transform Part(PrimitiveType type, string name, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.transform.SetParent(_root, false);
            return go.transform;
        }

        private static Material NewMat()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            return new Material(shader);
        }

        private static void Colour(Material m, Color c)
        {
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        }

        private void OnDestroy()
        {
            Kill(_barMat); Kill(_discMat); Kill(_backMat);
        }

        private static void Kill(Material m)
        {
            if (m == null) return;
            if (Application.isPlaying) Destroy(m); else DestroyImmediate(m);
        }
    }
}
