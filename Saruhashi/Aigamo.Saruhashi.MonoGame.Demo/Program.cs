namespace Aigamo.Saruhashi.MonoGame.Demo;

public static class Program
{
	[STAThread]
	static void Main()
	{
		using var game = new MainGame();
		game.Run();
	}
}
