using System;
using System.IO;
using System.Runtime.InteropServices;
using SevenZip.Compression.LZMA;

namespace OpenCTM
{
	public class CtmInputStream : BinaryReader
	{
        Decoder decoder ;
        public CtmInputStream (Stream input) : base(input)
		{
            decoder = new Decoder();
		}
		
		public String readString ()
		{
			int len = readLittleInt ();
			if (len > 0) {
				byte[] values = ReadBytes (len);
				if (values.Length == 0) {
					throw new IOException ("End of file reached while parsing the file!");
				}
				System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding ();
				return enc.GetString (values);
			} else {
				return "";
			}
		}

		/**
	     * reads a single Integer value, in little edian order
	     * <p/>
	     * @return < p/> @throws IOException
	     */
		public int readLittleInt ()
		{
			int ch1 = ReadByte ();
			int ch2 = ReadByte ();
			int ch3 = ReadByte ();
			int ch4 = ReadByte ();

			if ((ch1 | ch2 | ch3 | ch4) < 0) {
				throw new EOFException ();
			}
			return (ch1 + (ch2 << 8) + (ch3 << 16) + (ch4 << 24));
		}
	
		public int[] readLittleIntArray (int count)
		{
			int[] array = new int[count];
			for (int i = 0; i < count; i++) {
				array [i] = readLittleInt ();
			}
			return array;
		}
		
		[StructLayout(LayoutKind.Explicit)]
		public struct IntFloat
		{       
			[FieldOffset(0)]
			public float FloatValue;
			[FieldOffset(0)]
			public int IntValue;        
		}

        /**
	     * Reads floating point type stored in little endian (see readFloat() for
	     * big endian)
	     * <p/>
	     * @return float value translated from little endian
	     * <p/>
	     * @throws IOException if an IO error occurs
	     */
        IntFloat a = new IntFloat();
        public float readLittleFloat ()
		{
			a.IntValue = readLittleInt ();
			return a.FloatValue;
		}
	
		public float[] readLittleFloatArray (int count)
		{
			float[] array = new float[count];
			for (int i = 0; i < count; i++) {
				array [i] = readLittleFloat ();
			}
			return array;
		}
	
		public int[] readPackedInts (int count, int size, bool signed)
		{
			int[] data = new int[count * size];
			byte[] tmp = readCompressedData (count * size * 4);//a Integer is 4 bytes
			// Convert interleaved array to integers
			for (int i = 0; i < count; ++i) {
				for (int k = 0; k < size; ++k) {
					int val = interleavedRetrive (tmp, i + k * count, count * size);
					if (signed) {
						long x = ((long)val) & 0xFFFFFFFFL;//not sure if correct
						val = (x & 1) != 0 ? -(int)((x + 1) >> 1) : (int)(x >> 1);
					}
					data [i * size + k] = val;
				}
			}
			return data;
		}

       

        public float[] readPackedFloats (int count, int size)
		{
            float[] data = new float[count * size];
            byte[] tmp = readCompressedData(count * size * 4);//a Float is 4 bytes
            // Convert interleaved array to floats
            for (int i = 0; i < count; ++i)
            {
                for (int k = 0; k < size; ++k)
                {
                    IntFloat a = new IntFloat();
                    a.IntValue = interleavedRetrive(tmp, i + k * count, count * size);
                    data[i * size + k] = a.FloatValue;
                }
            }

            return data;

        }

        
        public byte[] readCompressedData (int size)
		{
			//Decoder decoder = new Decoder ();
	            
			 MemoryStream newOutStream = new MemoryStream ();
				
			int compressedSize = readLittleInt ();
			
			byte[] properties = ReadBytes (5);
			if (properties.Length == 0) {
				throw new IOException ("End of file reached while parsing the file!");
			}
				
			long accPos = BaseStream.Position;
			
			decoder.SetDecoderProperties (properties);	
			decoder.Code (BaseStream, newOutStream, compressedSize, size, null);
	
			BaseStream.Position = accPos + compressedSize;
			
			return newOutStream.ToArray ();
		}

		public static int interleavedRetrive (byte[] data, int offset, int stride)
		{
			byte b1 = data [offset + 3 * stride];
			byte b2 = data [offset + 2 * stride];
			byte b3 = data [offset + 1 * stride];
			byte b4 = data [offset];
	
			int i1 = ((int)b1) & 0xff;
			int i2 = ((int)b2) & 0xff;
			int i3 = ((int)b3) & 0xff;
			int i4 = ((int)b4) & 0xff;
	
			return i1 | (i2 << 8) | (i3 << 16) | (i4 << 24);
		}
	}
}

