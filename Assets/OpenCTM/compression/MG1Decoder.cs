using System;

namespace OpenCTM
{
	public class MG1Decoder : RawDecoder
	{
		public static readonly int MG1_TAG = CtmFileReader.getTagInt("MG1\0");

	    public override Mesh decode(MeshInfo minfo, CtmInputStream input)
	    {
	        Mesh m = base.decode(minfo, input);
	        restoreIndices(minfo.getTriangleCount(), m.indices);
	        return m;
	    }
	
	    public override bool isFormatSupported(int tag, int version)
	    {
	        return tag == MG1_TAG && version == RawDecoder.FORMAT_VERSION;
	    }
	
	    protected override float[] readFloatArray(CtmInputStream input, int count, int size)
	    {
	        return input.readPackedFloats(count, size);
	    }
	
	    protected override int[] readIntArray(CtmInputStream input, int count, int size, bool signed)
	    {
	        return input.readPackedInts(count, size, signed);
	    }
	
	    public void restoreIndices(int triangleCount, int[] indices)
	    {
	        for (int i = 0; i < triangleCount; ++i) {
	            // Step 1: Reverse derivative of the first triangle index
	            if (i >= 1) {
	                indices[i * 3] += indices[(i - 1) * 3];
	            }
	
	            // Step 2: Reverse delta from third triangle index to the first triangle
	            // index
	            indices[i * 3 + 2] += indices[i * 3];
	
	            // Step 3: Reverse delta from second triangle index to the previous
	            // second triangle index, if the previous triangle shares the same first
	            // index, otherwise reverse the delta to the first triangle index
	            if ((i >= 1) && (indices[i * 3] == indices[(i - 1) * 3])) {
	                indices[i * 3 + 1] += indices[(i - 1) * 3 + 1];
	            } else {
	                indices[i * 3 + 1] += indices[i * 3];
	            }
	        }
	    }
	}
}

