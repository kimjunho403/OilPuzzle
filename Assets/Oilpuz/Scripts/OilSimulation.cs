using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oilpuz
{
    // Material particles keep their identity through merging and tearing.
    // No transform/centroid is ever assigned the pointer position.
    public sealed class OilSimulation
    {
        public const float ParticleRadius = .028f;
        public const float Spacing = .051f;
        public const float BowlRadius = .96f;
        public const float FixedDelta = 1f / 120f;
        public sealed class Particle
        {
            public Vector2 position, velocity, previous;
            public float cooldown;
            public int group;
            public Particle(Vector2 p) { position = previous = p; }
        }
        sealed class Bond
        {
            public int a, b;
            public float rest;
            public Bond(int a, int b, float rest) { this.a = a; this.b = b; this.rest = rest; }
        }
        public readonly List<Particle> Particles = new List<Particle>();
        readonly List<Bond> bonds = new List<Bond>();
        public int LinkCount => bonds.Count;
        public void GetLink(int index, out Vector2 a, out Vector2 b)
        {
            var link = bonds[index];
            a = Particles[link.a].position;
            b = Particles[link.b].position;
        }
        readonly List<int> anchors = new List<int>();
        readonly List<Vector2> anchorOffsets = new List<Vector2>();
        readonly Dictionary<int, Vector2> centers = new Dictionary<int, Vector2>();
        readonly Dictionary<int, int> sizes = new Dictionary<int, int>();
        int[] parents;
        Vector2 pointer, lastPointer;
        float tearCharge, tearCooldown;
        int stepIndex;
        public int RelaxedLinks { get; private set; }
        public StageDefinition Stage { get; private set; }
        public int GroupCount { get; private set; }
        public int SplitCount { get; private set; }
        public int MergeCount { get; private set; }
        public int GrabbedParticle => anchors.Count > 0 ? anchors[0] : -1;
        public bool Dragging => anchors.Count > 0;
        public bool Won { get; private set; }
        public bool Failed { get; private set; }
        public bool BlockedByGate { get; private set; }
        public bool InGoal { get; private set; }
        public float Tension { get; private set; }
        public float PointerSpeed { get; private set; }
        public float SettleTime { get; private set; }
        public float Time { get; private set; }
        public Vector2 Pointer => pointer;
        public float TotalArea => Particles.Count * Mathf.PI * ParticleRadius * ParticleRadius;

        public void Reset(StageDefinition stage)
        {
            Stage = stage; Particles.Clear(); bonds.Clear(); Release();
            GroupCount = SplitCount = MergeCount = 0; Won = Failed = InGoal = false;
            Tension = tearCharge = tearCooldown = SettleTime = PointerSpeed = Time = 0;
            stepIndex = RelaxedLinks = 0;
            foreach (var seed in stage.seeds)
            {
                int start = Particles.Count;
                int rings = Mathf.Clamp(seed.rings, 1, 3);
                for (int q = -rings; q <= rings; q++) for (int r = -rings; r <= rings; r++)
                {
                    if (Mathf.Abs(q + r) > rings) continue;
                    var p = seed.position + new Vector2((q + r * .5f) * Spacing, r * Spacing * .8660254f);
                    Particles.Add(new Particle(p));
                }
                for (int a = start; a < Particles.Count; a++) for (int b = a + 1; b < Particles.Count; b++)
                {
                    float distance = Vector2.Distance(Particles[a].position, Particles[b].position);
                    if (distance < Spacing * 1.15f) bonds.Add(new Bond(a, b, distance));
                }
            }
            parents = new int[Particles.Count]; UpdateGroups();
        }
        public Vector2 GroupCenter(int group) => centers[group];
        public int GroupSize(int group) => sizes[group];
        public bool Grab(Vector2 point)
        {
            if (Won || Failed || Dragging) return false;
            int nearest = -1; float distance = .064f;
            for (int i = 0; i < Particles.Count; i++)
            {
                float d = Vector2.Distance(point, Particles[i].position);
                if (d < distance) { distance = d; nearest = i; }
            }
            if (nearest < 0) return false;
            pointer = lastPointer = point; PointerSpeed = tearCharge = 0;
            anchors.Add(nearest); anchorOffsets.Add(Particles[nearest].position - point);
            // Local attachment only; retaining offsets prevents a click-induced jump.
            for (int i = 0; i < Particles.Count && anchors.Count < 3; i++)
                if (i != nearest && Particles[i].group == Particles[nearest].group && Vector2.Distance(Particles[i].position, point) < .069f)
                { anchors.Add(i); anchorOffsets.Add(Particles[i].position - point); }
            return true;
        }
        public void Move(Vector2 point) { if (Dragging) pointer = point; }
        public void Release() { anchors.Clear(); anchorOffsets.Clear(); tearCharge = 0; }
        int Root(int i) { while (parents[i] != i) { parents[i] = parents[parents[i]]; i = parents[i]; } return i; }
        // A liquid has no permanent shear lattice. Nearby material exchanges
        // neighbours; only a spanning set of old links retains a stretched neck
        // until the explicit tearing rule severs it.
        void RelaxNeighborhood(float dt)
        {
            foreach (var b in bonds)
            {
                float d = Vector2.Distance(Particles[b.a].position, Particles[b.b].position);
                float relaxed = Mathf.Clamp(d, Spacing * .94f, Spacing * 1.65f);
                b.rest = Mathf.Lerp(b.rest, relaxed, 1 - Mathf.Exp(-11 * dt));
            }
            if (++stepIndex % 6 != 0) return;
            var oldLinks = new Dictionary<int, Bond>();
            foreach (var b in bonds) oldLinks[b.a * Particles.Count + b.b] = b;
            var next = new List<Bond>();
            for (int i = 0; i < parents.Length; i++) parents[i] = i;
            for (int a = 0; a < Particles.Count; a++) for (int b = a + 1; b < Particles.Count; b++)
            {
                if (Particles[a].group != Particles[b].group) continue;
                float distance = Vector2.Distance(Particles[a].position, Particles[b].position);
                if (distance > Spacing * 1.38f || !ClearSegment(Particles[a].position, Particles[b].position)) continue;
                int key = a * Particles.Count + b;
                next.Add(oldLinks.TryGetValue(key, out var old) ? old : new Bond(a, b, Mathf.Max(Spacing, distance)));
                parents[Root(b)] = Root(a);
            }
            foreach (var b in bonds)
            {
                int a = Root(b.a), c = Root(b.b);
                if (a != c) { next.Add(b); parents[c] = a; }
                else if (!next.Contains(b)) RelaxedLinks++;
            }
            bonds.Clear(); bonds.AddRange(next);
        }
        void UpdateGroups()
        {
            for (int i = 0; i < parents.Length; i++) parents[i] = i;
            foreach (var b in bonds) { int a = Root(b.a), c = Root(b.b); if (a != c) parents[c] = a; }
            centers.Clear(); sizes.Clear();
            for (int i = 0; i < Particles.Count; i++)
            {
                int group = Root(i); Particles[i].group = group;
                if (!centers.ContainsKey(group)) { centers[group] = Vector2.zero; sizes[group] = 0; }
                centers[group] += Particles[i].position; sizes[group]++;
            }
            var keys = new List<int>(centers.Keys);
            foreach (int key in keys) centers[key] /= sizes[key];
            GroupCount = keys.Count;
        }
        bool ClearSegment(Vector2 a, Vector2 b)
        {
            Vector2 delta = b - a;
            foreach (var o in Stage.obstacles)
            {
                float t = Mathf.Clamp01(Vector2.Dot(o.position - a, delta) / Mathf.Max(.000001f, delta.sqrMagnitude));
                if (Vector2.Distance(a + delta * t, o.position) < o.radius + .007f) return false;
            }
            return true;
        }
        void Constrain(Particle p)
        {
            // Bound a material step before collision projection, including the
            // local pointer constraint, so a flick cannot teleport through food.
            p.position=p.previous+Vector2.ClampMagnitude(p.position-p.previous,.032f);
            for (int pass = 0; pass < 3; pass++)
            {
                foreach (var o in Stage.obstacles)
                {
                    Vector2 delta = p.position - o.position; float d = delta.magnitude, min = o.radius + ParticleRadius;
                    if (d < min) p.position = o.position + (d > .00001f ? delta / d : Vector2.up) * min;
                }
                float length = p.position.magnitude;
                if (length > BowlRadius - ParticleRadius) p.position *= (BowlRadius - ParticleRadius) / length;
            }
            // Nominal area controls gate eligibility; a large connected volume cannot be threaded into an arbitrarily thin string.
            if (Stage.hasGate && sizes.TryGetValue(p.group, out int count) && 2f * ParticleRadius * Mathf.Sqrt(count) > Stage.gateWidth)
            {
                float oldSide = p.previous.y - Stage.gateY;
                if (Mathf.Abs(p.position.x - Stage.gateX) < Stage.gateWidth * .5f + ParticleRadius && oldSide * (p.position.y - Stage.gateY) <= 0)
                { p.position.y = Stage.gateY + Mathf.Sign(oldSide) * .002f; BlockedByGate = true; }
            }
        }
        void TearLocalNeck()
        {
            if (!Dragging) return;
            int grabbed = GrabbedParticle, group = Particles[grabbed].group;
            Vector2 direction = pointer - Particles[grabbed].position;
            if (direction.sqrMagnitude < .00001f) direction = pointer - lastPointer;
            direction.Normalize();
            var members = new List<int>();
            for (int i = 0; i < Particles.Count; i++) if (Particles[i].group == group) members.Add(i);
            if (members.Count < 4) return;
            // Separate the stretched leading material along a cross-section through its neck.
            members.Sort((a,b) => Vector2.Dot(Particles[b].position, direction).CompareTo(Vector2.Dot(Particles[a].position, direction)));
            var head = new HashSet<int>(); int headCount = Mathf.Clamp(members.Count / 3, 2, 7);
            for (int i = 0; i < headCount; i++) head.Add(members[i]);
            head.Add(grabbed);
            bonds.RemoveAll(b => Particles[b.a].group == group && head.Contains(b.a) != head.Contains(b.b));
            foreach (int i in members) Particles[i].cooldown = .7f;
            // Keep the same primary material handle after a split.
            for (int i = anchors.Count - 1; i >= 1; i--) if (!head.Contains(anchors[i])) { anchors.RemoveAt(i); anchorOffsets.RemoveAt(i); }
            int before = GroupCount; UpdateGroups();
            if (GroupCount > before) SplitCount++;
            tearCooldown = .65f; tearCharge = 0;
            if (SplitCount > Stage.splitLimit) { Failed = true; Release(); }
        }
        public void Step(float dt = FixedDelta)
        {
            if (Won || Failed || dt <= 0) return;
            dt = Mathf.Min(dt, 1f / 60f); Time += dt; BlockedByGate = false;
            tearCooldown = Mathf.Max(0, tearCooldown - dt);
            float speed = Dragging ? Vector2.Distance(pointer, lastPointer) / dt : 0;
            PointerSpeed = Mathf.Lerp(PointerSpeed, Mathf.Min(speed, 8), 1 - Mathf.Exp(-12 * dt));
            float lag = Dragging ? Vector2.Distance(pointer + anchorOffsets[0], Particles[GrabbedParticle].position) : 0;
            float neckStrain=0;
            if(Dragging) foreach(var bond in bonds)
                if(Particles[bond.a].group==Particles[GrabbedParticle].group)
                    neckStrain=Mathf.Max(neckStrain,Vector2.Distance(Particles[bond.a].position,Particles[bond.b].position)/Spacing-1);
            Tension = Mathf.Lerp(Tension, Mathf.Clamp01(Mathf.Max(lag / .29f,neckStrain*.72f)), 1 - Mathf.Exp(-14 * dt));
            if (Dragging && PointerSpeed > 1.05f && (lag > .12f || neckStrain > .55f)) tearCharge += dt;
            else tearCharge = Mathf.Max(0, tearCharge - dt * .7f);
            if (tearCharge > .075f && tearCooldown <= 0) TearLocalNeck();
            lastPointer = pointer;
            if (Failed) return;
            for (int i = 0; i < Particles.Count; i++)
            {
                var p = Particles[i]; p.previous = p.position; p.cooldown = Mathf.Max(0, p.cooldown - dt);
                // Weak capillary rounding, not a spring pulling every particle
                // back to a rigid blob centre. Shape relaxes by local flow below.
                Vector2 acceleration = (centers[p.group] - p.position) * 1.4f;
                if (Stage.current > 0)
                {
                    float shelter = Vector2.Distance(p.position, Stage.goal) < Stage.goalRadius ? .04f : .5f;
                    acceleration += new Vector2(Mathf.Sin(Time * .7f + p.position.y * 3), Mathf.Cos(Time * .4f + p.position.x * 3) * .3f) * Stage.current * shelter;
                }
                int anchor = anchors.IndexOf(i);
                if (anchor >= 0)
                {
                    float weight = anchor == 0 ? 1 : .24f;
                    acceleration += Vector2.ClampMagnitude(pointer + anchorOffsets[anchor] - p.position, .8f) * (400f * weight);
                }
                p.velocity = (p.velocity + acceleration * dt) * Mathf.Exp(-dt * 10f);
                p.velocity = Vector2.ClampMagnitude(p.velocity, 4);
                p.position += p.velocity * dt;
            }
            for (int iteration = 0; iteration < 4; iteration++)
            {
                foreach (var b in bonds)
                {
                    var a = Particles[b.a]; var c = Particles[b.b]; Vector2 delta = c.position - a.position; float distance = delta.magnitude;
                    if (distance < .000001f) continue;
                    float stiffness=distance>Spacing*1.85f?.085f:.014f;
                    Vector2 correction = delta / distance * ((distance - b.rest) * stiffness);
                    a.position += correction; c.position -= correction;
                }
                for (int a = 0; a < Particles.Count; a++) for (int b = a + 1; b < Particles.Count; b++)
                {
                    Vector2 delta = Particles[b].position - Particles[a].position; float distance = delta.magnitude;
                    float min = Spacing * .87f;
                    if (distance >= min) continue;
                    Vector2 direction = distance > .000001f ? delta / distance : Vector2.right;
                    Vector2 correction = direction * ((min - distance) * .45f);
                    Particles[a].position -= correction; Particles[b].position += correction;
                }
                // The grabbed material stays under the fingertip, while only the
                // neighbour constraints pull the trailing volume after it.
                for(int a=0;a<anchors.Count;a++)
                {
                    var p=Particles[anchors[a]];
                    p.position=Vector2.Lerp(p.position,pointer+anchorOffsets[a],a==0?.4f:.065f);
                }
                foreach (var p in Particles) Constrain(p);
            }
            // Constraint relaxation must not turn into a rubber-band rebound.
            foreach (var p in Particles) p.velocity = Vector2.Lerp(p.velocity, (p.position - p.previous) / dt, .30f);
            // Pairwise viscosity dissipates relative motion while conserving the
            // pair's momentum. The grabbed material continues to lead the tail.
            foreach (var b in bonds)
            {
                var a=Particles[b.a];var c=Particles[b.b];
                float weight=Mathf.Clamp01(1-Vector2.Distance(a.position,c.position)/(Spacing*2));
                Vector2 viscous=(c.velocity-a.velocity)*(weight*.075f);
                a.velocity+=viscous;c.velocity-=viscous;
            }
            RelaxNeighborhood(dt);
            // Neighbour attraction is local and obstructed by ingredients.
            int previousCount = GroupCount;
            for (int a = 0; a < Particles.Count; a++) for (int b = a + 1; b < Particles.Count; b++)
            {
                var p = Particles[a]; var q = Particles[b];
                if (p.group == q.group || p.cooldown > 0 || q.cooldown > 0) continue;
                float distance = Vector2.Distance(p.position, q.position);
                if (distance > .11f || !ClearSegment(p.position, q.position)) continue;
                if (Stage.hasGate && (p.position.y - Stage.gateY) * (q.position.y - Stage.gateY) < 0) continue;
                Vector2 attraction = (q.position - p.position).normalized * (.11f - distance) * 12f * dt;
                p.velocity += attraction; q.velocity -= attraction;
                if (distance < Spacing * 1.22f) bonds.Add(new Bond(a, b, Spacing));
            }
            UpdateGroups();
            if (GroupCount < previousCount) MergeCount += previousCount - GroupCount;
            InGoal = GroupCount == 1;
            if (InGoal) foreach (var p in Particles) if (Vector2.Distance(p.position, Stage.goal) + ParticleRadius * 1.2f > Stage.goalRadius) { InGoal = false; break; }
            SettleTime = InGoal && !Dragging ? SettleTime + dt : 0;
            if (SettleTime > 1.1f) Won = true;
        }
    }
}
