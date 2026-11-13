
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TuanTool
{
    /// <summary>
    /// Extension method dùng chung cho GameObject / Transform.
    /// </summary>
    public static class ExtensionMethod
    {
        /// <summary>
        /// Xoá toàn bộ con trực tiếp của GameObject.
        /// Trong Play mode dùng Destroy, trong Editor dùng DestroyImmediate.
        /// </summary>
        public static void DeleteAllChild(this GameObject obj)
        {
            DeleteAllChild(obj, obj);
        }

        /// <summary>
        /// Kiểm tra GameObject đã bị Destroy hay chưa.
        /// </summary>
        /// <param name="gameObject">GameObject cần kiểm tra</param>
        /// <returns>True nếu object đã bị UnityEngine đánh dấu destroy</returns>
        public static bool IsDestroyed(this GameObject gameObject)
        {
            // UnityEngine overloads the == opeator for the GameObject type
            // and returns null when the object has been destroyed, but
            // actually the object is still there but has not been cleaned up yet
            // if we test both we can determine if the object has been destroyed.
            return gameObject == null && !ReferenceEquals(gameObject, null);
        }

        /// <summary>
        /// Xoá tất cả con của transform, trừ <paramref name="exclusive"/>.
        /// </summary>
        public static void DeleteAllChild(this Transform transform, Transform exclusive)
        {
            DeleteAllChild(transform.gameObject, exclusive.gameObject);
        }

        /// <summary>
        /// Xoá tất cả con của transform.
        /// </summary>
        public static void DeleteAllChild(this Transform transform)
        {
            DeleteAllChild(transform.gameObject);
        }

        /// <summary>
        /// Xoá tất cả con của transform, giữ lại các transform trong <paramref name="exclusive"/>.
        /// </summary>
        public static void DeleteAllChild(this Transform transform, List<Transform> exclusive)
        {
            DeleteAllChild(transform.gameObject, exclusive.ConvertAll(_ => _.gameObject).ToList());
        }

        /// <summary>
        /// Xoá tất cả con của obj, trừ <paramref name="exclusive"/>.
        /// </summary>
        public static void DeleteAllChild(this GameObject obj, GameObject exclusive)
        {
            var childrens = new List<GameObject>();
            foreach (Transform child in obj.transform)
                childrens.Add(child.gameObject);

            foreach (var item in childrens)
            {
                if (item != exclusive)
                    if (Application.isPlaying)
                        Object.Destroy(item.gameObject);
                    else
                        Object.DestroyImmediate(item.gameObject);
            }


        }

        /// <summary>
        /// Xoá tất cả con của obj, giữ lại các GameObject trong <paramref name="exclusive"/>.
        /// </summary>
        public static void DeleteAllChild(this GameObject obj, List<GameObject> exclusive)
        {

            var childrens = new List<GameObject>();
            foreach (Transform child in obj.transform)
                childrens.Add(child.gameObject);

            foreach (var item in childrens)
            {
                if (!exclusive.Contains(item))
                    if (Application.isPlaying)
                        Object.Destroy(item.gameObject);
                    else
                        Object.DestroyImmediate(item.gameObject);
            }
        }
    }
}
