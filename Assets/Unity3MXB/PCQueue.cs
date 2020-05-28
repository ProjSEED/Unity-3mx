using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

public class PCQueue : MonoBehaviour
{
	private static PCQueue _current;
    public static int maxThreads = 4;

    public readonly object LockerForUser = new object();

    readonly object _locker = new object();
    Thread[] _workers;
    Queue<Action> _itemQ = new Queue<Action>();
    public static PCQueue Current
    {
        get
        {
            Initialize();
            return _current;
        }
    }

    static bool initialized;

    static void Initialize()
    {
        if (!initialized)
        {

            if (!Application.isPlaying)
                return;
            initialized = true;
            var g = new GameObject("PCQueue");
            _current = g.AddComponent<PCQueue>();
            _current._workers = new Thread[maxThreads];

            // Create and start a separate thread for each worker
            for (int i = 0; i < maxThreads; i++)
            {
                _current._workers[i] = new Thread(_current.Consume);
                _current._workers[i].IsBackground = true;
                _current._workers[i].Start();
            }
        }
    }

    public void Dispose()
    {
        Shutdown(true);
    }

    public void Shutdown(bool waitForWorkers)
    {
        // Enqueue one null item per worker to make each exit.
        foreach (Thread worker in _workers)
            EnqueueItem(null);

        // Wait for workers to finish
        if (waitForWorkers)
            foreach (Thread worker in _workers)
                worker.Join();
    }

    public void EnqueueItem(Action item)
    {
        lock (_locker)
        {
            _itemQ.Enqueue(item);           // We must pulse because we're
            Monitor.Pulse(_locker);         // changing a blocking condition.
        }
    }

    void Consume()
    {
        while (true)                        // Keep consuming until
        {                                   // told otherwise.
            Action item;
            lock (_locker)
            {
                while (_itemQ.Count == 0) Monitor.Wait(_locker);
                item = _itemQ.Dequeue();
            }
            if (item == null) return;         // This signals our exit.
            item();                           // Execute item.
        }
    }
}