using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RSG;

namespace Unity3mx
{
    public class CamState
    {
        public Vector4 pixelSizeVector;
        public Plane[] planes;
        public Vector3 position;
    }

    public class PagedLOD
    {
        public enum ChildrenStatus
        {
            Unstaged = 0,   // CommitedChildren.Count = 0
            Staging,        // CommitedChildren.Count = Unknown
            Staged,         // CommitedChildren.Count = Known
            Commited        // CommitedChildren.Count = Known
        };

        public Unity3mxComponent unity3mxComponent = null;

        public string dir;
        public GameObject Go;  // one node could contains more than one mesh, use this GameObject as a group, insert each mesh to a child GameObject
        public MeshRenderer[] renderers = null;
        private bool HasColliders = false;
        public bool IsPointCloud = false;

        public Vector3 BBMin;
        public Vector3 BBMax;
        public TileBoundingSphere BoundingSphere;
        public float MaxScreenDiameter;

        public ChildrenStatus childrenStatus;     
        public List<string> ChildrenFiles;          

        public List<PagedLOD> CommitedChildren;

        public int FrameNumberOfLastTraversal;

        public int Depth;

        public PagedLOD(string name, string dir, PagedLOD parent)
        {
            this.dir = dir;

            this.Go = new GameObject();
            this.Go.name = name;
            this.Go.transform.SetParent(parent.Go.transform, false);
            //this.Go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

            this.childrenStatus = ChildrenStatus.Unstaged;

            this.CommitedChildren = new List<PagedLOD>();

            this.FrameNumberOfLastTraversal = -1;

            this.Depth = parent.Depth + 1;
        }

        public PagedLOD(string name, string dir, Transform parent, int depth)
        {
            this.dir = dir;

            this.Go = new GameObject();
            this.Go.name = name;
            this.Go.transform.SetParent(parent, false);
            //this.Go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

            this.childrenStatus = ChildrenStatus.Unstaged;

            this.CommitedChildren = new List<PagedLOD>();

            this.FrameNumberOfLastTraversal = -1;

            this.Depth = depth;
        }

        private void EnableRenderer(bool enabled)
        {
            if (this.renderers == null) return;
            for (int i = 0; i < this.renderers.Length; i++)
            {
                var mr = this.renderers[i];
                if (enabled != mr.enabled)
                {
                    mr.enabled = enabled;
                }
                if (mr.enabled == true)
                {
                    if ((this.unity3mxComponent.MaterialOverride != null) && (mr.sharedMaterial != this.unity3mxComponent.MaterialOverride))
                    {
                        mr.sharedMaterial = this.unity3mxComponent.MaterialOverride;
                    }
                    if (mr.receiveShadows != this.unity3mxComponent.ReceiveShadows)
                    {
                        mr.receiveShadows = this.unity3mxComponent.ReceiveShadows;
                    }
                    if (this.unity3mxComponent.AddColliders)
                    {
                        if (this.HasColliders == false)
                        {
                            MeshCollider collider = mr.gameObject.AddComponent<MeshCollider>();
                            collider.sharedMesh = mr.gameObject.GetComponent<MeshFilter>().mesh;
                            this.HasColliders = true;
                        }
                    }
                    else
                    {
                        if (this.HasColliders)
                        {
                            GameObject.Destroy(mr.gameObject.GetComponent<MeshCollider>());
                            this.HasColliders = false;
                        }
                    }
                }
            }
        }

        bool MarkStagingChildren()
        {
            // recursively check every child's children's status to see if they are staging, if true, do NOT destory this child
            if (this.childrenStatus == ChildrenStatus.Unstaged)
            {
                return false;
            }
            else if (this.childrenStatus == ChildrenStatus.Staging)
            {
                this.unity3mxComponent.LRUCache.MarkUsed(this);
                return true;
            }
            else if (this.childrenStatus == ChildrenStatus.Staged)
            {
                this.childrenStatus = ChildrenStatus.Unstaged;
                return false;
            }

            bool hasStagingChid = false;
            foreach (PagedLOD child in this.CommitedChildren)
            {
                if (child.childrenStatus == ChildrenStatus.Staging)
                {
                    hasStagingChid = true;
                }
                else
                {
                    hasStagingChid = hasStagingChid | child.MarkStagingChildren();
                }
            }
            if(hasStagingChid)
            {
                this.unity3mxComponent.LRUCache.MarkUsed(this);
            }
            return hasStagingChid;
        }

        public void UnloadChildren()
        {       
            foreach (PagedLOD child in this.CommitedChildren)
            {
                GameObject.Destroy(child.Go);
            }
            this.CommitedChildren.Clear();
            this.childrenStatus = ChildrenStatus.Unstaged;
        }

        public IEnumerator StageChildrenCo(Promise<bool> finished)
        {
            char[] slash = { '/', '\\' };
            for (int j = 0; j < this.ChildrenFiles.Count; ++j)
            {
                string file = this.ChildrenFiles[j];
                file = file.TrimStart(slash);
                Unity3mxLoader loaderChild = new Unity3mxLoader(this);
                yield return loaderChild.LoadStreamCo(file);
            }
            finished.Resolve(true);
        }

        public float Priority(float distanceToCamera)
        {
            return (float)(this.Depth - 1.0 / distanceToCamera);
        }

        public bool Commit()
        {
            if (this.childrenStatus == ChildrenStatus.Staged)
            {
                this.childrenStatus = ChildrenStatus.Commited;
                //this.Content.Initialize(this.Tileset.TilesetOptions.CreateColliders);
                return true;
            }
            return false;
        }

        public void Traverse(int frameCount, List<CamState> camStates)
        {
            if(camStates.Count == 0)
            {
                return;
            }
            this.FrameNumberOfLastTraversal = frameCount;

            // TODO: optimize run speed

            // cull by bounding sphere
            bool isInSide = false;
            float screenDiameter = 0;
            float minDistance = float.MaxValue;
            foreach (CamState camState in camStates)
            {
                PlaneClipMask mask = this.BoundingSphere.IntersectPlanes(camState.planes, PlaneClipMask.GetDefaultMask());
                if (mask.Intersection != IntersectionType.OUTSIDE)
                {
                    isInSide = true;
                    screenDiameter = Mathf.Max(screenDiameter, this.BoundingSphere.ScreenDiameter(camState.pixelSizeVector));
                    float distance = this.BoundingSphere.DistanceTo(camState.position);
                    minDistance = Mathf.Min(distance, minDistance); // We take the min in case multiple cameras, reset dist to max float on frame reset
                }
            }
            if (isInSide == false)
            {
                this.EnableRenderer(false);
                MarkStagingChildren();
                return;
            }

            // traverse based on screenDiameter
            if (screenDiameter < MaxScreenDiameter || this.ChildrenFiles.Count == 0)
            {
                this.EnableRenderer(true);
                MarkStagingChildren();
            }
            else
            {
                // commited
                if (this.childrenStatus == ChildrenStatus.Commited)
                {
                    this.EnableRenderer(false);
                    this.unity3mxComponent.LRUCache.MarkUsed(this);
                    foreach (PagedLOD pagedLOD in this.CommitedChildren)
                    {
                        pagedLOD.Traverse(Time.frameCount, camStates);
                    }
                }
                else if (this.childrenStatus == ChildrenStatus.Staged)
                {
                    this.EnableRenderer(true);
                    this.unity3mxComponent.LRUCache.MarkUsed(this);
                }
                else
                {
                    this.EnableRenderer(true);
                    if (this.childrenStatus == ChildrenStatus.Unstaged)
                    {
                        if(!RequestManager.Current.Full())
                        {
                            this.childrenStatus = ChildrenStatus.Staging;

                            Promise<bool> finished = new Promise<bool>();
                            finished.Then((success) =>
                            {
                                this.childrenStatus = PagedLOD.ChildrenStatus.Staged;
                                this.unity3mxComponent.LRUCache.Add(this);
                                this.unity3mxComponent.CommitingQueue.Enqueue(this);
                            });
                            
                            Promise started = new Promise();
                            started.Then(() =>
                            {
                                this.unity3mxComponent.StartCoroutine(this.StageChildrenCo(finished));
                            });
                            Request request = new Request(this, this.Priority(minDistance), started, finished);
                            RequestManager.Current.EnqueRequest(request);
                        }
                    }
                }
            }
        }
    }
}