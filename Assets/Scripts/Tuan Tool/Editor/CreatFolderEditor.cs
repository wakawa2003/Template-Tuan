using System.IO;
using UnityEditor;
using UnityEngine;

namespace TuanTool
{
    /// <summary>
    /// Menu tạo nhanh các folder chuẩn (Prefabs, Animations, Scripts, Materials, Textures)
    /// ngay tại folder đang chọn trong Project. Folder đã tồn tại sẽ được bỏ qua.
    /// </summary>
    public class CreatFolderEditor
    {
        const string menu = "Create/";

        /// <summary>Tạo folder "Prefabs".</summary>
        [MenuItem(menu + "Create folder Prefabs")]
        public static void CreateFolderPrefabs()
        {
            TryCreatFolder("Prefabs");
        }

        /// <summary>Tạo folder "Animations".</summary>
        [MenuItem(menu + "Create folder Animations")]
        public static void CreateFolderAnimations()
        {
            TryCreatFolder("Animations");
        }

        /// <summary>Tạo folder "Scripts".</summary>
        [MenuItem(menu + "Create folder Scripts")]
        public static void CreateFolderScripts()
        {
            TryCreatFolder("Scripts");
        }

        /// <summary>Tạo folder "Materials".</summary>
        [MenuItem(menu + "Create folder Materials")]
        public static void CreateFolderMaterials()
        {
            TryCreatFolder("Materials");
        }

        /// <summary>Tạo folder "Textures".</summary>
        [MenuItem(menu + "Create folder Textures")]
        public static void CreateFolderTextures()
        {
            TryCreatFolder("Textures");
        }

        /// <summary>Tạo cùng lúc folder "Materials" và "Textures".</summary>
        [MenuItem(menu + "Create folder Textures + Materials")]
        public static void CreateFolderTexturesAndMaterials()
        {
            CreateFolderMaterials();
            CreateFolderTextures();
        }

        /// <summary>
        /// Tạo folder con <paramref name="name"/> trong folder hiện tại nếu chưa tồn tại.
        /// </summary>
        private static void TryCreatFolder(string name)
        {
            var currentFolder = GetSelectedPathOrFallback();
            if (!AssetDatabase.IsValidFolder(currentFolder + "/" + name))
                AssetDatabase.CreateFolder(currentFolder, name);
        }

        /// <summary>
        /// Lấy đường dẫn folder đích: nếu chọn folder thì dùng chính folder đó,
        /// nếu chọn file thì dùng folder chứa file đó, không chọn gì thì dùng "Assets".
        /// </summary>
        /// <returns>Đường dẫn folder dạng "Assets/..."</returns>
        public static string GetSelectedPathOrFallback()
        {
            string path = "Assets";

            foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }
            return path;
        }
    }

}
