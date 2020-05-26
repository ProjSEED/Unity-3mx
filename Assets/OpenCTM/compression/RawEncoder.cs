using System;

namespace OpenCTM
{
	public class RawEncoder : MeshEncoder
	{
	    public virtual void encode(Mesh m, CtmOutputStream output)
	    {
	        int vc = m.getVertexCount();
	
	        output.writeLittleInt(MeshDecoder.INDX);
	        writeIndicies(m.indices, output);
	
	        output.writeLittleInt(MeshDecoder.VERT);
	        writeFloatArray(m.vertices, output, vc * 3, 1);
	
	        // Write normals
	        if (m.normals != null) {
	            output.writeLittleInt(MeshDecoder.NORM);
	            writeFloatArray(m.normals, output, vc, 3);
	        }
	
	        foreach (AttributeData ad in m.texcoordinates) {
	            output.writeLittleInt(MeshDecoder.TEXC);
	            output.writeString(ad.name);
	            output.writeString(ad.materialName);
	            writeFloatArray(ad.values, output, vc, 2);
	        }
	
	        foreach (AttributeData ad in m.attributs) {
	            output.writeLittleInt(MeshDecoder.ATTR);
	            output.writeString(ad.name);
	            writeFloatArray(ad.values, output, vc, 4);
	        }
	    }
	
	    protected virtual void writeIndicies(int[] indices, CtmOutputStream output)
	    {
	        output.writeLittleIntArray(indices);
	    }
	
	    protected virtual void writeFloatArray(float[] array, CtmOutputStream output, int count, int size)
	    {
	        output.writeLittleFloatArray(array);
	    }
	
	    public virtual int getTag()
	    {
	        return RawDecoder.RAW_TAG;
	    }
	
	    public virtual int getFormatVersion()
	    {
	        return RawDecoder.FORMAT_VERSION;
	    }
	}
}

