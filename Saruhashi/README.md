# Saruhashi
Saruhashi is a UI framework for [MonoGame](https://github.com/MonoGame/MonoGame). This project is mainly used by [OpenNSPW](https://github.com/OpenNSPW/OpenNSPW).

## Installation

```
PM> Install-Package Aigamo.Saruhashi.MonoGame
```

## Usage

### Minimal Configuration

```csharp
using Aigamo.Saruhashi;
using Aigamo.Saruhashi.MonoGame;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Input.InputListeners;
using DrawingRectangle = System.Drawing.Rectangle;
using SaruhashiKeys = Aigamo.Saruhashi.Keys;
using SaruhashiMouseListener = Aigamo.Saruhashi.MonoGame.MouseListener;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

private WindowManager _windowManager;

protected override void LoadContent()
{
    var mouseListener = new SaruhashiMouseListener();
    var keyboardListener = new KeyboardListener(new KeyboardListenerSettings
    {
        InitialDelayMilliseconds = 500,
        RepeatDelayMilliseconds = 30,
    });
    Components.Add(new InputListenerComponent(this, mouseListener, keyboardListener));

    _windowManager = new WindowManager(new DrawingRectangle(0, 0, 1024, 768), new MonoGameGraphicsFactory(_spriteBatch));
    mouseListener.MouseDown += (sender, e) => _windowManager.OnMouseDown(e);
    mouseListener.MouseMove += (sender, e) => _windowManager.OnMouseMove(e);
    mouseListener.MouseUp += (sender, e) => _windowManager.OnMouseUp(e);
    keyboardListener.KeyPressed += (sender, e) => _windowManager.OnKeyDown(new KeyEventArgs((SaruhashiKeys)e.Key));
    keyboardListener.KeyReleased += (sender, e) => _windowManager.OnKeyUp(new KeyEventArgs((SaruhashiKeys)e.Key));
    Window.TextInput += (sender, e) => _windowManager.OnKeyPress(new KeyPressEventArgs(e.Character));
}

private readonly RasterizerState _rasterizerState = new RasterizerState
{
    ScissorTestEnable = true,
};

protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(Color.CornflowerBlue);

    base.Draw(gameTime);

    _windowManager.Draw();
}
```

### Form

```csharp
var form = new Form();
WindowManager.Root.Controls.Add(form);
form.Show();
```

### Button

```csharp
var button = new Button
{
    GetText = () => $"{Environment.TickCount}",
};
button.Click += (sender, e) => Game.Exit();
WindowManager.Root.Controls.Add(button);
```

### CheckBox

```csharp
var @checked = false;
var checkBox = new CheckBox
{
    Appearance = Appearance.Button,
    AutoCheck = false,
    IsChecked = () => @checked,
};
checkBox.Click += (sender, e) => @checked = !@checked;
WindowManager.Root.Controls.Add(checkBox);
```

### RadioButton

```csharp
var @checked = false;
var radioButton = new RadioButton
{
    Appearance = Appearance.Button,
    AutoCheck = false,
    IsChecked = () => @checked,
};
radioButton.Click += (sender, e) => @checked = !@checked;
WindowManager.Root.Controls.Add(radioButton);
```
