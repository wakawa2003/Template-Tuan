
using UnityEngine;

namespace UITool.Popup
{
    public static class ExtensionMethod
    {

        /// <summary>
        /// Inverts the X axis (time) of an AnimationCurve.
        /// </summary>
        public static AnimationCurve InvertX(this AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
                return new AnimationCurve();

            float maxTime = curve[curve.length - 1].time;
            Keyframe[] keys = new Keyframe[curve.length];

            for (int i = 0; i < curve.length; i++)
            {
                var k = curve[i];
                float newTime = maxTime - k.time;

                // Đảo chiều tangents để giữ đúng hướng
                keys[i] = new Keyframe(newTime, k.value, -k.inTangent, -k.outTangent);
            }

            // Sắp xếp lại key theo time tăng dần
            System.Array.Sort(keys, (a, b) => a.time.CompareTo(b.time));

            return new AnimationCurve(keys);
        }
    }
}