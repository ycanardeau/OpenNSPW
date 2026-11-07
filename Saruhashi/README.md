# Saruhashi

**A WinForms-style UI toolkit for [MonoGame](https://github.com/MonoGame/MonoGame).** Buttons, check boxes, radio buttons, labels and forms, with focus, mouse capture, keyboard input and data binding, drawn with your game's own `SpriteBatch`.

[![NuGet](https://img.shields.io/nuget/dt/Aigamo.Saruhashi.MonoGame?label=NuGet&logo=nuget)](https://www.nuget.org/packages/Aigamo.Saruhashi.MonoGame)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![MonoGame 3.8](https://img.shields.io/badge/MonoGame-3.8-E73C00)](https://monogame.net/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

Saruhashi draws every menu, dialog and the battle screen of [OpenNSPW](../OpenNSPW).

```csharp
var start = new Button { Bounds = new Rectangle(16, 152, 268, 32), Text = "Start" };
start.Click += (_, _) => StartGame();
ui.Root.Controls.Add(start);
```

## Why Saruhashi

- **You already know it.** `Control`, `Form`, `Button`, `CheckBox`, `RadioButton`, `Label`; `Click`, `MouseEnter`, `KeyPress`, `CheckedChanged`; `Controls.Add`, `BringToFront`, `Focus`, `DataBindings`. Much of it is adapted from [dotnet/winforms](https://github.com/dotnet/winforms), so it behaves the way WinForms does.
- **A real control tree.** Controls nest, clip to their parents, take focus and capture the mouse. Input goes to the right control, and every control repaints every frame.
- **Not tied to a renderer.** `Aigamo.Saruhashi` knows nothing about MonoGame: it draws through `IGraphicsFactory` and `Graphics`. `Aigamo.Saruhashi.MonoGame` is one backend, and you can write another.
- **Runs wherever MonoGame runs.** The MonoGame package isn't built for one platform: it uses the MonoGame your game references, such as `MonoGame.Framework.DesktopGL` or `MonoGame.Framework.Android`. A touch screen can stand in for the mouse.
- **Any resolution.** Give it a [MonoGame.Extended](https://github.com/craftworkgames/MonoGame.Extended) `ViewportAdapter`, and pointer positions and clipping scale with the window. A control can also be drawn larger than its parent (`Control.Scale`), with the pointer taken where it is drawn.
- **Your look.** Override `OnPaint`, or swap a control's `IButtonRenderer` or `ITextRenderer`.

## Packages

| Package | What it is |
| --- | --- |
| [`Aigamo.Saruhashi`](https://www.nuget.org/packages/Aigamo.Saruhashi) | The controls, the window manager and data binding, with no renderer. |
| [`Aigamo.Saruhashi.MonoGame`](https://www.nuget.org/packages/Aigamo.Saruhashi.MonoGame) | Drawing with a `SpriteBatch` and input from MonoGame and MonoGame.Extended. |

## Installation

```xml
<PackageReference Include="Aigamo.Saruhashi.MonoGame" Version="0.0.1-alpha.*" />
```

The packages built from this repository are versioned `0.0.1-alpha.*`. NuGet also lists older `1.x` versions, which it picks by default, so ask for `0.0.1-alpha.*`.

## Quick start

Create a `WindowManager` that covers the screen, send it MonoGame's input, and draw it after everything else:

```csharp
using Aigamo.Saruhashi;
using Aigamo.Saruhashi.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Input.InputListeners;
using DrawingRectangle = System.Drawing.Rectangle;
using SaruhashiKeys = Aigamo.Saruhashi.Keys;
using SaruhashiMouseListener = Aigamo.Saruhashi.MonoGame.MouseListener;

public class MyGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private WindowManager _ui = null!;

    public MyGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        var font = new SpriteFontWrapper(Content.Load<SpriteFont>("Fonts/Default"));
        _ui = new WindowManager(new DrawingRectangle(0, 0, 1024, 768), new MonoGameGraphicsFactory(_spriteBatch), font);

        // Send MonoGame's input to the UI.
        var mouse = new SaruhashiMouseListener();
        var keyboard = new KeyboardListener();
        Components.Add(new InputListenerComponent(this, mouse, keyboard));
        mouse.MouseDown += (_, e) => _ui.OnMouseDown(e);
        mouse.MouseMove += (_, e) => _ui.OnMouseMove(e);
        mouse.MouseUp += (_, e) => _ui.OnMouseUp(e);
        mouse.MouseWheel += (_, e) => _ui.OnMouseWheel(e);
        keyboard.KeyPressed += (_, e) => _ui.OnKeyDown(new KeyEventArgs((SaruhashiKeys)e.Key));
        keyboard.KeyReleased += (_, e) => _ui.OnKeyUp(new KeyEventArgs((SaruhashiKeys)e.Key));
        Window.TextInput += (_, e) => _ui.OnKeyPress(new KeyPressEventArgs(e.Character));

        var quit = new Button { Bounds = new DrawingRectangle(462, 372, 100, 24), Text = "Quit" };
        quit.Click += (_, _) => Exit();
        _ui.Root.Controls.Add(quit);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        base.Draw(gameTime);
        _ui.Draw();
    }
}
```

To scale the UI with the window, pass the same `ViewportAdapter` to `SaruhashiMouseListener` and `MonoGameGraphicsFactory`. To use something else as the pointer, such as a touch screen's touches, give `SaruhashiMouseListener` a function that returns a `MouseState`.

## Building screens

```csharp
var options = new Form { Bounds = new DrawingRectangle(362, 234, 300, 200) };

var music = new CheckBox { Bounds = new DrawingRectangle(16, 16, 268, 24), Text = "Music", Checked = true };
var easy = new RadioButton { Bounds = new DrawingRectangle(16, 56, 130, 24), Text = "Easy", Checked = true };
var hard = new RadioButton { Bounds = new DrawingRectangle(154, 56, 130, 24), Text = "Hard" };
var start = new Button { Bounds = new DrawingRectangle(16, 152, 268, 32), Text = "Start" };

music.CheckedChanged += (_, _) => audio.Enabled = music.Checked;
start.Click += (_, _) => StartGame(hard: hard.Checked);

options.Controls.Add(music);
options.Controls.Add(easy);
options.Controls.Add(hard);
options.Controls.Add(start);
ui.Root.Controls.Add(options);
options.Show();
```

Radio buttons in the same parent form a group: checking one unchecks the others. A `Form` starts hidden until you call `Show()`, and `Close()` removes it.

| Control | What it does |
| --- | --- |
| `Control` | The base of everything: bounds, colors, font, text, visibility, enabled state, child controls, and mouse, keyboard and focus events. Use it as a panel. |
| `Form` | A top-level container that starts hidden, with `Show`, `Close`, `Load`, `FormClosed` and `DialogResult`. |
| `Button` | A push button that raises `Click`. |
| `CheckBox` | Toggles `Checked`. Set `Appearance.Button` to draw it as a toggle button. |
| `RadioButton` | One choice of a group. Set `Appearance.Button` for tab-like buttons. |
| `Label` | Text, drawn by an `ITextRenderer`. |

## Custom controls

Override `OnPaint` and draw with `Graphics`. Coordinates are relative to the control, and drawing is clipped to it:

```csharp
public sealed class HealthBar : Control
{
    public float Value { get; set; } = 1;

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.FillRectangle(SolidBrush.Of(Color.DarkRed), ClientRectangle);
        e.Graphics.FillRectangle(SolidBrush.Of(Color.LimeGreen), 0, 0, Width * Value, Height);
        e.Graphics.DrawRectangle(Pen.Of(Color.White), 0, 0, Width - 1, Height - 1);
    }
}
```

`Graphics` draws lines, rectangles, images and text, and measures text. `SolidBrush.Of` and `Pen.Of` return cached instances, so painting every frame doesn't allocate.

## Data binding

Bind a property of a control to a property of your model. When the model raises `PropertyChanged`, the control updates:

```csharp
var player = new Player { Name = "Alice" };   // implements INotifyPropertyChanged
var name = new Label { Bounds = new DrawingRectangle(16, 100, 268, 24) };
options.Controls.Add(name);

name.DataBindings.Add("Text", player, nameof(Player.Name));
player.Name = "Bob";                          // the label now shows "Bob"
```

## Demo

[Aigamo.Saruhashi.MonoGame.Demo](Aigamo.Saruhashi.MonoGame.Demo) shows nested controls, clipping, mouse capture and keyboard input on three screens, and logs their events to the console. Press Escape to quit.

```bash
dotnet run --project Saruhashi/Aigamo.Saruhashi.MonoGame.Demo
```

## License

[MIT](LICENSE). The parts adapted from [dotnet/winforms](https://github.com/dotnet/winforms) are under the .NET Foundation's MIT license, as noted in each file.
