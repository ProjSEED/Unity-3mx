using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

namespace OpenCTM
{
    public class CtmFileReader
    {
        public static readonly int OCTM = getTagInt("OCTM");

        //本来把最大的限制在60000.但是这个算法有点问题，所以将最大限制在50000
        //private int maxNumVerticesPerMesh = 60000;
        private int maxNumVerticesPerMesh = 50000;
        

        private static readonly MeshDecoder[] DECODER = new MeshDecoder[]{
            new RawDecoder(),
            new MG1Decoder(),
            new MG2Decoder()
        };

        private String comment;
        private readonly CtmInputStream input;
        //private readonly CtmInputStream input=new CtmInputStream();
        private bool decoded;

        //public static Stopwatch sw = new Stopwatch();

        public CtmFileReader(Stream source)
        {
            input = new CtmInputStream(source);
        }

        public void decode(ref List<Mesh> meshList)
        {

            if (decoded)
            {
                throw new Exception("Ctm File got already decoded");
            }
            decoded = true;

            if (input.readLittleInt() != OCTM)
            {
                throw new BadFormatException("The CTM file doesn't start with the OCTM tag!");
            }


            int formatVersion = input.readLittleInt();
            int methodTag = input.readLittleInt();

            MeshInfo mi = new MeshInfo(input.readLittleInt(),//vertex count
                    input.readLittleInt(), //triangle count
                    input.readLittleInt(), //uvmap count
                    input.readLittleInt(), //attribute count
                    input.readLittleInt());                  //flags

            comment = input.readString();


            // Uncompress from stream
            Mesh ctmMesh = null;
            foreach (MeshDecoder md in DECODER)
            {
                if (md.isFormatSupported(methodTag, formatVersion))
                {              
                    ctmMesh = md.decode(mi, input);               
                    break;
                }
            }

            if (ctmMesh == null)
            {
                throw new IOException("No sutible decoder found for Mesh of compression type: " + unpack(methodTag) + ", version " + formatVersion);
            }

            // Check mesh integrity
            ctmMesh.checkIntegrity();

            // Unity only support maximum 65534 vertices per mesh. So large meshes need to be splitted.
            if (ctmMesh.vertices.Length > maxNumVerticesPerMesh*3)
            {
                SplitMesh(ctmMesh, ref meshList);
            }
            else
            {
                meshList.Add(ctmMesh);
            }

            //return m;
        }

        public Mesh decode()
        {

            if (decoded)
            {
                throw new Exception("Ctm File got already decoded");
            }
            decoded = true;

            if (input.readLittleInt() != OCTM)
            {
                throw new BadFormatException("The CTM file doesn't start with the OCTM tag!");
            }


            int formatVersion = input.readLittleInt();
            int methodTag = input.readLittleInt();

            MeshInfo mi = new MeshInfo(input.readLittleInt(),//vertex count
                    input.readLittleInt(), //triangle count
                    input.readLittleInt(), //uvmap count
                    input.readLittleInt(), //attribute count
                    input.readLittleInt());                  //flags

            comment = input.readString();


            // Uncompress from stream
            Mesh ctmMesh = null;
            foreach (MeshDecoder md in DECODER)
            {
                if (md.isFormatSupported(methodTag, formatVersion))
                {
                    ctmMesh = md.decode(mi, input);
                    break;
                }
            }

            if (ctmMesh == null)
            {
                throw new IOException("No sutible decoder found for Mesh of compression type: " + unpack(methodTag) + ", version " + formatVersion);
            }

            // Check mesh integrity
            ctmMesh.checkIntegrity();

            return ctmMesh;
        }

        /// <summary>
        /// split openCTM Mesh
        /// </summary>
        /// <param name="ctmMesh"></param>
        /// <param name="splitMeshes"></param>
        public void SplitMesh(Mesh ctmMesh, ref List<Mesh> splitMeshes)
        {
            //是否包含UV坐标
            bool isContainUVs;

            //顶点个数
            int ctmMeshVerticesNum = ctmMesh.vertices.Length;

            int maxNumVerticesFloatArray = maxNumVerticesPerMesh * 3;

            //判断需要分割成多少个Mesh
            int splitCTMMeshNum = Mathf.CeilToInt(ctmMeshVerticesNum / (maxNumVerticesFloatArray * 1.0f));

            int[] oldIndicesArray = ctmMesh.indices;
            float[] oldVerticesArray = ctmMesh.vertices;
            float[] oldUVArray;

            if (ctmMesh.texcoordinates==null|| ctmMesh.texcoordinates.Length==0)
            {
                isContainUVs = false;
                oldUVArray = new float[0];
            }
            else
            {
               
                isContainUVs = true;
                oldUVArray = ctmMesh.texcoordinates[0].values;
            }

            

            //下一个Mesh的Indices开始时的索引
            int lastMeshEndIndicesCount = 0;

            for (int i = 0; i < splitCTMMeshNum; i++)
            {
                OpenCTM.Mesh splitCTMMesh;

                //判断是不是最后一个分割的Mesh
                if (i != splitCTMMeshNum - 1)
                {

                    //oldIndicesArray.
                    int newIndicesCount = Array.IndexOf(oldIndicesArray, maxNumVerticesPerMesh * (i + 1));

                    //索引数必须为3的倍数
                    newIndicesCount -= newIndicesCount % 3;

                    int newIndicesArrayLength = newIndicesCount - lastMeshEndIndicesCount;

                    int[] newIndicesArray = new int[newIndicesArrayLength];
                    int[] splitMeshIndicesArray = new int[newIndicesArrayLength];

                    Array.Copy(oldIndicesArray, lastMeshEndIndicesCount, newIndicesArray, 0, newIndicesArrayLength);
                    Array.Copy(oldIndicesArray, lastMeshEndIndicesCount, splitMeshIndicesArray, 0, newIndicesArrayLength);

                    Array.Sort(newIndicesArray);

                    //找到Indices中最小的索引，将其设置为Vertices中第一个参数
                    int startVerticeCount = newIndicesArray[0];
                    //找到Indices中最大的索引，将其设置为Vertices中最后一个参数
                    int endVerticeCount = newIndicesArray[newIndicesArray.Length - 1];
                    int newVerticesArrayLength = (endVerticeCount - startVerticeCount + 1) * 3;

                    float[] splitMeshVerticesArray = new float[newVerticesArrayLength];
                    Array.Copy(oldVerticesArray, startVerticeCount * 3, splitMeshVerticesArray, 0, newVerticesArrayLength);

                    AttributeData[] texcoordinates;
                    if (isContainUVs)
                    {
                        int newUVArrayLength = (endVerticeCount - startVerticeCount + 1) * 2;
                        float[] splitMeshUVArray = new float[newUVArrayLength];
                        Array.Copy(oldUVArray, startVerticeCount * 2, splitMeshUVArray, 0, newUVArrayLength);

                        texcoordinates = new AttributeData[1];
                        AttributeData oldTexcoordinate = ctmMesh.texcoordinates[0];
                        texcoordinates[0] = new AttributeData(oldTexcoordinate.name, oldTexcoordinate.materialName, oldTexcoordinate.precision, splitMeshUVArray);
                    }
                    else
                    {
                        texcoordinates = new AttributeData[0];
                    }
                   

                    //将索引数组跟顶点数组对应
                    for (int k = 0; k < splitMeshIndicesArray.Length; k++)
                    {
                        splitMeshIndicesArray[k] -= startVerticeCount;
                    }


                    splitCTMMesh = new OpenCTM.Mesh(splitMeshVerticesArray, null, splitMeshIndicesArray, texcoordinates, null);
                    splitMeshes.Add(splitCTMMesh);
                    lastMeshEndIndicesCount += newIndicesArrayLength;


                }
                else
                {

                    int newIndicesArrayLength = oldIndicesArray.Length - lastMeshEndIndicesCount;

                    int[] newIndicesArray = new int[newIndicesArrayLength];
                    int[] splitMeshIndicesArray = new int[newIndicesArrayLength];

                    Array.Copy(oldIndicesArray, lastMeshEndIndicesCount, newIndicesArray, 0, newIndicesArrayLength);
                    Array.Copy(oldIndicesArray, lastMeshEndIndicesCount, splitMeshIndicesArray, 0, newIndicesArrayLength);

                    Array.Sort(newIndicesArray);

                    //找到Indices中最小的索引，将其设置为Vertices中第一个参数
                    int startVerticeCount = newIndicesArray[0];
                    //找到Indices中最大的索引，将其设置为Vertices中最后一个参数
                    int endVerticeCount = newIndicesArray[newIndicesArray.Length - 1];

                    int newVerticesArrayLength = (endVerticeCount - startVerticeCount + 1) * 3;                   
                    float[] splitMeshVerticesArray = new float[newVerticesArrayLength];
                    Array.Copy(oldVerticesArray, startVerticeCount * 3, splitMeshVerticesArray, 0, newVerticesArrayLength);

                    AttributeData[] texcoordinates;
                    //判断是否包含UV坐标
                    if (isContainUVs)
                    {
                        int newUVArrayLength = (endVerticeCount - startVerticeCount + 1) * 2;
                        float[] splitMeshUVArray = new float[newUVArrayLength];
                        Array.Copy(oldUVArray, startVerticeCount * 2, splitMeshUVArray, 0, newUVArrayLength);

                        texcoordinates = new AttributeData[1];
                        AttributeData oldTexcoordinate = ctmMesh.texcoordinates[0];
                        texcoordinates[0] = new AttributeData(oldTexcoordinate.name, oldTexcoordinate.materialName, oldTexcoordinate.precision, splitMeshUVArray);
                    }
                    else
                    {
                        texcoordinates = new AttributeData[0];
                    }
                    

                    //将索引数组跟顶点数组对应
                    for (int k = 0; k < splitMeshIndicesArray.Length; k++)
                    {
                        splitMeshIndicesArray[k] -= startVerticeCount;
                    }
                    

                    splitCTMMesh = new OpenCTM.Mesh(splitMeshVerticesArray, null, splitMeshIndicesArray, texcoordinates, null);
                    splitMeshes.Add(splitCTMMesh);
                }
            }
        }



        public static String unpack(int tag)
        {
            byte[] chars = new byte[4];
            chars[0] = (byte)(tag & 0xff);
            chars[1] = (byte)((tag >> 8) & 0xff);
            chars[2] = (byte)((tag >> 16) & 0xff);
            chars[3] = (byte)((tag >> 24) & 0xff);
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            return enc.GetString(chars);
        }

        /**
	     * before calling this method the first time, the decode method has to be
	     * called.
	     * <p/>
	     * @throws RuntimeExceptio- if the file wasn't decoded before.
	     */
        public String getFileComment()
        {
            if (!decoded)
            {
                throw new Exception("The CTM file is not decoded yet.");
            }
            return comment;
        }

        public static int getTagInt(String tag)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            byte[] chars = enc.GetBytes(tag);
            if (chars.Length != 4)
                throw new Exception("A tag has to be constructed out of 4 characters!");
            return chars[0] | (chars[1] << 8) | (chars[2] << 16) | (chars[3] << 24);
        }
    }
}

