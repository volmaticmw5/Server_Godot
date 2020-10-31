using System;
using System.Collections.Generic;

class ThreadManager
{
	// Main
	private static readonly List<Action> executeOnMainThread = new List<Action>();
	private static readonly List<Action> executeCopiedOnMainThread = new List<Action>();
	private static bool actionToExecuteOnMainThread = false;

	// Map Thread
	private static readonly List<Action> executeOnMapThread = new List<Action>();
	private static readonly List<Action> executeCopiedOnMapThread = new List<Action>();
	private static bool actionToExecuteOnMapThread = false;

	// Player Thread
	private static readonly List<Action> executeOnPlayerThread = new List<Action>();
	private static readonly List<Action> executeCopiedOnPlayerThread = new List<Action>();
	private static bool actionToExecuteOnPlayerThread = false;

	/// <summary>Sets an action to be executed on the main thread.</summary>
	/// <param name="_action">The action to be executed on the main thread.</param>
	public static void ExecuteOnMainThread(Action _action)
	{
		if (_action == null)
		{
			Logger.Syserr("No action to execute on main thread!");
			return;
		}

		lock (executeOnMainThread)
		{
			executeOnMainThread.Add(_action);
			actionToExecuteOnMainThread = true;
		}
	}

	/// <summary>Executes all code meant to run on the main thread. NOTE: Call this ONLY from the main thread.</summary>
	public static void UpdateMain()
	{
		if (actionToExecuteOnMainThread)
		{
			executeCopiedOnMainThread.Clear();
			lock (executeOnMainThread)
			{
				executeCopiedOnMainThread.AddRange(executeOnMainThread);
				executeOnMainThread.Clear();
				actionToExecuteOnMainThread = false;
			}

			for (int i = 0; i < executeCopiedOnMainThread.Count; i++)
			{
				executeCopiedOnMainThread[i]();
			}
		}
	}

	/// <summary>Sets an action to be executed on the map thread.</summary>
	/// <param name="_action">The action to be executed on the map thread.</param>
	public static void ExecuteOnMapThread(Action _action)
	{
		if (_action == null)
		{
			Logger.Syserr("No action to execute on main thread!");
			return;
		}

		lock (executeOnMapThread)
		{
			executeOnMapThread.Add(_action);
			actionToExecuteOnMapThread = true;
		}
	}

	/// <summary>Executes all code meant to run on the map thread. NOTE: Call this ONLY from the main thread.</summary>
	public static void UpdateMapThread()
	{
		if (actionToExecuteOnMapThread)
		{
			executeCopiedOnMapThread.Clear();
			lock (executeOnMapThread)
			{
				executeCopiedOnMapThread.AddRange(executeOnMapThread);
				executeOnMapThread.Clear();
				actionToExecuteOnMapThread = false;
			}

			for (int i = 0; i < executeCopiedOnMapThread.Count; i++)
			{
				executeCopiedOnMapThread[i]();
			}
		}
	}

    public static void UpdatePlayerThread()
    {
		if (actionToExecuteOnPlayerThread)
		{
			executeCopiedOnPlayerThread.Clear();
			lock (executeOnPlayerThread)
			{
				executeCopiedOnMapThread.AddRange(executeOnPlayerThread);
				executeOnPlayerThread.Clear();
				actionToExecuteOnPlayerThread = false;
			}

			for (int i = 0; i < executeCopiedOnPlayerThread.Count; i++)
			{
				executeCopiedOnPlayerThread[i]();
			}
		}
	}
}