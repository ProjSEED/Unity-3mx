// LzOutWindow.cs

using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

namespace SevenZip.Compression.LZ
{
    public class OutWindow
    {

        //µ¥Àý ÓÅ»¯
        private static OutWindow instance;

        public static OutWindow GetInstance()
        {
            if (instance == null)
                instance = new OutWindow();

            return instance;
        }

        //private Stopwatch sw = new Stopwatch();

        byte[] _buffer = new byte[16777216];
        //List<byte> _bufferList = new List<byte>();
        uint _pos;
        uint _windowSize = 0;
        uint _streamPos;
        System.IO.Stream _stream;

        public void Create(uint windowSize)
        {
            if (_windowSize != windowSize)
            {
                //sw.Reset();
                //sw.Start();
                //System.GC.Collect(_buffer);
                //if (_buffer == null)
                //{
                //    //_buffer = new byte[windowSize];
                //    //UnityEngine.Debug.Log("buffer");
                //}

                //sw.Stop();

                //UnityEngine.Debug.Log("windowSize:" + _buffer.Length);
                //UnityEngine.Debug.Log("Time:"+ sw.ElapsedMilliseconds);
                //_bufferList.Clear();
                //_bufferList.Capacity = System.Convert.ToInt32(windowSize) ;
                // _bufferList
            }
            _windowSize = windowSize;
            _pos = 0;
            _streamPos = 0;
        }

        public void Init(System.IO.Stream stream, bool solid)
        {
            ReleaseStream();
            _stream = stream;
            if (!solid)
            {
                _streamPos = 0;
                _pos = 0;
            }
        }

        public void Init(System.IO.Stream stream) { Init(stream, false); }

        public void ReleaseStream()
        {
            Flush();
            _stream = null;
        }

        public void Flush()
        {
            uint size = _pos - _streamPos;
            if (size == 0)
                return;
            _stream.Write(_buffer, (int)_streamPos, (int)size);
            //_stream.Write(_bufferList.ToArray(), (int)_streamPos, (int)size);
            if (_pos >= _windowSize)
                _pos = 0;
            _streamPos = _pos;
        }

        public void CopyBlock(uint distance, uint len)
        {
            uint pos = _pos - distance - 1;
            if (pos >= _windowSize)
                pos += _windowSize;
            for (; len > 0; len--)
            {
                if (pos >= _windowSize)
                    pos = 0;
                _buffer[_pos++] = _buffer[pos++];
                //_bufferList[System.Convert.ToInt32(_pos++)] = _bufferList[System.Convert.ToInt32(pos++)];
                if (_pos >= _windowSize)
                    Flush();
            }
        }

        public void PutByte(byte b)
        {
            //Debug.Log("Capacity:" + _bufferList.Capacity+"-Count:"+_bufferList.Count+"-Index:"+System.Convert.ToInt32(_pos++));
            _buffer[_pos++] = b;
            //_bufferList[System.Convert.ToInt32(_pos++)] = b;
            //_bufferList.Insert(System.Convert.ToInt32(_pos++), b);
            if (_pos >= _windowSize)
                Flush();
        }

        public byte GetByte(uint distance)
        {
            uint pos = _pos - distance - 1;
            if (pos >= _windowSize)
                pos += _windowSize;
            return _buffer[pos];
        }
    }
}
