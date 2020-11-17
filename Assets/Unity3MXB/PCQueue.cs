
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity3MXB;

public class StageTask
{
    public StageTask(PagedLOD pagedLOD)
    {
        _pagedLOD = pagedLOD;
    }

    public IEnumerator StageChildrenCo()
    {
        char[] slash = { '/', '\\' };
        for (int j = 0; j < _pagedLOD.ChildrenFiles.Count; ++j)
        {
            string file = _pagedLOD.ChildrenFiles[j];
            file = file.TrimStart(slash);
            Unity3MXBLoader loaderChild = new Unity3MXBLoader(_pagedLOD.dir);
            loaderChild.StagedChildren = _pagedLOD.StagedChildren;
            yield return loaderChild.LoadStreamCo(file);
        }
        _pagedLOD.childrenStatus = PagedLOD.ChildrenStatus.Staged;
    }

    public void StageChildren()
    {
        char[] slash = { '/', '\\' };
        for (int j = 0; j < _pagedLOD.ChildrenFiles.Count; ++j)
        {
            string file = _pagedLOD.ChildrenFiles[j];
            file = file.TrimStart(slash);
            Unity3MXBLoader loaderChild = new Unity3MXBLoader(_pagedLOD.dir);
            loaderChild.StagedChildren = _pagedLOD.StagedChildren;
            loaderChild.LoadStream(file);
        }
        _pagedLOD.childrenStatus = PagedLOD.ChildrenStatus.Staged;
    }

    private PagedLOD _pagedLOD;
}

public class PCQueue : MonoBehaviour
{
	private static PCQueue _current;
    Queue<StageTask> _itemQ = new Queue<StageTask>();
    static bool initialized;

    public static PCQueue Current
    {
        get
        {
            Initialize();
            return _current;
        }
    }


    public void Dispose()
    {
        Shutdown(true);
    }

#if !UNITY_WEBGL

    Thread[] _workers;
    public static int maxThreads = 4;
    public readonly object LockerForUser = new object();
    readonly object _locker = new object();

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

    public void EnqueueItem(StageTask item)
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
            StageTask item;
            lock (_locker)
            {
                while (_itemQ.Count == 0) Monitor.Wait(_locker);
                item = _itemQ.Dequeue();
            }
            if (item == null) return;         // This signals our exit.
            item.StageChildren();                           // Execute item.
        }
    }
#else
    static void Initialize()
    {
        if (!initialized)
        {

            if (!Application.isPlaying)
                return;
            initialized = true;
            var g = new GameObject("PCQueue");
            _current = g.AddComponent<PCQueue>();

            _current.StartCoroutine(_current.Consume());
        }
    }

    public void Shutdown(bool waitForWorkers)
    {
        EnqueueItem(null);
    }

    public void EnqueueItem(StageTask item)
    {
        _itemQ.Enqueue(item);           
    }

    IEnumerator Consume()
    {
        while (true)                        // Keep consuming until
        {                                   // told otherwise.
            StageTask item;
            if (_itemQ.Count > 0)
            {
                item = _itemQ.Dequeue();
                if (item == null) yield break;         // This signals our exit.
                yield return item.StageChildrenCo();                           // Execute item.
                yield return null;
            }
            yield return null;

        }
    }
#endif
}

