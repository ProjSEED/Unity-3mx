//#define DEBUG_TIME

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using KeyJ;

namespace Unity3MXB
{
    public class Unity3MXBLoader : Loader.ILoader
    {
        private Loader.ILoader loader;

        private string dir;

        public Unity3MXBLoader(PagedLOD parent)
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

        protected void ConstructTexture(string id, BinaryReader br, int size)
        {
            if (_TextureCache.ContainsKey(id) == false)
            {
                byte[] data = br.ReadBytes(size);
                NanoJPEG nanoJPEG = new NanoJPEG();
                nanoJPEG.njDecode(data);
                byte[] rawPixels = nanoJPEG.njGetImage();

                if(rawPixels != null && rawPixels.Length > 0 && rawPixels.Length == nanoJPEG.njGetWidth() * nanoJPEG.njGetHeight() * 3)
                {
                    Texture2D texture2d = new Texture2D(nanoJPEG.njGetWidth(), nanoJPEG.njGetHeight(), TextureFormat.RGB24, false);
                    texture2d.LoadRawTextureData(rawPixels);
                    texture2d.filterMode = FilterMode.Bilinear;
                    texture2d.wrapMode = TextureWrapMode.Clamp;
                    // After we conduct the Apply(), then we can make the texture non-readable and never create a CPU copy
                    texture2d.Apply(true, true);

                    _TextureCache.Add(id, texture2d);
                }
            }
        }

        protected void ConstructMesh(string id, BinaryReader br, int size, Vector3 bbMin, Vector3 bbMax)
        {
            // NOTE: OpenCTM does NOT support multi-threading
            if (_MeshCache.ContainsKey(id) == false)
            {
                OpenCTM.CtmFileReader reader = new OpenCTM.CtmFileReader(br.BaseStream);
                OpenCTM.Mesh mesh = reader.decode();
                UnityEngine.Mesh um = new UnityEngine.Mesh();
                mesh.checkIntegrity();
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

                if (mesh.getUVCount() > 0)
                {
                    Vector2[] UVList = new Vector2[mesh.texcoordinates[0].values.Length / 2];
                    for (int j = 0; j < mesh.texcoordinates[0].values.Length / 2; j++)
                    {
                        UVList[j].x = mesh.texcoordinates[0].values[(j * 2)];
                        UVList[j].y = 1 - mesh.texcoordinates[0].values[(j * 2) + 1];
                    }
                    um.uv = UVList;
                }

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

                um.bounds.SetMinMax(bbMin, bbMax);

                _MeshCache.Add(id, um);
            }
        }

        protected void ConstructPointCloud(string id, BinaryReader br, int size, Vector3 bbMin, Vector3 bbMax)
        {
            if (_PointCloudCache.ContainsKey(id) == false)
            {
                Int32 vertNum = br.ReadInt32();
                byte[] vertData = br.ReadBytes(vertNum * 3 * 4);
                byte[] colorData = br.ReadBytes(vertNum * 4);
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
            yield return this.loader.LoadStreamCo(relativeFilePath);

            if (this.loader.LoadedStream.Length == 0)
            {
                LoadedStream = new MemoryStream(0);
            }
            else
            {
                // We need to read the header info off of the .3mxb file
                // Using statment will ensure this.loader.LoadedStream is disposed
                using (BinaryReader br = new BinaryReader(this.loader.LoadedStream))
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
                    Schema.Header3MXB header3MXB = JsonConvert.DeserializeObject<Schema.Header3MXB>(headerJson);

#if DEBUG_TIME
                    swHeader.Stop();
                    System.Diagnostics.Stopwatch swTexture = new System.Diagnostics.Stopwatch();
                    System.Diagnostics.Stopwatch swGeometry = new System.Diagnostics.Stopwatch();
#endif
                    // resources
                    for (int i = 0; i < header3MXB.Resources.Count; ++i)
                    {
                        Schema.Resource resource = header3MXB.Resources[i];
                        if (resource.Type == "textureBuffer" && resource.Format == "jpg")
                        {
#if DEBUG_TIME
                            swTexture.Start();
#endif
                            ConstructTexture(resource.Id, br, resource.Size);
#if DEBUG_TIME
                            swTexture.Stop();
#endif
                        }
                        else if (resource.Type == "geometryBuffer" && resource.Format == "ctm")
                        {
#if DEBUG_TIME
                            swGeometry.Start();
#endif
                            ConstructMesh(resource.Id, br, resource.Size,
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
                            ConstructPointCloud(resource.Id, br, resource.Size,
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
                    for (int i = 0; i < header3MXB.Nodes.Count; ++i)
                    {
                        string url = UrlUtils.ReplaceDataProtocol(this.dir + relativeFilePath);
                        string childDir = UrlUtils.GetBaseUri(url);

                        Schema.Node node = header3MXB.Nodes[i];

                        PagedLOD commitedChild = new PagedLOD(node.Id, childDir, Parent);
                        commitedChild.unity3MXBComponent = Parent.unity3MXBComponent;
                        commitedChild.BBMin = new Vector3(node.BBMin[0], node.BBMin[2], node.BBMin[1]);
                        commitedChild.BBMax = new Vector3(node.BBMax[0], node.BBMax[2], node.BBMax[1]);
                        commitedChild.BoundingSphere = new TileBoundingSphere((commitedChild.BBMax + commitedChild.BBMin) / 2, (commitedChild.BBMax - commitedChild.BBMin).magnitude / 2);
                        commitedChild.MaxScreenDiameter = node.MaxScreenDiameter;
                        commitedChild.ChildrenFiles = node.Children;

                        for (int j = 0; j < node.Resources.Count; ++j)
                        {
                            Mesh mesh;
                            Mesh pointCloud;
                            if (_MeshCache.TryGetValue(node.Resources[j], out mesh))
                            {
                                Texture2D texture = null;
                                string textureId;
                                if (_meshTextureIdCache.TryGetValue(node.Resources[j], out textureId))
                                {
                                    if (textureId != null)
                                    {
                                        _TextureCache.TryGetValue(textureId, out texture);
                                    }
                                }
                                commitedChild.AddTextureMesh(mesh, texture);
                            }
                            else if (_PointCloudCache.TryGetValue(node.Resources[j], out pointCloud))
                            {
                                commitedChild.AddPointCloud(pointCloud);
                            }
                        }

                        Parent.CommitedChildren.Add(commitedChild);
                    }
#if DEBUG_TIME
                    swNodes.Stop();
                    UnityEngine.Debug.Log(string.Format("Header: {0} ms, Texture: {1} ms, Geometry: {2} ms, Nodes: {3} m", 
                        swHeader.ElapsedMilliseconds, swTexture.ElapsedMilliseconds, swGeometry.ElapsedMilliseconds, swNodes.ElapsedMilliseconds));
#endif
                }
            }
            Dispose();
        }
    }
}