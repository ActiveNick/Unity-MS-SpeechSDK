using System;
using System.Collections.Generic;
using UnityEngine;


#if WINDOWS_UWP
/// <summary>
/// A helper class for dispatching actions to run on various Unity threads.
/// </summary>
static public class UnityDispatcher
{
	/// <summary>
	/// Schedules the specified action to be run on Unity's main thread.
	/// </summary>
	/// <param name="action">
	/// The action to run.
	/// </param>
	static public void InvokeOnAppThread(Action action)
	{
		if (UnityEngine.WSA.Application.RunningOnAppThread())
		{
			// Already on app thread, just run inline
			action();
		}
		else
		{
			// Schedule
			UnityEngine.WSA.Application.InvokeOnAppThread(() => action(), false);
		}
	}
}
#endif

#if !WINDOWS_UWP
/// <summary>
/// A helper class for dispatching actions to run on various Unity threads.
/// </summary>
public class UnityDispatcher : MonoBehaviour
{
    #region Member Variables
    static private UnityDispatcher instance;
    static private volatile bool queued = false;
    static private List<Action> backlog = new List<Action>(8);
    static private List<Action> actions = new List<Action>(8);
    #endregion // Member Variables

    #region Internal Methods
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static private void Initialize()
    {
        if (instance == null)
        {
            instance = new GameObject("Dispatcher").AddComponent<UnityDispatcher>();
            DontDestroyOnLoad(instance.gameObject);
        }
    }
    #endregion // Internal Methods

    #region Unity Overrides
    protected virtual void Update()
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
            {
                action();
            }

            actions.Clear();
        }
    }
    #endregion // Unity Overrides

    #region Public Methods
    /// <summary>
    /// Schedules the specified action to be run on Unity's main thread.
    /// </summary>
    /// <param name="action">
    /// The action to run.
    /// </param>
    static public void InvokeOnAppThread(Action action)
    {
        lock (backlog)
        {
            backlog.Add(action);
            queued = true;
        }
    }
    #endregion // Public Methods
}
#endif