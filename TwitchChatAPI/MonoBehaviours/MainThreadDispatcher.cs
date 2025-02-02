using System.Collections.Generic;
using UnityEngine;

namespace com.github.zehsteam.TwitchChatAPI.MonoBehaviours;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class MainThreadDispatcher : MonoBehaviour
{
    public static MainThreadDispatcher Instance { get; private set; }

    private static readonly Queue<System.Action> _actions = [];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
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

    public static void Enqueue(System.Action action)
    {
        lock (_actions)
        {
            _actions.Enqueue(action);
        }
    }
}
