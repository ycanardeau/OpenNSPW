namespace OpenNspw;

// Resumes what the game awaits on the game's thread, as a message of its loops (PostContinuation), so that the code after
// an await runs where the original's call returned, and nothing waits for a task. Not part of the original.
public sealed class GameSynchronizationContext(Nspw game) : SynchronizationContext
{
	private readonly Nspw _game = game;

	public override void Post(SendOrPostCallback d, object? state)
	{
		_game.PostContinuation(() => d(state));
	}

	// Running it on the game's thread would make the caller wait for it.
	public override void Send(SendOrPostCallback d, object? state)
	{
		throw new NotSupportedException("The game's thread is not waited for; post to it instead.");
	}

	// The same context, not a copy: a task that completes on the game's thread runs the game's continuation at once, with
	// nothing of the game's loop between.
	public override SynchronizationContext CreateCopy()
	{
		return this;
	}
}

public partial class Nspw
{
	// The closes of the peers that the game has let go of (ClosePeer), which the game's thread sees through before it ends.
	private readonly List<Task<int>> _closings = [];

	private GameSynchronizationContext? _synchronizationContext;

	// The context of the game's thread, which RunWinMain sets on it.
	public GameSynchronizationContext SynchronizationContext =>
		_synchronizationContext ??= new(this);

	// Closes a peer that the game lets go of, as its Close does. The game's thread ends once it has closed (RunUntilClosed).
	public void ClosePeer(IDirectPlay8Peer peer)
	{
		_closings.Add(peer.CloseAsync(0));
	}

	// Runs the game's messages until the peers it let go of have closed, so that its thread ends after their goodbyes, as
	// the original's Close returned after them. The closes resume as messages; the others are dropped, as WinMain has
	// returned and nothing reads them.
	public void RunUntilClosed()
	{
		var closed = Task.WhenAll(_closings);
		while (!closed.IsCompleted)
		{
			if (Mailbox.TryReceive<GameMessage.Window>(out var window))
			{
				if (window.Message.isContinuation)
				{
					window.Message.userAction!();
				}
			}
			else
			{
				_platform.WaitForMessage(Mailbox);
			}
		}

		_closings.Clear();

		// A close that failed ends the game's thread with its exception, as the call would have.
		if (closed.Exception?.InnerException is { } exception)
		{
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(exception);
		}
	}

	// Runs `action` with the calling thread as the game's: what the game awaits in it resumes in the game's message loops.
	// For a test that drives the game from its own thread.
	public void RunAsGameThread(Action action)
	{
		var previous = System.Threading.SynchronizationContext.Current;
		System.Threading.SynchronizationContext.SetSynchronizationContext(SynchronizationContext);
		try
		{
			action();
		}
		finally
		{
			System.Threading.SynchronizationContext.SetSynchronizationContext(previous);
		}
	}

	// Runs WinMain with the calling thread as the game's, then the closes of the peers it let go of.
	public int RunWinMain(object? hInst, object? hPrevInst, string lpCmdLine, int nCmdShow)
	{
		var result = 0;
		RunAsGameThread(() =>
		{
			result = WinMain(hInst, hPrevInst, lpCmdLine, nCmdShow);
			RunUntilClosed();
		});
		return result;
	}
}
