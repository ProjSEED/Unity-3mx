using System;

namespace OpenCTM
{
	public class InvalidDataException : Exception
	{
		public InvalidDataException (String message) : base(message)
		{
		}
	}
}

