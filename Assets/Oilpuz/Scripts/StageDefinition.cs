using System;
using UnityEngine;

namespace Oilpuz
{
    [Serializable] public struct OilSeed
    {
        public Vector2 position;
        [Range(1, 3)] public int rings;
        public OilSeed(float x, float y, int rings = 1) { position = new Vector2(x, y); this.rings = rings; }
    }
    [Serializable] public struct FoodObstacle
    {
        public Vector2 position;
        public float radius;
        public bool fishBall;
        public FoodObstacle(float x, float y, float radius, bool fish = false)
        { position = new Vector2(x, y); this.radius = radius; fishBall = fish; }
    }
    [CreateAssetMenu(menuName = "Oilpuz/Stage")]
    public sealed class StageDefinition : ScriptableObject
    {
        public string title;
        [TextArea] public string hint;
        public OilSeed[] seeds = Array.Empty<OilSeed>();
        public FoodObstacle[] obstacles = Array.Empty<FoodObstacle>();
        public Vector2 goal = new Vector2(0, -.52f);
        public float goalRadius = .34f;
        public int splitLimit = 2;
        public float current;
        public bool hasGate;
        public float gateY, gateX, gateWidth = .24f;
    }
}
