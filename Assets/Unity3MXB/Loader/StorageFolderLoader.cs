#if WINDOWS_UWP
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using System;
using System.Collections;

namespace Unity3MXB.Loader
{
	public class StorageFolderLoader : ILoader
	{
		private StorageFolder _rootFolder;
		public Stream LoadedStream { get; private set; }

		public StorageFolderLoader(StorageFolder rootFolder)
		{
			_rootFolder = rootFolder;
		}

		public IEnumerator LoadStreamCo(string gltfFilePath)
		{
			if (gltfFilePath == null)
			{
				throw new ArgumentNullException("gltfFilePath");
			}
			
			yield return LoadStorageFileCo(gltfFilePath).AsCoroutine();
		}

		public async Task LoadStorageFileCo(string path)
		{
			StorageFolder parentFolder = _rootFolder;
			string fileName = Path.GetFileName(path);
			if (path != fileName)
			{
				string folderToLoad = path.Substring(0, path.Length - fileName.Length);
				parentFolder = await _rootFolder.GetFolderAsync(folderToLoad);
			}

			StorageFile bufferFile = await parentFolder.GetFileAsync(fileName);
			LoadedStream = await bufferFile.OpenStreamForReadAsync();
		}
	}
}
#endif
