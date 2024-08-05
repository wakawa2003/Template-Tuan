
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

public class TimeScaleController : MonoBehaviour
{

    [SerializeField] List<SlotTimeScale> slotTimeScales = new List<SlotTimeScale>();


    #region Singleton
    private static TimeScaleController ins;
    public static TimeScaleController Ins
    {
        get
        {
            if (ins == null)
            {
                var a = FindObjectOfType<TimeScaleController>();
                a?.Awake();
            }
            return ins;
        }
        set => ins = value;
    }
    #endregion

    private void Awake()
    {
        #region Singleton
        if (ins == null)
            ins = this;
        else
        {
            if (ins != this)
                Destroy(gameObject);
            return;
        }
        #endregion
    }

    void OnDestroy()
    {
        Time.timeScale = 1;
    }

    public SlotTimeScale CreatSlot(Behaviour source, float timeScale)
    {
        var newSlot = new SlotTimeScale(source, timeScale);
        AddSlot(newSlot);
        return newSlot;
    }

    public void AddSlot(SlotTimeScale timeScale)
    {
        if (!slotTimeScales.Contains(timeScale))
            slotTimeScales.Add(timeScale);
    }

    public void RemoveSlot(SlotTimeScale timeScale)
    {
        if (slotTimeScales.Contains(timeScale))
            slotTimeScales.Remove(timeScale);
    }

    private void Update()
    {
        float t = 1;
        foreach (var item in slotTimeScales)
        {
            t *= item.timeScale;
        }
        Time.timeScale = t;
        //Debug.Log($"time scale: {Time.timeScale}");
    }

    [System.Serializable]
    public class SlotTimeScale
    {
        public string Description;
        public Behaviour source;
        public float timeScale;

        public SlotTimeScale(Behaviour source, float timeScale)
        {
            this.source = source;
            this.timeScale = timeScale;
        }
    }
}