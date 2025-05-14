using System;
using System.Collections.Generic;
using UnityEngine;

namespace TwitchChatAPI.MonoBehaviours;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class MainThreadDispatcher : MonoBehaviour
{
    public static MainThreadDispatcher Instance { get; private set; }

    private static readonly Queue<Action> _actions = [];

    public static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        var gameObject = new GameObject($"{MyPluginInfo.PLUGIN_NAME} MainThreadDispatcher")
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        DontDestroyOnLoad(gameObject);

        gameObject.AddComponent<MainThreadDispatcher>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Logger.LogInfo($"Spawned \"{gameObject.name}\"", extended: true);
    }

    private void Update()
    {
        lock (_actions)
        {
            while (_actions.Count > 0)
            {
                _actions.Dequeue()?.Invoke();
            }
        }
    }

    public static void Enqueue(Action action)
    {
        lock (_actions)
        {
            _actions.Enqueue(action);
        }
    }
}
