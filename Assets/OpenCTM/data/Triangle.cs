using System;

namespace OpenCTM
{
	public class Triangle:IComparable
	{
		int[] elements = new int[3];

	    public Triangle(int[] source, int offset)
	    {
			Array.Copy(source, offset, elements, 0, 3);
	    }
	
	    public void copyBack(int[] dest, int offset)
	    {
			Array.Copy(elements, 0, dest, offset, 3);
	    }
		
		public int CompareTo(object obj) {
	        if (obj == null) return 1;
	
	        Triangle otherTriangle = obj as Triangle;
	        if (otherTriangle != null) 
	            return compareTo(otherTriangle);
	        else 
	           throw new ArgumentException("Object is not a Triangle");
	    }	
	    
	    public int compareTo(Triangle o)
	    {
	        if (elements[0] != o.elements[0]) {
	            return elements[0] - o.elements[0];
	        } else if (elements[1] != o.elements[1]) {
	            return elements[1] - o.elements[1];
	        }
	        return elements[2] - o.elements[2];
	    }
		
		public override bool Equals(object obj)
	    {			
	        Triangle other = obj as Triangle;
			
	        if (other == null) {
	            return false;
	        }
			
	        return Mesh.ArrayEquals(this.elements, other.elements);
	    }
	
	    public override int GetHashCode()
		{
	        return GetArrayHash(elements);
	    }
		
		public static int GetArrayHash(int[] array)
		{
			int hc=array.Length;
			for(int i=0;i<array.Length;++i)
			{
			     hc=unchecked(hc*31 +array[i]);
			}
			return hc;
		}
	}
}

