using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugPanel : MonoBehaviour
{
    static DebugPanel _singleton;
    public static DebugPanel singleton
    {
        get
        {
            if (!_singleton)
                _singleton = FindObjectOfType<DebugPanel>();

            return _singleton;
        }
    }

    public TextMeshPro tmpro;

    public int maxLength = 240;

    public static void Log(string message)
    {
        if (!singleton)
            return;

        singleton.tmpro.text += "\n" + message;

        if (singleton.tmpro.text.Length > singleton.maxLength)
            singleton.tmpro.text = singleton.tmpro.text.Substring(singleton.tmpro.text.Length - singleton.maxLength, singleton.maxLength);
    }

    public static void Clear()
    {
        if (!singleton)
            return;

        singleton.tmpro.text = "";
    }
}
