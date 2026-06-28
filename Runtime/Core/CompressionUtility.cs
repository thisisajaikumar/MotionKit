using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AjoyGames.MotionKit
{
    /// <summary>
    /// Dependency-free numeric compression helpers: IEEE-754 half packing and keyframe reduction. Used by
    /// recorders to shrink stored data without depending on Unity.Mathematics.
    /// </summary>
    public static class CompressionUtility
    {
        /// <summary>Packs a 32-bit float into a 16-bit half (round-to-nearest, with denormal/inf/nan handling).</summary>
        public static ushort FloatToHalf(float value)
        {
            FloatUIntUnion u = default;
            u.FloatValue = value;
            uint bits = u.UIntValue;

            uint sign = (bits >> 16) & 0x8000u;
            int exponent = (int)((bits >> 23) & 0xFFu) - 127 + 15;
            uint mantissa = bits & 0x007FFFFFu;

            if (exponent <= 0)
            {
                if (exponent < -10)
                    return (ushort)sign;
                mantissa |= 0x00800000u;
                int shift = 14 - exponent;
                uint h = mantissa >> shift;
                if ((mantissa & ((1u << shift) - 1u)) > (1u << (shift - 1)))
                    h++;
                return (ushort)(sign | h);
            }

            if (exponent >= 0x1F)
            {
                if (((bits >> 23) & 0xFFu) == 0xFFu && mantissa != 0)
                    return (ushort)(sign | 0x7E00u); // NaN
                return (ushort)(sign | 0x7C00u);     // Inf
            }

            uint result = sign | ((uint)exponent << 10) | (mantissa >> 13);
            if ((mantissa & 0x00001FFFu) > 0x00001000u)
                result++;
            return (ushort)result;
        }

        /// <summary>Unpacks a 16-bit half into a 32-bit float.</summary>
        public static float HalfToFloat(ushort half)
        {
            uint sign = (uint)(half & 0x8000) << 16;
            uint exponent = (uint)(half & 0x7C00) >> 10;
            uint mantissa = (uint)(half & 0x03FF);

            uint bits;
            if (exponent == 0)
            {
                if (mantissa == 0)
                {
                    bits = sign;
                }
                else
                {
                    exponent = 1;
                    while ((mantissa & 0x0400) == 0)
                    {
                        mantissa <<= 1;
                        exponent--;
                    }
                    mantissa &= 0x03FF;
                    bits = sign | ((exponent + (127 - 15)) << 23) | (mantissa << 13);
                }
            }
            else if (exponent == 0x1F)
            {
                bits = sign | 0x7F800000u | (mantissa << 13);
            }
            else
            {
                bits = sign | ((exponent + (127 - 15)) << 23) | (mantissa << 13);
            }

            FloatUIntUnion u = default;
            u.UIntValue = bits;
            return u.FloatValue;
        }

        /// <summary>Returns true if two floats are within <paramref name="epsilon"/> of each other.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Approximately(float a, float b, float epsilon)
        {
            return (a > b ? a - b : b - a) <= epsilon;
        }

        /// <summary>
        /// Removes redundant interior keyframes from parallel (time,value) lists when they lie on the line
        /// between their neighbours within <paramref name="epsilon"/>. Endpoints are always preserved.
        /// </summary>
        /// <returns>The new key count written back into the supplied lists.</returns>
        public static int ReduceLinearKeys(List<float> times, List<float> values, float epsilon)
        {
            int n = times.Count;
            if (n <= 2) return n;

            int write = 1;
            for (int i = 1; i < n - 1; i++)
            {
                float t0 = times[write - 1];
                float v0 = values[write - 1];
                float t2 = times[i + 1];
                float v2 = values[i + 1];
                float t1 = times[i];
                float v1 = values[i];

                float dt = t2 - t0;
                float interpolated = Mathf.Approximately(dt, 0f) ? v0 : Mathf.Lerp(v0, v2, (t1 - t0) / dt);
                if (!Approximately(interpolated, v1, epsilon))
                {
                    times[write] = t1;
                    values[write] = v1;
                    write++;
                }
            }

            times[write] = times[n - 1];
            values[write] = values[n - 1];
            write++;

            times.RemoveRange(write, n - write);
            values.RemoveRange(write, n - write);
            return write;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct FloatUIntUnion
        {
            [FieldOffset(0)] public float FloatValue;
            [FieldOffset(0)] public uint UIntValue;
        }
    }
}
