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

		public IEnumerator LoadStream(string inputFilePath)
		{
			if (inputFilePath == null)
			{
				throw new ArgumentNullException("inputFilePath");
			}

			yield return LoadFileStream(_rootDirectoryPath, inputFilePath);
		}

		private IEnumerator LoadFileStream(string rootPath, string fileToLoad)
		{
			string pathToLoad = Path.Combine(rootPath, fileToLoad);
			if (!File.Exists(pathToLoad))
			{
				throw new FileNotFoundException("Buffer file not found", fileToLoad);
			}

			yield return null;
			LoadedStream = File.OpenRead(pathToLoad);
		}
	}
}
