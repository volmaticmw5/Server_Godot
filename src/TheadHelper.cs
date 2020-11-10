using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class TheadHelper
{
	public bool abort;
	public Thread the_thread;
	public ConcurrentBag<Action> doAlwaysTasks = new ConcurrentBag<Action>();
	public ConcurrentBag<Action> QeuedActions = new ConcurrentBag<Action>();
	private int locked = 0;

	public TheadHelper(List<Action> doAlways = null)
	{
		if(doAlways != null)
			this.doAlwaysTasks = new ConcurrentBag<Action>(doAlways);
		the_thread = new Thread(new ThreadStart(() => Update()));
	}

	public async void addToQeue(Action action)
	{
		while (locked == 1)
			await Task.Delay(1);
		QeuedActions.Add(action);
	}

	public void Update()
	{
		while(!abort)
		{
			if(QeuedActions.Count > 0)
			{
				locked = 1;
				foreach (Action action in QeuedActions)
					action();

				QeuedActions.Clear();
				locked = 0;
			}

			foreach (Action action in doAlwaysTasks)
				action();

			Thread.Sleep(100); // Sleep for a bit, don't consume all the system resources for no reason
		}
	}
}