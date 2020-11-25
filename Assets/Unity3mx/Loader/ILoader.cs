using System.Collections;
using System.IO;
#if WINDOWS_UWP
using System.Threading.Tasks;
#endif

namespace Unity3mx.Loader
{
	public interface ILoader
	{
		IEnumerator LoadStreamCo(string relativeFilePath);

		Stream LoadedStream { get; }
	}
}
