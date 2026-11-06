namespace OpenNspw.App;

// What the app asks of the system it runs on beyond MonoGame, which each host gives in its own way (OpenNspw.Desktop).
internal interface IAppHost
{
	// Shows a folder of the app's, such as the replays', in the system's file browser, creating it if there is none.
	void OpenFolder(string path);
}
