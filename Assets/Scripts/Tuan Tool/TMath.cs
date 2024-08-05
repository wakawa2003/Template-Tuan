using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace TuanTool
{
    //ver 1.2
    public static class TMath
    {
        /// <summary>
        ///  Clamp in range b1, b2.
        /// <br>exp: TMath.RemapClamp(1f, 0, 10, 0, 1) => 0.1</br>
        /// <br>exp: TMath.RemapClamp(1f, 10, 0, 0, 1) => 0.9</br>
        /// <br>exp: TMath.RemapClamp(1f, 3, 0, 0, 1) => 0,6666667</br>
        /// </summary>
        /// <param name="s"></param>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        public static float RemapClamp(float s, float a1, float a2, float b1, float b2)
        {
            return Mathf.Clamp(b1 + (s - a1) * (b2 - b1) / (a2 - a1), b1, b2);
        }

        public static float Remap(float s, float a1, float a2, float b1, float b2)
        {
            return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
        }

        public static Vector3 Bezier(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            return (1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * p1 + t * t * p2;
        }

        public static Vector3 Bezier(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return Mathf.Pow(1 - t, 3) * p0 + 3 * (1 - t) * (1 - t) * t * p1 + 3 * (1 - t) * t * t * p2 + Mathf.Pow(t, 3) * p3;
        }

        public static Vector2 GiaoDiem2DuongThang(Vector2 A, Vector2 B, Vector2 C, Vector2 D)
        {
            // Line AB represented as a1x + b1y = c1  
            float a1 = B.y - A.y;
            float b1 = A.x - B.x;
            float c1 = a1 * (A.x) + b1 * (A.y);

            // Line CD represented as a2x + b2y = c2  
            float a2 = D.y - C.y;
            float b2 = C.x - D.x;
            float c2 = a2 * (C.x) + b2 * (C.y);

            float determinant = a1 * b2 - a2 * b1;

            if (determinant == 0)
            {
                Debug.LogError("Hai duong thang song song!!");
                return new Vector2(float.MaxValue, float.MaxValue);
            }
            else
            {
                float x = (b2 * c1 - b1 * c2) / determinant;
                float y = (a1 * c2 - a2 * c1) / determinant;
                return new Vector2(x, y);
            }
        }

        /// <summary>
        /// Binary Search
        /// </summary>
        public static (int i_prev, int i_next, float prev, float next) FindPrevNext(List<float> list, float value)
        {
            int l = 0;
            int r = list.Count - 1;

            while (l <= r)
            {
                int m = l + (r - l) / 2;

                if (list[r] == value)
                {
                    return (r - 1, r, list[r - 1], list[r]);
                }
                if (m + 1 == list.Count)//cuoi danh sach
                {
                    return (r - 1, -1, list[r - 1], 0);
                }
                if (value >= list[m] && value < list[m + 1])
                {
                    return (m, m + 1, list[m], list[m + 1]);
                }

                // If x greater, ignore left half 
                if (list[m] < value)
                    l = m + 1;

                // If x is smaller, ignore right half 
                else
                    r = m - 1;
            }

            //Debug.LogError("Khong tim thay khoang gia tri cua: " + value);
            return (-1, -1, 0, 0);
        }

        /// <summary>
        /// Binary Search
        /// </summary>
        public static (int i_prev, int i_next, float prev, float next) FindPrevNext(float[] list, float value)
        {
            int l = 0;
            int r = list.Length - 1;

            while (l <= r)
            {
                int m = l + (r - l) / 2;

                if (list[r] == value)
                {
                    return (r - 1, r, list[r - 1], list[r]);
                }
                if (m + 1 == list.Length)//cuoi danh sach
                {
                    return (r - 1, -1, list[r - 1], 0);
                }
                if (value >= list[m] && value < list[m + 1])
                {
                    return (m, m + 1, list[m], list[m + 1]);
                }

                // If x greater, ignore left half 
                if (list[m] < value)
                    l = m + 1;

                // If x is smaller, ignore right half 
                else
                    r = m - 1;
            }

            //Debug.LogError("Khong tim thay khoang gia tri cua: " + value);
            return (-1, -1, 0, 0);
        }

        public static float RandomMinMax(Vector2 minMax)
        {
            return UnityEngine.Random.Range(minMax.x, minMax.y);
        }

        public static bool RandomPercent(int percent)
        {
            int t = UnityEngine.Random.Range(1, 101);
            return (t <= percent);
        }

        public static bool RandomPercent(float percent)
        {
            float t = UnityEngine.Random.Range(0f, 100f);
            return (t <= percent);
        }

        public static t RandomSelect<t>(params t[] numbers)
        {
            return numbers[UnityEngine.Random.Range(0, numbers.Length)];

        }

        public static t RandomSelect<t>(List<t> numbers)
        {
            return numbers[UnityEngine.Random.Range(0, numbers.Count)];
        }

        public static t RandomEnum<t>() where t : Enum
        {
            var a = Enum.GetValues(typeof(t));
            return (t)a.GetValue(UnityEngine.Random.Range(0, a.Length));

        }

        /// <summary>
        /// <image url="https://drive.google.com/open?id=124AB4tF_TKG6K_izD8hvDIwoW5vCPfI6" scale="0.5"/>
        /// Huong cua "return" la khong xac dinh tren duong tron.
        /// </summary>
        public static Vector3 RotateVector(Vector3 dir, float angle, float angleQuanhDir)
        {
            float D = Vector3.Magnitude(dir) * Mathf.Tan(angle * Mathf.Deg2Rad);
            Vector3 dirD = dir + GetPerpendicularXY(dir).normalized * D;
            dirD = Quaternion.AngleAxis(angleQuanhDir, dir) * dirD;
            return dirD;
        }

        /// <summary>
        /// Tra ve vuong goc dang vector3 (x,y,0).
        /// </summary>
        public static Vector3 GetPerpendicularXY(Vector3 dir)
        {
            if (dir.x == 0 && dir.y == 0 && dir.z != 0)
                return new Vector3(dir.z, 0, 0);
            return new Vector3(-dir.y, dir.x, 0);
        }

        /// <summary>
        /// Draw direction with Arrow.
        /// Dir cung la length.
        /// </summary>
        public static void DrawDirection(Vector3 origin, Vector3 dir, Color color, float lengthArrow = 0.2f, float angleArrow = 30)
        {
            float length = lengthArrow;
            Vector3 endPos = origin + dir;

            Debug.DrawLine(origin, endPos, color);

            //draw arow
            Debug.DrawLine(endPos, endPos + TMath.RotateVector(-dir, angleArrow, 0).normalized * length, color);
            Debug.DrawLine(endPos, endPos + TMath.RotateVector(-dir, angleArrow, 90).normalized * length, color);
            Debug.DrawLine(endPos, endPos + TMath.RotateVector(-dir, angleArrow, 180).normalized * length, color);
            Debug.DrawLine(endPos, endPos + TMath.RotateVector(-dir, angleArrow, 270).normalized * length, color);


        }


        /// <summary>
        /// Vuong goc.
        /// </summary>
        public static Vector3 PerpendicularXY(this Vector3 vector3)
        {
            return GetPerpendicularXY(vector3);
        }

        /// <summary>
        /// Exp:
        /// count = 100;
        /// i= 0 ;
        /// step= 103 => 3;
        /// step= -103 => 97;
        /// step= -4 => 96;
        /// </summary>
        /// <param name="countList"></param>
        /// <param name="currentIPosition"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public static int LoopStepCalculate(int countList, int currentIPosition, int step)
        {
            countList = Mathf.Abs(countList);
            step = step % countList;
            int t = (countList + currentIPosition + step) % countList;
            t = Mathf.Abs(t);
            return t;
        }

        /// <summary>
        /// Convert Rotation From 0 : 360 To -x:+x. 
        /// transform.localEulerAngles is 0:360 type. 
        /// Inspector Rotation is -x:+x type. 
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float Convert_0_360_To_NegativableAngle(float angle)
        {
            return (angle > 180) ? angle - 360 : angle;
        }


        public static Coroutine UpdateDuration(this MonoBehaviour monoBehaviour, float duration, float delay = 0, Action<float> OnUpdate = null, Action OnComplete = null)
        {
            IEnumerator IERunAnimator()
            {
                yield return new WaitForSeconds(delay);
                float start = Time.time;
                OnUpdate?.Invoke(0);
                while (Time.time <= start + duration)
                {
                    float time = (Time.time - start) / duration;
                    OnUpdate?.Invoke(Mathf.Clamp01(time));
                    yield return null;
                }
                OnUpdate?.Invoke(1);
                OnComplete?.Invoke();
            }
            return monoBehaviour.StartCoroutine(IERunAnimator());
        }

        public static Coroutine UpdateRepeat(this MonoBehaviour monoBehaviour, float delay, float repeatTime, Action<int> OnRepeat)
        {
            IEnumerator IERunAnimator()
            {
                yield return new WaitForSeconds(delay);
                float start = Time.time;
                int t = 0;
                while (true)
                {
                    if (Time.time >= start)
                    {
                        start += repeatTime;
                        t++;
                        OnRepeat?.Invoke(t);
                    }
                    yield return null;
                }
            }
            return monoBehaviour.StartCoroutine(IERunAnimator());
        }
        public static Coroutine UpdateRepeat(this MonoBehaviour monoBehaviour, float delay, int count, float repeatTime, Action<int> OnRepeat = null, Action OnComplete = null)
        {
            IEnumerator IERunAnimator()
            {
                yield return new WaitForSeconds(delay);
                float start = Time.time;
                int t = 0;
                while (count > t)
                {
                    if (Time.time >= start)
                    {
                        start += repeatTime;
                        t++;
                        OnRepeat?.Invoke(t);
                    }
                    yield return null;
                }
                OnComplete?.Invoke();
            }
            return monoBehaviour.StartCoroutine(IERunAnimator());
        }
        public static Coroutine UpdateRepeat(this MonoBehaviour monoBehaviour, float delay, float repeatTimeMin, float repeatTimeMax, Action<int> OnRepeat)
        {
            IEnumerator IERunAnimator()
            {
                yield return new WaitForSeconds(delay);
                float start = Time.time;
                int t = 0;
                while (true)
                {
                    if (Time.time >= start)
                    {
                        start += UnityEngine.Random.Range(repeatTimeMin, repeatTimeMax);
                        t++;
                        OnRepeat?.Invoke(t);
                    }
                    yield return null;
                }
            }
            return monoBehaviour.StartCoroutine(IERunAnimator());
        }


        public static Coroutine UpdateInfinity(this MonoBehaviour monoBehaviour, float delay, Action OnUpdate)
        {
            IEnumerator IERunAnimator()
            {
                yield return new WaitForSeconds(delay);
                while (true)
                {
                    OnUpdate?.Invoke();
                    yield return null;
                }
            }
            return monoBehaviour.StartCoroutine(IERunAnimator());
        }


        /// <summary>
        /// Same with Mathf.Sign() but have return 0.
        /// </summary>
        public static int Sign(int t)
        {
            return System.Math.Sign(t);
        }
        public static int Sign(float t)
        {
            return System.Math.Sign(t);
        }

    }
}