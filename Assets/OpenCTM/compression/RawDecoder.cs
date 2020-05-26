using System;
using System.Collections.Generic;
//using UnityEngine;
using System.Diagnostics;

namespace OpenCTM
{
	public class RawDecoder : MeshDecoder
	{
		public static readonly int RAW_TAG = CtmFileReader.getTagInt("RAW\0");
	    public const int FORMAT_VERSION = 5;

        public static Stopwatch sw = new Stopwatch();
	
	    public override Mesh decode(MeshInfo minfo, CtmInputStream input)
	    {

            //sw.Start();
            int vc = minfo.getVertexCount();

            AttributeData[] tex = new AttributeData[minfo.getUvMapCount()];
            AttributeData[] att = new AttributeData[minfo.getAttrCount()];

            checkTag(input.readLittleInt(), INDX);
	        int[] indices = readIntArray(input, minfo.getTriangleCount(), 3, false);
	
	        checkTag(input.readLittleInt(), VERT);
	        float[] vertices = readFloatArray(input, vc * Mesh.CTM_POSITION_ELEMENT_COUNT, 1);
            //sw.Stop();

            float[] normals = null;
	        if (minfo.hasNormals()) {
	            checkTag(input.readLittleInt(), NORM);
	            normals = readFloatArray(input, vc, Mesh.CTM_NORMAL_ELEMENT_COUNT);
	        }

            for (int i = 0; i < tex.Length; ++i)
            {
                checkTag(input.readLittleInt(), TEXC);
                tex[i] = readUVData(vc, input);
            }

            
            for (int i = 0; i < att.Length; ++i)
            {
                checkTag(input.readLittleInt(), ATTR);
                att[i] = readAttrData(vc, input);
            }
            

            return new Mesh(vertices, normals, indices, tex, att);
	    }
	
	    protected void checkTag(int readTag, int expectedTag)
	    {
	        if (readTag != expectedTag) {
	            throw new BadFormatException("Instead of the expected data tag(\"" + CtmFileReader.unpack(expectedTag)
	                    + "\") the tag(\"" + CtmFileReader.unpack(readTag) + "\") was read!");
	        }
	    }

        List<int> intArray = new List<int>();
	    protected virtual int[] readIntArray(CtmInputStream input, int count, int size, bool signed)
	    {
            //int[] array = new int[count * size];
            //for (int i = 0; i < array.Length; i++) {
            //    array[i] = input.readLittleInt();
            //}
            //return array;

            intArray.Clear();
             int length= count * size;
            for (int i = 0; i < length; i++)
            {
                intArray.Add(input.readLittleInt());
            }

            return intArray.ToArray();
        }

        private List<float> floatArray;

        protected virtual float[] readFloatArray(CtmInputStream input, int count, int size)
	    {
         //   float[] floatArray = new float[count * size];
	        //for (int i = 0; i < floatArray.Length; i++) {
         //       floatArray[i] = input.readLittleFloat();
	        //}
	        //return floatArray;

            int length = count * size;

            for (int i = 0; i < length; i++)
            {
                floatArray.Add(input.readLittleFloat()) ;
            }
            return floatArray.ToArray();
        }



        private float[] UVdataArray;
        private string name;
        private string matname;
	    private AttributeData readUVData(int vertCount, CtmInputStream input)
	    {
	         name = input.readString();
	         matname = input.readString();

            // Array.Clear(UVdataArray, 0, UVdataArray.Length);
            UVdataArray = readFloatArray(input, vertCount, Mesh.CTM_UV_ELEMENT_COUNT);
	
	        return new AttributeData(name, matname, AttributeData.STANDARD_UV_PRECISION, UVdataArray);
	    }
	
	    private AttributeData readAttrData(int vertCount, CtmInputStream input)
	    {
	        String name = input.readString();
	        float[] data = readFloatArray(input, vertCount, Mesh.CTM_ATTR_ELEMENT_COUNT);
	
	        return new AttributeData(name, null, AttributeData.STANDARD_PRECISION, data);
	    }
	
	    public override bool isFormatSupported(int tag, int version)
	    {
	        return tag == RAW_TAG && version == FORMAT_VERSION;
	    }
	}
}

