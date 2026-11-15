using UnityEngine;

namespace DanceTap
{
    /// <summary>One pooled note. Pure view: NoteSpawner drives it, it never updates itself.</summary>
    public sealed class NoteRing : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sprite;

        public int NoteIndex { get; private set; } = -1;
        public bool Busy => NoteIndex >= 0;

        Transform tr;
        Color baseColor;

        void Awake()
        {
            tr = transform;
            if (sprite == null) sprite = GetComponent<SpriteRenderer>();
            baseColor = sprite.color;
            gameObject.SetActive(false);
        }

        public void Show(int noteIndex)
        {
            NoteIndex = noteIndex;
            gameObject.SetActive(true);
        }

        /// <summary>y = height above the target (lane local), scale, alpha 0..1.</summary>
        public void SetPose(float y, float scale, float alpha)
        {
            tr.localPosition = new Vector3(0f, y, 0f);
            tr.localScale = new Vector3(scale, scale, 1f);
            var c = baseColor; c.a *= alpha;
            sprite.color = c;
        }

        public void Hide()
        {
            NoteIndex = -1;
            gameObject.SetActive(false);
        }
    }
}
