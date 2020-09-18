using System;
using System.Collections.Generic;

class ThreadManager
{
	// Main
	private static readonly List<Action> executeOnMainThread = new List<Action>();
	private static readonly List<Action> executeCopiedOnMainThread = new List<Action>();
	private static bool actionToExecuteOnMainThread = false;

	// OtherThread (todo)
	private static readonly List<Action> executeOnAuthThread = new List<Action>();
	private static readonly List<Action> executeCopiedOnAuthThread = new List<Action>();
	private static bool actionToExecuteOnAuthThread = false;

	/// <summary>Sets an action to be executed on the main thread.</summary>
	/// <param name="_action">The action to be executed on the main thread.</param>
	public static void ExecuteOnMainThread(Action _action)
	{
		if (_action == null)
		{
			Logger.Syslog("No action to execute on main thread!");
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
}