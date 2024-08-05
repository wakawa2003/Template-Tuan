
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ExtensionMethod
{
    public static void DeleteAllChild(this GameObject obj)
    {
        DeleteAllChild(obj, obj);
    }


    public static void DeleteAllChild(this Transform transform, Transform exclusive)
    {

        DeleteAllChild(transform.gameObject, exclusive.gameObject);
    }

    public static void DeleteAllChild(this Transform transform)
    {

        DeleteAllChild(transform.gameObject);
    }


    public static void DeleteAllChild(this Transform transform, List<Transform> exclusive)
    {

        DeleteAllChild(transform.gameObject, exclusive.ConvertAll(_ => _.gameObject).ToList());
    }

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


    public static int FindIndex<T>(this List<T> list, T obj)
    {
        int j = -1;
        for (int i = 0; i < list.Count; i++)
        {
            //if (EqualityComparer<T>.Default.Equals(list[i], obj))
            if (ReferenceEquals(list[i], obj))
            {
                j = i;
                break;
            }
        }

        if (j != -1)
            return j;
        else
        {
            Debug.LogError("Khong co phan tu trong mang!");
            return -1;
        }
    }


}
