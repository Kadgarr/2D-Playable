using System;
using UnityEngine;

namespace DanceTap
{
    /// <summary>
    /// Fits the orthographic camera to the screen aspect so the widest dance poses always stay on screen.
    /// 9:16 and wider: the art is shown 1:1 (baseOrthoSize). Taller phones (19.5:9, 20:9): the camera zooms out,
    /// the bottom edge (lanes, crowd) stays in place and the extra space goes to the top (HUD area, bg extension).
    /// Recalculates only when the screen size changes (two int compares per frame, no allocations).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFitter : MonoBehaviour
    {
        [Tooltip("Half height of the stage art in world units (shown 1:1 at 9:16)")]
        [SerializeField] float baseOrthoSize = 9.6f;
        [Tooltip("Half width that must always be visible: widest hand position + margin")]
        [SerializeField] float safeHalfWidth = 5.2f;
        [Tooltip("World Y of the bottom screen edge, kept fixed while zooming out")]
        [SerializeField] float bottomY = -9.6f;

        /// <summary>Raised after the camera was refitted (UI that follows world objects re-positions itself).</summary>
        public static event Action<Camera> ViewChanged;

        Camera cam;
        int lastW = -1, lastH = -1;

        public Camera Camera => cam;

        void OnEnable()
        {
            cam = GetComponent<Camera>();
            lastW = lastH = -1;
            Refit();
        }

        void LateUpdate()
        {
            if (cam.pixelWidth != lastW || cam.pixelHeight != lastH) Refit();
        }

        void OnValidate()
        {
            if (cam != null) { lastW = lastH = -1; Refit(); }
        }

        void Refit()
        {
            lastW = cam.pixelWidth;
            lastH = cam.pixelHeight;
            if (lastW <= 0 || lastH <= 0) return;
            float size = Mathf.Max(baseOrthoSize, safeHalfWidth / cam.aspect);
            cam.orthographicSize = size;
            var p = transform.position;
            p.y = bottomY + size;
            transform.position = p;
            ViewChanged?.Invoke(cam);
        }
    }
}
