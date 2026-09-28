using UnityEngine;

namespace Oilpuz
{
    [CreateAssetMenu(menuName = "Oilpuz/Soup Skin")]
    public sealed class SoupSkin : ScriptableObject
    {
        public string skinId = "mala";
        public string displayName = "마라탕";
        public Sprite table, bowlBroth, mushroom, fishBall, garnish;
        public Font font;
        public Color ink = new Color(.16f, .23f, .17f);
        public Color accent = new Color(.40f, .57f, .37f);
        public Color oil = new Color(1f, .48f, .045f);
        public Color oilEdge = new Color(.76f, .25f, .018f);
        public Color oilHighlight = new Color(1f, .94f, .61f);
    }
}
