
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TuanTool
{
    public class Pool : MonoBehaviour, IPoolMannager
    {

        [System.Serializable]
        private class PoolSlot
        {
            public bool AutoInit = true;
            public string Name;
            [OnValueChanged(nameof(OnObjectChange))]
            public GameObject Object;
            [HideIf(nameof(checkHideAmount))] public int Amount = 10;
            [HideInInspector] public int hintIndex = 0;
            public List<GameObject> _poolQueue = new List<GameObject>();
            [SerializeField] public Transform Container;
            void OnObjectChange()
            {
                Name = Object.name;
            }
            bool checkHideAmount()
            {
                return !AutoInit;
            }
        }

        [SerializeField] Transform containerGlobal;
        [SerializeField] private List<PoolSlot> _poolSlots = new List<PoolSlot>();

        [SerializeField] private Dictionary<string, PoolSlot> _poolDict = new Dictionary<string, PoolSlot>();

        bool isInit = false;
        public void Init()
        {
            if (!isInit)
            {
                for (int i = 0; i < _poolSlots.Count; i++)
                {
                    for (int j = 0; j < _poolSlots[i].Amount; j++)
                    {
                        var poolSlot = _poolSlots[i];
                        if (poolSlot.AutoInit)
                            AddNewGameObjectForSlot(poolSlot);
                        else
                        {
                            //init
                            foreach (var item in poolSlot._poolQueue)
                            {
                                InitForNewGameObject(item);
                            }
                        }
                    }
                    _poolDict.Add(_poolSlots[i].Name, _poolSlots[i]);
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
            poolSlot.hintIndex = poolSlot._poolQueue.Count - 1;
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

        //[Button]
        //void TestSpawn(string s = "Sphere")
        //{
        //    var a = Instantiate(s, transform.position, Quaternion.identity);
        //}

        public GameObject Instantiate(string name, Vector3 pos, Quaternion quaternion)
        {
            var a = Instantiate(name);
            a.transform.SetPositionAndRotation(pos, quaternion);
            return a;
        }

        public GameObject Instantiate(string name)
        {
            Init();
            if (!_poolDict.ContainsKey(name))
            {
                Debug.LogError("Pool ko co Key: " + name);
                return null;
            }
            var poolSLot = _poolDict[name];
            //GameObject gameObject1 = _poolDict[name]._poolQueue.Peek();
            GameObject gameObject1 = getInActiveObject(poolSLot);
            if (gameObject1 == null)
            {
                gameObject1 = AddNewGameObjectForSlot(_poolDict[name]);
            }
            if (gameObject1.activeSelf)
            {
                gameObject1 = AddNewGameObjectForSlot(_poolDict[name]);
                Debug.LogError($"Obj {gameObject1.name} is in use!");
            }


            gameObject1.SetActive(true);
            foreach (var item in gameObject1.GetComponentsInChildren<IPool>(true))
            {
                item.OnPoolSpawn();
            }
            //gameObject1.SendMessage("OnPoolSpawn", SendMessageOptions.DontRequireReceiver);

            return gameObject1;

            static GameObject getInActiveObject(PoolSlot poolSLot)
            {
                for (int i = 0; i < poolSLot._poolQueue.Count; i++)
                {
                    poolSLot.hintIndex += 1;
                    poolSLot.hintIndex = poolSLot.hintIndex % (poolSLot._poolQueue.Count);
                    var obj = poolSLot._poolQueue[poolSLot.hintIndex];
                    if (!obj.activeSelf)
                        return obj;
                }

                return null;
            }
        }

        public void UnSpawn(GameObject go)
        {
            Init();
            foreach (var item in go.GetComponentsInChildren<IPool>(true))
            {
                item.OnPoolUnSpawn();
            }
            //go.SendMessage("OnPoolUnSpawn", SendMessageOptions.DontRequireReceiver);
            go.SetActive(false);
        }

    }

    public interface IPoolMannager
    {
        GameObject Instantiate(string name, Vector3 pos, Quaternion quaternion);
        public void UnSpawn(GameObject go);
    }

    public interface IPool
    {
        GameObject gameObject { get; }
        IPoolMannager PoolMannagerOwner { get; set; }
        //public void SetPoolManager(IPoolMannager poolMannager) { PoolMannagerOwner = poolMannager; Debug.Log($"set pool ne!!"); }
        void UnSpawn() { PoolMannagerOwner.UnSpawn(gameObject); }
        void OnPoolSpawn();
        void OnPoolUnSpawn();
    }

}