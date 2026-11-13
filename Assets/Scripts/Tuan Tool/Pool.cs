
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;

namespace TuanTool
{
    /// <summary>
    /// Object pool dựa trên GameObject. Mỗi PoolSlot là một loại prefab, spawn/unspawn bằng key là Name.
    /// </summary>
    public class Pool : MonoBehaviour, IPoolManager
    {

        /// <summary>
        /// Một loại object trong pool.
        /// </summary>
        [System.Serializable]
        public class PoolSlot
        {
            [Tooltip("Tự tạo sẵn object khi Init. Tắt để dùng danh sách _poolQueue có sẵn trong scene.")]
            public bool AutoInit = true;
            [Tooltip("Key dùng khi gọi Instantiate/UnSpawn. Tự điền theo tên prefab khi gán Object.")]
            public string Name;
            [Tooltip("Prefab sẽ được nhân bản vào pool.")]
            [OnValueChanged(nameof(OnObjectChange))]
            public GameObject Object;
            [Tooltip("Số lượng object tạo sẵn khi AutoInit bật.")]
            [HideIf(nameof(checkHideAmount))] public int Amount = 10;
            [HideInInspector] public int hintIndex = 0;
            [Tooltip("Danh sách object đang thuộc pool. Tự điền khi AutoInit, hoặc gán sẵn khi tắt AutoInit.")]
            public List<GameObject> _poolQueue = new List<GameObject>();
            [Tooltip("Transform cha chứa object của slot này. Để trống sẽ dùng Container Global của Pool.")]
            [SerializeField] public Transform Container;
            void OnObjectChange()
            {
                Name = Object != null ? Object.name : string.Empty;
            }
            bool checkHideAmount()
            {
                return !AutoInit;
            }
        }

        [Tooltip("Transform cha mặc định cho các object của slot không có Container riêng.")]
        [SerializeField] Transform containerGlobal;
        [Tooltip("Danh sách các loại object trong pool.")]
        [SerializeField] private List<PoolSlot> _poolSlots = new List<PoolSlot>();

        // Dictionary không serialize được trong Unity nên không đánh dấu [SerializeField], được dựng lại trong Init
        private Dictionary<string, PoolSlot> _poolDict = new Dictionary<string, PoolSlot>();

        bool isInit = false;

        public List<PoolSlot> PoolSlots { get => _poolSlots; set => _poolSlots = value; }
        public Dictionary<string, PoolSlot> PoolDict { get => _poolDict; set => _poolDict = value; }

        /// <summary>
        /// Tạo object sẵn cho các slot (chỉ chạy một lần). Tự gọi trong Start hoặc khi Instantiate/UnSpawn được gọi trước.
        /// </summary>
        public void Init()
        {
            if (!isInit)
            {
                for (int i = 0; i < PoolSlots.Count; i++)
                {
                    var poolSlot = PoolSlots[i];
                    if (poolSlot.AutoInit)
                    {
                        for (int j = 0; j < poolSlot.Amount; j++)
                            AddNewGameObjectForSlot(poolSlot);
                    }
                    else
                    {
                        // Object đã có sẵn trong _poolQueue: chỉ cần init một lần cho cả slot
                        foreach (var item in poolSlot._poolQueue)
                        {
                            InitForNewGameObject(item);
                        }
                    }
                    PoolDict.Add(poolSlot.Name, poolSlot);
                }
                isInit = true;
            }
        }

        private GameObject AddNewGameObjectForSlot(PoolSlot poolSlot)
        {
            Transform parent = poolSlot.Container == null ? containerGlobal : poolSlot.Container;
            GameObject newGameObject = Instantiate(poolSlot.Object, parent);

            InitForNewGameObject(newGameObject);
            poolSlot._poolQueue.Add(newGameObject);
            InCreasePoolIndex(poolSlot);
            return newGameObject;
        }

        private void InitForNewGameObject(GameObject newGameObject)
        {
            //Debug.Log($"init ipoool");
            foreach (var item in newGameObject.GetComponentsInChildren<IPool>(true))
            {
                item.PoolMannagerOwner = this;
            }
            newGameObject.SetActive(false);
        }

        private void Start()
        {
            Init();

        }

        /// <summary>
        /// Spawn object theo key tại vị trí và góc quay chỉ định.
        /// </summary>
        /// <param name="name">Key của PoolSlot (PoolSlot.Name)</param>
        public GameObject Instantiate(string name, Vector3 pos, Quaternion quaternion)
        {
            var a = Instantiate(name, g =>
            {
                g.transform.SetPositionAndRotation(pos, quaternion);
            });
            return a;
        }

        /// <summary>
        /// Spawn object theo key. Gọi callback trước khi bật object và trước OnPoolSpawn của các IPool con.
        /// Nếu hết object rảnh, tự tạo thêm một object mới.
        /// </summary>
        /// <param name="name">Key của PoolSlot (PoolSlot.Name)</param>
        /// <param name="onBeforeCallSpawnPool">Callback chỉnh object trước khi spawn (vị trí, parent...)</param>
        public GameObject Instantiate(string name, Action<GameObject> onBeforeCallSpawnPool = null)
        {
            Init();
            if (!PoolDict.ContainsKey(name))
            {
                Debug.LogError("Pool ko co Key: " + name);
                return null;
            }
            var poolSLot = PoolDict[name];
            GameObject gameObject1 = getNextObj(poolSLot);

            if (gameObject1 == null)
            {
                gameObject1 = AddNewGameObjectForSlot(PoolDict[name]);
                Debug.LogWarning($"Can't find: {name} inactive in pool => spawn new object!!");
            }


            gameObject1.SetActive(true);
            onBeforeCallSpawnPool?.Invoke(gameObject1);
            foreach (var item in gameObject1.GetComponentsInChildren<IPool>(true))
            {
                item.OnPoolSpawn();
            }

            return gameObject1;

            static GameObject getNextObj(PoolSlot poolSLot)
            {
                for (int i = 0; i < poolSLot._poolQueue.Count; i++)
                {
                    InCreasePoolIndex(poolSLot);
                    var obj = poolSLot._poolQueue[poolSLot.hintIndex];
                    if (!obj.activeSelf)
                        return obj;
                }

                return null;
            }
        }

        private static void InCreasePoolIndex(PoolSlot poolSLot)
        {
            poolSLot.hintIndex += 1;
            poolSLot.hintIndex = poolSLot.hintIndex % (poolSLot._poolQueue.Count);
        }

        /// <summary>
        /// Trả object về pool: gọi OnPoolUnSpawn của các IPool con rồi tắt object.
        /// </summary>
        /// <param name="go">Object đang spawn cần trả về pool</param>
        public void UnSpawn(GameObject go)
        {
            Init();
            foreach (var item in go.GetComponentsInChildren<IPool>(true))
            {
                item.OnPoolUnSpawn();
            }
            go.SetActive(false);
        }

    }

    /// <summary>
    /// Interface cho manager quản lý spawn/unspawn (Pool implement).
    /// </summary>
    public interface IPoolManager
    {
        /// <summary>Spawn object theo key tại vị trí và góc quay.</summary>
        GameObject Instantiate(string name, Vector3 pos, Quaternion quaternion);
        /// <summary>Trả object về pool.</summary>
        public void UnSpawn(GameObject go);
    }

    /// <summary>
    /// Đánh dấu component cần nhận sự kiện khi được spawn/unspawn từ Pool.
    /// </summary>
    public interface IPool
    {
        GameObject gameObject { get; }
        /// <summary>Pool đang quản lý object này, được gán tự động khi Init.</summary>
        IPoolManager PoolMannagerOwner { get; set; }
        /// <summary>Trả chính object này về pool.</summary>
        void UnSpawn() { PoolMannagerOwner.UnSpawn(gameObject); }
        /// <summary>Gọi khi object được lấy ra khỏi pool.</summary>
        void OnPoolSpawn();
        /// <summary>Gọi khi object được trả về pool.</summary>
        void OnPoolUnSpawn();
    }

}