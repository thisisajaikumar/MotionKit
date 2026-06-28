namespace AjoyGames.MotionKit
{
    /// <summary>
    /// Allocation-free keyframe lookup shared by all track players. Uses a per-player cursor hint so forward
    /// playback is O(1) amortized, falling back to binary search on seeks/scrubs.
    /// </summary>
    public static class KeyframeSearch
    {
        /// <summary>
        /// Returns the index <c>i</c> such that <c>times[i] &lt;= t &lt; times[i+1]</c>, clamped to
        /// <c>[0, count-1]</c>. <paramref name="hint"/> is read and updated to accelerate sequential queries.
        /// </summary>
        public static int FindLower(float[] times, int count, float t, ref int hint)
        {
            if (count <= 1) return 0;

            int last = count - 1;
            if (t <= times[0]) { hint = 0; return 0; }
            if (t >= times[last]) { hint = last; return last; }

            if (hint >= 0 && hint < last)
            {
                if (t >= times[hint] && t < times[hint + 1])
                    return hint;
                if (hint + 1 < last && t >= times[hint + 1] && t < times[hint + 2])
                {
                    hint += 1;
                    return hint;
                }
            }

            int lo = 0, hi = last;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                if (times[mid] <= t)
                {
                    if (mid == last || times[mid + 1] > t)
                    {
                        hint = mid;
                        return mid;
                    }
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            hint = 0;
            return 0;
        }

        /// <summary>Normalized interpolation factor between keys <paramref name="i"/> and i+1 for time t.</summary>
        public static float SegmentT(float[] times, int count, int i, float t)
        {
            if (count <= 1 || i >= count - 1) return 0f;
            float t0 = times[i];
            float t1 = times[i + 1];
            float dt = t1 - t0;
            if (dt <= 0f) return 0f;
            float f = (t - t0) / dt;
            return f < 0f ? 0f : (f > 1f ? 1f : f);
        }
    }
}
