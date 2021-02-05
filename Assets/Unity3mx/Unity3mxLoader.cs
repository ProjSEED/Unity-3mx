//#define DEBUG_TIME

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditorInternal;
using UnityEngine;

namespace Unity3mx
{
    public class Unity3mxLoader : Loader.ILoader
    {
        private Loader.ILoader loader;

        private string dir;

        public Unity3mxLoader(PagedLOD parent)
        {
            Parent = parent;
            this.dir = parent.dir;
            this.loader = Loader.AbstractWebRequestLoader.CreateDefaultRequestLoader(dir);
            _TextureCache = new Dictionary<string, Texture2D>();
            _MeshCache = new Dictionary<string, Mesh>();
            _PointCloudCache = new Dictionary<string, Mesh>();
            _meshTextureIdCache = new Dictionary<string, string>();
        }

        PagedLOD Parent;

        public Stream LoadedStream { get; private set; }

        protected Dictionary<string, Texture2D> _TextureCache { get; set; }

        protected Dictionary<string, Mesh> _MeshCache { get; set; }

        protected Dictionary<string, Mesh> _PointCloudCache { get; set; }

        protected Dictionary<string, string> _meshTextureIdCache { get; set; }

        public void Dispose()
        {
            _TextureCache = null;
            _MeshCache = null;
            _meshTextureIdCache = null;
            _PointCloudCache = null;
        }

        protected IEnumerator ConstructTexture(string id, BinaryReader br, int size)
        {
            if (_TextureCache.ContainsKey(id) == false)
            {
                byte[] data = br.ReadBytes(size);
                yield return null;
                Texture2D texture2d = new Texture2D(0, 0);
                texture2d.LoadImage(data, true);
                //texture2d.filterMode = FilterMode.Bilinear;
                //texture2d.wrapMode = TextureWrapMode.Clamp;
                yield return null;
                // After we conduct the Apply(), then we can make the texture non-readable and never create a CPU copy
                //texture2d.Apply(true, true);

                _TextureCache.Add(id, texture2d);

                texture2d = null;
                yield return null;
            }
        }

        protected IEnumerator ConstructMesh(string id, BinaryReader br, int size, Vector3 bbMin, Vector3 bbMax)
        {
            // NOTE: OpenCTM does NOT support multi-threading
            if (_MeshCache.ContainsKey(id) == false)
            {
                OpenCTM.CtmFileReader reader = new OpenCTM.CtmFileReader(br.BaseStream);
                yield return null;
                OpenCTM.Mesh mesh = reader.decode();
                yield return null;
                UnityEngine.Mesh um = new UnityEngine.Mesh();
                {
                    Vector3[] Vertices = new Vector3[mesh.getVertexCount()];
                    for (int j = 0; j < mesh.getVertexCount(); j++)
                    {
                        Vertices[j].x = mesh.vertices[(j * 3)];
                        Vertices[j].y = mesh.vertices[(j * 3) + 2];
                        Vertices[j].z = mesh.vertices[(j * 3) + 1];
                    }
                    um.vertices = Vertices;
                }
                yield return null;

                {
                    int[] Triangles = new int[mesh.indices.Length];
                    for (int j = 0; j < mesh.indices.Length / 3; j++)
                    {
                        Triangles[(j * 3)] = mesh.indices[(j * 3)];
                        Triangles[(j * 3) + 1] = mesh.indices[(j * 3) + 2];
                        Triangles[(j * 3) + 2] = mesh.indices[(j * 3) + 1];
                    }
                    um.triangles = Triangles;
                }
                yield return null;

                if (mesh.getUVCount() > 0)
                {
                    Vector2[] UVList = new Vector2[mesh.texcoordinates[0].values.Length / 2];
                    for (int j = 0; j < mesh.texcoordinates[0].values.Length / 2; j++)
                    {
                        UVList[j].x = mesh.texcoordinates[0].values[(j * 2)];
                        UVList[j].y = mesh.texcoordinates[0].values[(j * 2) + 1];
                    }
                    um.uv = UVList;
                }
                yield return null;

                if (mesh.hasNormals())
                {
                    Vector3[] Normals = new Vector3[mesh.getVertexCount()];
                    for (int j = 0; j < mesh.getVertexCount(); j++)
                    {
                        Normals[j].x = mesh.normals[(j * 3)];
                        Normals[j].y = mesh.normals[(j * 3) + 2];
                        Normals[j].z = mesh.normals[(j * 3) + 1];
                    }
                    um.normals = Normals;
                }
                else
                {
                    um.RecalculateNormals();
                }
                yield return null;

                um.bounds.SetMinMax(bbMin, bbMax);

                _MeshCache.Add(id, um);
            }
        }

        protected IEnumerator ConstructPointCloud(string id, BinaryReader br, int size, Vector3 bbMin, Vector3 bbMax)
        {
            if (_PointCloudCache.ContainsKey(id) == false)
            {
                Int32 vertNum = br.ReadInt32();
                byte[] vertData = br.ReadBytes(vertNum * 3 * 4);
                byte[] colorData = br.ReadBytes(vertNum * 4);
                yield return null;
                Vector3[] Vertices = new Vector3[vertNum];
                Color[] Colors = new Color[vertNum];
                int[] indecies = new int[vertNum];
                for (int i = 0; i < vertNum; ++i)
                {
                    Vertices[i].x = BitConverter.ToSingle(vertData, i * 3 * 4);
                    Vertices[i].z = BitConverter.ToSingle(vertData, i * 3 * 4 + 4);
                    Vertices[i].y = BitConverter.ToSingle(vertData, i * 3 * 4 + 8);
                    Colors[i].r = colorData[i * 4] / 255.0f;
                    Colors[i].g = colorData[i * 4 + 1] / 255.0f;
                    Colors[i].b = colorData[i * 4 + 2] / 255.0f;
                    Colors[i].a = colorData[i * 4 + 3] / 255.0f;
                    indecies[i] = i;
                }
                yield return null;
                Mesh um = new Mesh();
                um.vertices = Vertices;
                um.colors = Colors;
                um.bounds.SetMinMax(bbMin, bbMax);
                um.SetIndices(indecies, MeshTopology.Points, 0);

                _PointCloudCache.Add(id, um);
            }
        }

        public IEnumerator LoadStreamCo(string relativeFilePath)
        {
            if(Parent.Go == null)
            {
                yield break;
            }

            yield return this.loader.LoadStreamCo(relativeFilePath);

            if (this.loader.LoadedStream.Length == 0)
            {
                LoadedStream = new MemoryStream(0);
            }
            else
            {
                using (BinaryReader br = new BinaryReader(this.loader.LoadedStream))
                {
                    if(relativeFilePath.EndsWith(".3mx", StringComparison.OrdinalIgnoreCase))
                    {
                        string _3mxJson = new String(br.ReadChars((int)this.loader.LoadedStream.Length));
                        Schema._3mx _3mx = JsonConvert.DeserializeObject<Schema._3mx>(_3mxJson);
                        PagedLOD commitedChild = new PagedLOD(_3mx.Layers[0].Id, Parent.dir, Parent);
                        commitedChild.unity3mxComponent = Parent.unity3mxComponent;
                        commitedChild.MaxScreenDiameter = 0;
                        commitedChild.BoundingSphere = new TileBoundingSphere(new Vector3(0, 0, 0), 1e30f);
                        commitedChild.ChildrenFiles = new List<string>();
                        commitedChild.ChildrenFiles.Add(_3mx.Layers[0].Root);
                        Parent.CommitedChildren.Add(commitedChild);
                        yield return null;
                    }
                    else
                    {
#if DEBUG_TIME
                        System.Diagnostics.Stopwatch swHeader = new System.Diagnostics.Stopwatch();
                        swHeader.Start();
#endif

                        // Remove query parameters if there are any
                        string filename = relativeFilePath.Split('?')[0];
                        const int magicNumberLen = 5;
                        // magic number
                        string magicNumber = new String(br.ReadChars((int)magicNumberLen));
                        if (magicNumber != "3MXBO")
                        {
                            Debug.LogError("Unsupported magic number in 3mxb file: " + magicNumber + " " + relativeFilePath);
                        }

                        // header size
                        UInt32 headerSize = br.ReadUInt32();
                        if (headerSize == 0)
                        {
                            Debug.LogError("Unexpected zero length header in 3mxb file: " + relativeFilePath);
                        }

                        // header
                        string headerJson = new String(br.ReadChars((int)headerSize));
                        Schema.Header3mxb header3mxb = JsonConvert.DeserializeObject<Schema.Header3mxb>(headerJson);

#if DEBUG_TIME
                        swHeader.Stop();
                        System.Diagnostics.Stopwatch swTexture = new System.Diagnostics.Stopwatch();
                        System.Diagnostics.Stopwatch swGeometry = new System.Diagnostics.Stopwatch();
#endif
                        // resources
                        for (int i = 0; i < header3mxb.Resources.Count; ++i)
                        {
                            Schema.Resource resource = header3mxb.Resources[i];
                            if (resource.Type == "textureBuffer" && resource.Format == "jpg")
                            {
#if DEBUG_TIME
                                swTexture.Start();
#endif
                                yield return ConstructTexture(resource.Id, br, resource.Size);
#if DEBUG_TIME
                                swTexture.Stop();
#endif
                            }
                            else if (resource.Type == "geometryBuffer" && resource.Format == "ctm")
                            {
#if DEBUG_TIME
                                swGeometry.Start();
#endif
                                yield return ConstructMesh(resource.Id, br, resource.Size,
                                    new Vector3(resource.BBMin[0], resource.BBMin[2], resource.BBMin[1]),
                                    new Vector3(resource.BBMax[0], resource.BBMax[2], resource.BBMax[1]));

                                _meshTextureIdCache.Add(resource.Id, resource.Texture);
#if DEBUG_TIME
                                swGeometry.Stop();
#endif
                            }
                            else if (resource.Type == "geometryBuffer" && resource.Format == "xyz")
                            {
#if DEBUG_TIME
                                swGeometry.Start();
#endif
                                yield return ConstructPointCloud(resource.Id, br, resource.Size,
                                    new Vector3(resource.BBMin[0], resource.BBMin[2], resource.BBMin[1]),
                                    new Vector3(resource.BBMax[0], resource.BBMax[2], resource.BBMax[1]));
#if DEBUG_TIME
                                swGeometry.Stop();
#endif
                            }
                            else
                            {
                                Debug.LogError("Unexpected buffer type in 3mxb file: " + relativeFilePath);
                            }
                        }
#if DEBUG_TIME
                        System.Diagnostics.Stopwatch swNodes = new System.Diagnostics.Stopwatch();
                        swNodes.Start();
#endif
                        // nodes
                        for (int i = 0; i < header3mxb.Nodes.Count; ++i)
                        {
                            string url = UrlUtils.ReplaceDataProtocol(this.dir + relativeFilePath);
                            string childDir = UrlUtils.GetBaseUri(url);

                            Schema.Node node = header3mxb.Nodes[i];

                            PagedLOD commitedChild = new PagedLOD(node.Id, childDir, Parent);
                            commitedChild.unity3mxComponent = Parent.unity3mxComponent;
                            commitedChild.BBMin = new Vector3(node.BBMin[0], node.BBMin[2], node.BBMin[1]);
                            commitedChild.BBMax = new Vector3(node.BBMax[0], node.BBMax[2], node.BBMax[1]);
                            commitedChild.BoundingSphere = new TileBoundingSphere((commitedChild.BBMax + commitedChild.BBMin) / 2, (commitedChild.BBMax - commitedChild.BBMin).magnitude / 2);
                            commitedChild.MaxScreenDiameter = node.MaxScreenDiameter;
                            commitedChild.ChildrenFiles = node.Children;

                            for (int j = 0; j < node.Resources.Count; ++j)
                            {
                                GameObject goSingleMesh = new GameObject();
                                goSingleMesh.name = "Drawable";
                                //goSingleMesh.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                                goSingleMesh.transform.SetParent(commitedChild.Go.transform, false);
                                MeshRenderer mr = goSingleMesh.AddComponent<MeshRenderer>();
                                mr.enabled = false;
                                MeshFilter mf = goSingleMesh.AddComponent<MeshFilter>();

                                Mesh model;
                                if (_MeshCache.TryGetValue(node.Resources[j], out model))
                                {
                                    mf.mesh = model;
                                    string textureId;
                                    if (_meshTextureIdCache.TryGetValue(node.Resources[j], out textureId))
                                    {
                                        if (textureId != null)
                                        {
                                            Texture2D texture;
                                            _TextureCache.TryGetValue(textureId, out texture);
                                            mr.material.SetTexture("_MainTex", texture);
                                        }
                                    }
                                }
                                else if (_PointCloudCache.TryGetValue(node.Resources[j], out model))
                                {
                                    mf.mesh = model;
                                }
                            }
                            commitedChild.renderers = commitedChild.Go.GetComponentsInChildren<MeshRenderer>();
                            
                            Parent.CommitedChildren.Add(commitedChild);
                            yield return null;
                        }
                        if (!Parent.unity3mxComponent.HasBounds() && Parent.CommitedChildren.Count > 0)
                        {
                            Vector3 center = new Vector3();
                            Vector3 size = new Vector3();
                            bool hasBounds = false;
                            {
                                PagedLOD commitedChild = Parent.CommitedChildren[0];
                                center = (commitedChild.BBMax + commitedChild.BBMin) / 2;
                                size = commitedChild.BBMax - commitedChild.BBMin;
                                hasBounds = true;
                            }
                            if(hasBounds)
                            {
                                Bounds bounds = new Bounds(center, size);
                                foreach (PagedLOD commitedChild in Parent.CommitedChildren)
                                {
                                    bounds.Encapsulate(commitedChild.BBMin);
                                    bounds.Encapsulate(commitedChild.BBMax);
                                }
                                Parent.unity3mxComponent.SetBounds(bounds);
                            }
                        }
#if DEBUG_TIME
                        swNodes.Stop();
                        UnityEngine.Debug.Log(string.Format("Header: {0} ms, Texture: {1} ms, Geometry: {2} ms, Nodes: {3} m", 
                            swHeader.ElapsedMilliseconds, swTexture.ElapsedMilliseconds, swGeometry.ElapsedMilliseconds, swNodes.ElapsedMilliseconds));
#endif
                    }
                }
            }
            Dispose();
        }
    }
}