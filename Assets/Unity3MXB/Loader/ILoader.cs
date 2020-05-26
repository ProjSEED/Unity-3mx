using System.Collections;
using System.IO;
#if WINDOWS_UWP
using System.Threading.Tasks;
#endif

namespace Unity3MXB.Loader
{
	public interface ILoader
	{
		IEnumerator LoadStream(string relativeFilePath);

		Stream LoadedStream { get; }
	}
}
