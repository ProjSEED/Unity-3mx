using System.IO;
using UnityEngine;
using System;
using System.Collections;

#if WINDOWS_UWP
using System.Threading.Tasks;
#endif

namespace Unity3MXB.Loader
{
	public class FileLoader : ILoader
	{
		private string _rootDirectoryPath;
		public Stream LoadedStream { get; private set; }

		public FileLoader(string rootDirectoryPath)
		{
			_rootDirectoryPath = rootDirectoryPath;
		}

		public void LoadStream(string inputFilePath)
		{
			if (inputFilePath == null)
			{
				throw new ArgumentNullException("inputFilePath");
			}

			LoadFileStream(_rootDirectoryPath, inputFilePath);
		}

		private void LoadFileStream(string rootPath, string fileToLoad)
		{
			string pathToLoad = Path.Combine(rootPath, fileToLoad);
			if (!File.Exists(pathToLoad))
			{
				throw new FileNotFoundException("Buffer file not found", fileToLoad);
			}
			LoadedStream = File.OpenRead(pathToLoad);
		}
	}
}
