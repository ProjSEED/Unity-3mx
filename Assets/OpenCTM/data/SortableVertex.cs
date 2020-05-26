using System;

namespace OpenCTM
{
	public class SortableVertex:IComparable
	{
		/**
	     * Vertex X coordinate (used for sorting).
	     */
	    public readonly float x;
	    /**
	     * Grid index. This is the index into the 3D space subdivision grid.
	     */
	    public readonly int gridIndex;
	    /**
	     * Original index (before sorting).
	     */
	    public readonly int originalIndex;
	
	    public SortableVertex(float x, int gridIndex, int originalIndex)
	    {
	        this.x = x;
	        this.gridIndex = gridIndex;
	        this.originalIndex = originalIndex;
	    }
		
		public int CompareTo(object obj) {
	        if (obj == null) return 1;
	
	        SortableVertex other = obj as SortableVertex;
	        if (other != null) 
	            return compareTo(other);
	        else 
	           throw new ArgumentException("Object is not a SortableVertex");
	    }
	
	    public int compareTo(SortableVertex o)
	    {
	        if (gridIndex != o.gridIndex) {
	            return gridIndex - o.gridIndex;
	        } else if (x < o.x) {
	            return -1;
	        } else if (x > o.x) {
	            return 1;
	        } else {
	            return 0;
	        }
	    }
		
		public override bool Equals(object obj)
	    {				        
	        SortableVertex o = obj as SortableVertex;
			
	        if(o == null)
	            return false;
	
	        return gridIndex == o.gridIndex && x == o.x;
	    }
	
	    public override int GetHashCode()
		{	        
	        int hash = 7;
			CtmInputStream.IntFloat a = new CtmInputStream.IntFloat();
			a.FloatValue = x;
	        hash = 31 * hash + a.IntValue;
	        hash = 31 * hash + this.gridIndex;
	        return hash;
	    }
	}
}

