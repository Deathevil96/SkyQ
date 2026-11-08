using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>
    /// Authoritative passenger loop, matching the reference topology: two FEED lanes (left/right, running in from above
    /// the screen) merge into a closed RING that circulates around the small island.
    ///
    /// The unit of the loop is a ROW: <see cref="RowSize"/> passengers of one colour standing side by side. A row moves,
    /// merges into the ring and boards a plane as one piece, so the lanes of a row can never drift apart.
    ///
    /// The ring is a CONVEYOR of fixed slots that all move together. A feed row waits at a hold point a little upstream
    /// of the merge (so it never overlaps the circling rows), reserves the next empty slot that will reach the merge point
    /// exactly when it does, drives in and drops into that slot. Because rows sit in slots, any empty slot can be filled:
    /// the ring packs solid and never fragments into unusable gaps. A full ring stalls the feeds. A row boards when its
    /// slot passes the exit gap and a matching plane with room is waiting; otherwise it keeps circling.
    /// Positions are arc-length values along view-supplied paths; the model never reads visual transforms.
    /// </summary>
    public sealed class PassengerRing
    {
        /// <summary>One row of passengers. <see cref="FeedWait"/>: seconds spent waiting on-screen in a feed line (drives the
        /// view's emoji reactions; never affects rules).</summary>
        public sealed class Row { public int Color; public float S; public bool Entering; public int Slot = -1; public float FeedWait; }

        public readonly int RowSize;
        public readonly float Spacing, Length, BoardS, FeedLen;
        public readonly float SlotLength;                 // ring slot pitch (Length / slot count, so slots wrap exactly)
        public readonly float[] MergeS;                  // ring arc position where feed f joins
        public float Speed;
        public readonly List<Row> Ring = new List<Row>();
        public readonly List<Row>[] Feed = { new List<Row>(), new List<Row>() };   // index 0 = front (closest to the ring)

        public const float FeedSpeedFactor = 1.25f;      // feeds move faster than the ring so queues close up

        readonly int[] colorRows = new int[16];        // rows left per colour (ring + feeds)
        readonly int[] ringColorRows = new int[16];    // rows on the ring per colour
        readonly Row[] slots;                          // slot -> row (null = empty)
        readonly bool[] reserved;                      // slot promised to a feed row that is driving in
        readonly float holdGap;                        // how far upstream of the merge point a waiting feed row stands
        float phase;                                   // arc position of slot 0
        int freeSlots, remainingRows;

        /// <summary>Passengers still to board (ring + feeds).</summary>
        public int Remaining { get { return remainingRows * RowSize; } }
        public int RingRows { get { return Ring.Count; } }
        public int SlotCount { get { return slots.Length; } }

        /// <param name="bands">each band: [colour, size]; size must be a multiple of rowSize. Bands alternate between the two feeds.</param>
        /// <param name="startFull">level opens with the ring already packed and both feeds queued up to the hold line (reference look).</param>
        public PassengerRing(IList<int[]> bands, int rowSize, float spacing, float length, float boardS, float[] mergeS, float feedLen, float feedHeadStart, float speed, float holdGap = 0f, bool startFull = false)
        {
            RowSize = rowSize; Spacing = spacing; Length = length; BoardS = boardS; MergeS = mergeS; FeedLen = feedLen; Speed = speed;
            this.holdGap = Mathf.Clamp(holdGap, 0f, Mathf.Max(0f, feedLen - spacing));
            int n = Mathf.Max(1, Mathf.FloorToInt(length / spacing));
            SlotLength = length / n;
            slots = new Row[n];
            reserved = new bool[n];
            freeSlots = n;

            float hold = feedLen - this.holdGap;
            var rowsInFeed = new int[2];
            for (int b = 0; b < bands.Count; b++)
            {
                int f = b % 2, color = bands[b][0], rows = bands[b][1] / rowSize;
                for (int r = 0; r < rows; r++)
                {
                    Feed[f].Add(new Row { Color = color, S = Mathf.Min(feedHeadStart, hold) - rowsInFeed[f] * spacing });
                    rowsInFeed[f]++;
                    colorRows[color]++;
                    remainingRows++;
                }
            }
            if (startFull) Prefill(hold);
        }

        /// <summary>Moves the front rows of both feeds (alternating, in band order) into every ring slot, then packs each
        /// feed queue up against its hold line, so the level opens fully stocked instead of trickling in.</summary>
        void Prefill(float hold)
        {
            for (int k = 0; k < slots.Length; k++)
            {
                int f = Feed[k % 2].Count > 0 ? k % 2 : 1 - k % 2;
                if (Feed[f].Count == 0) break;
                var row = Feed[f][0];
                Feed[f].RemoveAt(0);
                row.Slot = k; row.S = SlotPos(k);
                slots[k] = row; freeSlots--;
                Ring.Add(row);
                ringColorRows[row.Color]++;
            }
            for (int f = 0; f < 2; f++)
                for (int i = 0; i < Feed[f].Count; i++) Feed[f][i].S = hold - i * Spacing;
        }

        /// <summary>Passengers of a colour still to board (ring + feeds).</summary>
        public int CountOfColor(int c) { return colorRows[c] * RowSize; }

        /// <summary>Rows of a colour currently circulating on the ring (the only ones that can board right now).</summary>
        public int RingRowsOfColor(int c) { return ringColorRows[c]; }

        /// <summary>True while the ring has an empty, unclaimed slot, so the feeds can still advance.</summary>
        public bool HasRoom { get { return freeSlots > 0; } }

        public static float Mod(float a, float m) { float r = a % m; return r < 0 ? r + m : r; }

        float SlotPos(int k) { return Mod(phase + k * SlotLength, Length); }

        /// <summary>True when arc position <paramref name="target"/> lies in the half-open stretch (from, from+step].</summary>
        bool Crossed(float from, float step, float target)
        {
            float d = Mod(target - from, Length);
            return d > 0f && d <= step;
        }

        /// <summary>Advance the loop. tryBoard(color, ringS) returns true when the whole row leaves the ring to board a plane.</summary>
        public void Tick(float dt, Func<int, float, bool> tryBoard)
        {
            float step = Speed * dt;
            float ringTravel = holdGap / FeedSpeedFactor;   // ring distance covered while a feed row drives from hold to merge
            float oldPhase = phase;
            phase = Mod(phase + step, Length);

            // 1. ring slots move together; full slots board at the exit gap, reserved slots collect their row at the merge
            for (int k = 0; k < slots.Length; k++)
            {
                float from = Mod(oldPhase + k * SlotLength, Length);
                var row = slots[k];
                if (row != null)
                {
                    row.S = SlotPos(k);
                    if (Crossed(from, step, BoardS) && tryBoard(row.Color, BoardS))
                    {
                        slots[k] = null; freeSlots++;
                        Ring.Remove(row);
                        ringColorRows[row.Color]--; colorRows[row.Color]--; remainingRows--;
                    }
                }
                else if (reserved[k])
                {
                    for (int f = 0; f < 2; f++)
                        if (Crossed(from, step, MergeS[f])) Land(f, k);
                }
            }

            // 2. feeds: compact queue behind the hold point; the head drives in once it has claimed a slot
            float hold = FeedLen - holdGap;
            for (int f = 0; f < 2; f++)
            {
                var feed = Feed[f];
                for (int i = 0; i < feed.Count; i++)
                {
                    var row = feed[i];
                    float limit = row.Entering ? FeedLen : hold;
                    if (i > 0) limit = Mathf.Min(limit, feed[i - 1].S - Spacing);
                    row.S = Mathf.Min(row.S + step * FeedSpeedFactor, limit);
                    if (row.S >= 0f && !row.Entering) row.FeedWait += dt;   // queued in view (S < 0 is above the screen)
                }
                // the row standing at the hold line may go even while rows ahead of it are still driving in
                int next = 0;
                while (next < feed.Count && feed[next].Entering) next++;
                if (next >= feed.Count || feed[next].S < hold - 0.001f || freeSlots == 0) continue;

                // claim the empty slot that reaches the merge point exactly when the head would (ringTravel from now)
                float launchPoint = Mod(MergeS[f] - ringTravel, Length);
                for (int k = 0; k < slots.Length; k++)
                {
                    if (slots[k] != null || reserved[k]) continue;
                    float from = Mod(oldPhase + k * SlotLength, Length);
                    if (!Crossed(from, step, launchPoint)) continue;
                    reserved[k] = true; freeSlots--;
                    feed[next].Entering = true; feed[next].Slot = k;
                    if (ringTravel <= 0f) Land(f, k);   // no hold gap: drop straight in
                    break;
                }
            }
        }

        /// <summary>
        /// Arrange booster: recolours the queue so the rows the parked planes need come first. Rows keep their positions;
        /// only colours move, so per-colour totals (and therefore plane/passenger consistency) never change. Order of
        /// "first": ring rows by distance to the exit gap, then the feed queues front to back, alternating feeds.
        /// </summary>
        /// <param name="demand">rows wanted per colour, in priority order as (colour, rows) pairs.</param>
        /// <returns>true when any row changed colour.</returns>
        public bool Arrange(IList<int[]> demand)
        {
            var order = new List<Row>(Ring.Count + Feed[0].Count + Feed[1].Count);
            var ring = new List<Row>(Ring);
            ring.Sort((a, b) => Mod(BoardS - a.S, Length).CompareTo(Mod(BoardS - b.S, Length)));
            order.AddRange(ring);
            for (int i = 0; i < Mathf.Max(Feed[0].Count, Feed[1].Count); i++)
                for (int f = 0; f < 2; f++) if (i < Feed[f].Count) order.Add(Feed[f][i]);

            var available = new int[colorRows.Length];
            foreach (var r in order) available[r.Color]++;
            var colors = new List<int>(order.Count);
            var taken = new int[colorRows.Length];
            foreach (var d in demand)
            {
                int c = d[0], n = Mathf.Min(d[1], available[c] - taken[c]);
                for (int k = 0; k < n; k++) colors.Add(c);
                taken[c] += Mathf.Max(0, n);
            }
            var skip = (int[])taken.Clone();
            foreach (var r in order)
            {
                if (skip[r.Color] > 0) { skip[r.Color]--; continue; }
                colors.Add(r.Color);
            }

            bool changed = false;
            for (int i = 0; i < order.Count; i++) if (order[i].Color != colors[i]) { order[i].Color = colors[i]; changed = true; }
            Array.Clear(ringColorRows, 0, ringColorRows.Length);
            foreach (var r in Ring) ringColorRows[r.Color]++;
            return changed;
        }

        /// <summary>The entering head row of feed f drops into its reserved slot k.</summary>
        void Land(int f, int k)
        {
            var feed = Feed[f];
            int i = 0;
            while (i < feed.Count && feed[i].Entering && feed[i].Slot != k) i++;
            if (i >= feed.Count || !feed[i].Entering) return;
            var row = feed[i];
            feed.RemoveAt(i);
            row.Entering = false; row.Slot = k; row.S = SlotPos(k);
            reserved[k] = false;
            slots[k] = row;
            Ring.Add(row);
            ringColorRows[row.Color]++;
        }
    }
}
