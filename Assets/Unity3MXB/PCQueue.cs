
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
            Unity3MXBLoader loaderChild = new Unity3MXBLoader(_pagedLOD);
            yield return loaderChild.LoadStreamCo(file);
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
}

