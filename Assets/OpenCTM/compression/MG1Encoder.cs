using System;

namespace OpenCTM
{
	public class MG1Encoder : RawEncoder
	{
		public override int getTag() {
	        return MG1Decoder.MG1_TAG;
	    }
	
	    protected override void writeFloatArray(float[] array, CtmOutputStream output,
	                                   int count, int size){
	        output.writePackedFloats(array, count, size);
	    }
	
	    protected override void writeIndicies(int[] indices, CtmOutputStream output){
	        int[] tmp = new int[indices.Length];
	        Array.Copy(indices, tmp, tmp.Length);
	        rearrangeTriangles(tmp);
	        makeIndexDeltas(tmp);
	        output.writePackedInts(tmp, tmp.Length / 3, 3, false);
	    }
	
	    /**
	     * Re-arrange all triangles for optimal compression.
	     */
	    public void rearrangeTriangles(int[] indices) {
	        if(indices.Length % 3 != 0)
				throw new Exception();
	        // Step 1: Make sure that the first index of each triangle is the smallest
	        // one (rotate triangle nodes if necessary)
	        for (int off = 0; off < indices.Length; off += 3) {
	            if ((indices[off + 1] < indices[off]) && (indices[off + 1] < indices[off + 2])) {
	                int tmp = indices[off];
	                indices[off] = indices[off + 1];
	                indices[off + 1] = indices[off + 2];
	                indices[off + 2] = tmp;
	            } else if ((indices[off + 2] < indices[off]) && (indices[off + 2] < indices[off + 1])) {
	                int tmp = indices[off];
	                indices[off] = indices[off + 2];
	                indices[off + 2] = indices[off + 1];
	                indices[off + 1] = tmp;
	            }
	        }
	
	        // Step 2: Sort the triangles based on the first triangle index
	        Triangle[] tris = new Triangle[indices.Length / 3];
	        for (int i = 0; i < tris.Length; i++) {
	            int off = i * 3;
	            tris[i] = new Triangle(indices, off);
	        }
	
			Array.Sort(tris);
	
	        for (int i = 0; i < tris.Length; i++) {
	            int off = i * 3;
	            tris[i].copyBack(indices, off);
	        }
	    }
	
	    /**
	     * Calculate various forms of derivatives in order to reduce data entropy.
	     */
	    public void makeIndexDeltas(int[] indices) {
	        if(indices.Length % 3 != 0)
				throw new Exception();
	
	        for (int i = indices.Length / 3 - 1; i >= 0; --i) {
	            // Step 1: Calculate delta from second triangle index to the previous
	            // second triangle index, if the previous triangle shares the same first
	            // index, otherwise calculate the delta to the first triangle index
	            if ((i >= 1) && (indices[i * 3] == indices[(i - 1) * 3])) {
	                indices[i * 3 + 1] -= indices[(i - 1) * 3 + 1];
	            } else {
	                indices[i * 3 + 1] -= indices[i * 3];
	            }
	
	            // Step 2: Calculate delta from third triangle index to the first triangle
	            // index
	            indices[i * 3 + 2] -= indices[i * 3];
	
	            // Step 3: Calculate derivative of the first triangle index
	            if (i >= 1) {
	                indices[i * 3] -= indices[(i - 1) * 3];
	            }
	        }
	    }
	}
}

