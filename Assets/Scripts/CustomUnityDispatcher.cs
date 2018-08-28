using System.Collections.Generic;
using System.Threading;
using System;
using UnityEngine;

#if !WINDOWS_UWP
public class CustomUnityDispatcher : MonoBehaviour
{

    static CustomUnityDispatcher instance;
    static volatile bool queued = false;
    static List<Action> backlog = new List<Action>(8);
    static List<Action> actions = new List<Action>(8);

    public static void RunAsync(Action action)
    {
        ThreadPool.QueueUserWorkItem(o => action());
    }

    public static void RunAsync(Action<object> action, object state)
    {
        ThreadPool.QueueUserWorkItem(o => action(o), state);
    }

    public static void RunOnMainThread(Action action)
    {
        lock (backlog)
        {
            backlog.Add(action);
            queued = true;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance == null)
        {
            instance = new GameObject("Dispatcher").AddComponent<CustomUnityDispatcher>();
            DontDestroyOnLoad(instance.gameObject);
        }
    }

    private void Update()
    {
        if (queued)
        {
            lock (backlog)
            {
                var tmp = actions;
                actions = backlog;
                backlog = tmp;
                queued = false;
            }

            foreach (var action in actions)
                action();

            actions.Clear();
        }
    }
}
#else

// Dummy empty shim for UWP to make sure the build doesn't break.
// This class is not needed for UWP since we already have access
// to a WSA Dispatcher.
public class CustomUnityDispatcher : MonoBehaviour {

}
#endif
