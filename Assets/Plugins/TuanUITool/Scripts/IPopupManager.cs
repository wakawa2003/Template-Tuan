using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AYellowpaper;
using UnityEngine;

namespace UITool.Popup
{
    public interface IPopupManager
    {
        public string AddShowPopupRequest(IPopup popup, object payloadData = null);
        public ReadOnlyCollection<InterfaceReference<IPopup>> PopupsOpening { get; }
    }
}