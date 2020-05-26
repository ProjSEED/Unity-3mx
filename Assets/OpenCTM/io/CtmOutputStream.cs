using System;
using System.IO;
using SevenZip;
using SevenZip.Compression.LZMA;

namespace OpenCTM
{
	public class CtmOutputStream : BinaryWriter
	{
		
    	private readonly int compressionLevel;
		
		public CtmOutputStream (Stream output) : this(5, output)
		{			
		}
		
		public CtmOutputStream (int compressionLevel, Stream output) : base(output)
		{
			this.compressionLevel = compressionLevel;
		}
		
		public void writeString(String text){
	        if (text != null) {
	            writeLittleInt(text.Length);
	            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
				Write(enc.GetBytes(text));
	        } else {
	            writeLittleInt(0);
	        }
	    }
	
	    public void writeLittleInt(int v){
			
	        Write((byte)(v & 0xFF));
			//>>> was used in the original code
	        Write((byte)((v >> 8) & 0xFF));
	        Write((byte)((v >> 16) & 0xFF));
	        Write((byte)((v >> 24) & 0xFF));
	    }
	
	    public void writeLittleIntArray(int[] v){
	        foreach (int a in v) {
	            writeLittleInt(a);
	        }
	    }
	
	    public void writeLittleFloat(float v){
	        CtmInputStream.IntFloat a = new CtmInputStream.IntFloat();
			a.FloatValue = v;
			writeLittleInt(a.IntValue);
	    }
	
	    public void writeLittleFloatArray(float[] v){
	        foreach (float a in v) {
	            writeLittleFloat(a);
	        }
	    }
	
	    public void writePackedInts(int[] data, int count, int size, bool signed){
	        if(data.Length < count * size)
				throw new Exception("The data to be written is smaller"
	                + " as stated by other parameters. Needed: " + (count * size) + " Provided: " + data.Length);
	        // Allocate memory for interleaved array
	        byte[] tmp = new byte[count * size * 4];
	
	        // Convert integers to an interleaved array
	        for (int i = 0; i < count; ++i) {
	            for (int k = 0; k < size; ++k) {
	                int val = data[i * size + k];
	                // Convert two's complement to signed magnitude?
	                if (signed) {
	                    val = val < 0 ? -1 - (val << 1) : val << 1;
	                }
	                interleavedInsert(val, tmp, i + k * count, count * size);
	            }
	        }
	
	        writeCompressedData(tmp);
	    }
	
	    public void writePackedFloats(float[] data, int count, int size){
	        if(data.Length < count * size)
				throw new Exception("The data to be written is smaller"
	                + " as stated by other parameters. Needed: " + (count * size) + " Provided: " + data.Length);
			
	        // Allocate memory for interleaved array
	        byte[] tmp = new byte[count * size * 4];
	
	        // Convert floats to an interleaved array
	        for (int x = 0; x < count; ++x) {
	            for (int y = 0; y < size; ++y) {
					CtmInputStream.IntFloat a = new CtmInputStream.IntFloat();
	                a.FloatValue = data[x * size + y];
	                interleavedInsert(a.IntValue, tmp, x + y * count, count * size);
	            }
	        }
	        writeCompressedData(tmp);
	    }
	
	    public static void interleavedInsert(int value, byte[] data, int offset, int stride) {
	        data[offset + 3 * stride] = (byte) (value & 0xff);
	        data[offset + 2 * stride] = (byte) ((value >> 8) & 0xff);
	        data[offset + stride] = (byte) ((value >> 16) & 0xff);
	        data[offset] = (byte) ((value >> 24) & 0xff);
	    }
		
		private static CoderPropID[] propIDs = 
				{
					CoderPropID.DictionarySize,
					CoderPropID.PosStateBits,
					CoderPropID.LitContextBits,
					CoderPropID.LitPosBits,
					CoderPropID.Algorithm,
					CoderPropID.NumFastBytes,
					CoderPropID.MatchFinder,
					CoderPropID.EndMarker
				};
	
	    public void writeCompressedData(byte[] data){			
			int dicSize;
			if (compressionLevel <= 5) {
	           dicSize = 1 << (compressionLevel * 2 + 14);
	        } else if (compressionLevel == 6) {
	            dicSize = 1 << 25;
	        } else {
	            dicSize = 1 << 26;
	        }
			object[] properties = 
				{
					(Int32)dicSize,
					(Int32)(2),
					(Int32)(3),
					(Int32)(0),
					(Int32)(2),
					(Int32)(compressionLevel < 7 ? 32 : 64),
					"bt4",
					true
				};
			
            Encoder encoder = new Encoder();
            encoder.SetCoderProperties(propIDs, properties);			
			
            MemoryStream inStream = new MemoryStream(data);
            MemoryStream outStream = new MemoryStream();			
            encoder.Code(inStream, outStream, -1, -1, null);
			byte[] compressedData = outStream.ToArray();
			
	        //This is the custom way of OpenCTM to write the LZMA properties
			writeLittleInt(compressedData.Length);			
            encoder.WriteCoderProperties(BaseStream);
			Write(compressedData);
	    }
	}
}

