using System.Collections.Generic;
using PlantNursery.Sim.Core;
using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// Draws a capsule for every player in the session. Only the local player exists today, and its capsule
    /// follows the local body (the CharacterController object). Reads session state; holds no rules.
    /// </summary>
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField] private SimHost host;
        [SerializeField] private Transform localBody;
        [SerializeField] private TopDownPlayerController controller;

        private readonly Dictionary<PlayerId, Transform> _views = new Dictionary<PlayerId, Transform>();

        /// <summary>Players currently drawn.</summary>
        public int ViewCount => _views.Count;

        /// <summary>Wire references (used by the scene generator).</summary>
        public void Configure(SimHost simHost, Transform body, TopDownPlayerController walker)
        {
            host = simHost; localBody = body; controller = walker;
        }

        private void LateUpdate()
        {
            if (host == null || !host.IsReady) return;
            foreach (var id in host.Session.Players)
            {
                if (!_views.TryGetValue(id, out var view))
                {
                    view = Build(id);
                    _views.Add(id, view);
                }
                if (id.Equals(host.Session.LocalPlayer) && localBody != null)
                {
                    view.position = localBody.position;
                    if (controller != null && controller.Facing.sqrMagnitude > 0f)
                        view.rotation = Quaternion.LookRotation(controller.Facing);
                }
            }
        }

        private Transform Build(PlayerId id)
        {
            var holder = new GameObject("PlayerView_" + id).transform;
            holder.SetParent(transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(holder, false);
            body.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f); // lift so the feet sit on the position
            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(nose.GetComponent<Collider>());
            nose.transform.SetParent(holder, false);
            nose.transform.localScale = new Vector3(0.15f, 0.15f, 0.3f);
            nose.transform.localPosition = new Vector3(0f, 1.3f, 0.35f);
            return holder;
        }
    }
}
