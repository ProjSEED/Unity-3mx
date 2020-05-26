using System;

namespace OpenCTM
{
	public class Grid
	{
		/**
	     * Axis-aligned bounding box for the grid
	     */
	    private readonly float[] min, max;
	    /**
	     * How many divisions per axis (minimum 1).
	     */
	    private readonly int[] division;
	
	    public static Grid fromStream(CtmInputStream input){
	        return new Grid(input.readLittleFloatArray(3),
	                        input.readLittleFloatArray(3),
	                        input.readLittleIntArray(3));
	    }
	
	    public Grid(float[] min, float[] max, int[] division) {
	        this.min = min;
	        this.max = max;
	        this.division = division;
	    }
	
	    public void writeToStream(CtmOutputStream output){
	        output.writeLittleFloatArray(min);
	        output.writeLittleFloatArray(max);
	        output.writeLittleIntArray(division);
	    }
	
	    public bool checkIntegrity() {
	        if (min.Length != 3) {
	            return false;
	        }
	        if (max.Length != 3) {
	            return false;
	        }
	        if (division.Length != 3) {
	            return false;
	        }
	
	        foreach(int d in division) {
	            if (d < 1) {
	                return false;
	            }
	        }
	        for (int i = 0; i < 3; i++) {
	            if (max[i] < min[i]) {
	                return false;
	            }
	        }
	        return true;
	    }
	
	    public float[] getMin() {
	        return min;
	    }
	
	    public float[] getMax() {
	        return max;
	    }
	
	    public int[] getDivision() {
	        return division;
	    }
	
	    public float[] getSize() {
	        float[] size = new float[3];
	        for (int i = 0; i < 3; i++) {
	            size[i] = (max[i] - min[i]) / division[i];
	        }
	        return size;
	    }
	}
}

