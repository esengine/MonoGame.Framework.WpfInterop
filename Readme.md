# MonoGame WPF Interop

This adds WPF support to MonoGame (tested with version 3.5.1.1679 and 3.6.x).

Note that you **need** MonoGame.Framework.WindowsDX (as this interop uses SharpDX). WPF only supports DirectX.

You can host as many MonoGame controls in Wpf as you want by using the WpfGame control. Note that they are however limited to 60 FPS (this is a WPF limit).

___
## Available on NuGet  [![NuGet Status](https://img.shields.io/nuget/v/MonoGame.Framework.WpfInterop.svg?style=flat)](https://www.nuget.org/packages/MonoGame.Framework.WpfInterop/)

https://nuget.org/packages/MonoGame.Framework.WpfInterop/

	PM> Install-Package MonoGame.Framework.WpfInterop
   
By adding the NuGet package to a project it is possible to host MonoGame inside WPF windows.

## Example

```csharp

public class MyGame : WpfGame
{
	private IGraphicsDeviceService _graphicsDeviceManager;
	private WpfKeyboard _keyboard;
	private WpfMouse _mouse;

	protected override void Initialize()
	{
		// must be initialized. required by Content loading and rendering (will add itself to the Services)
		_graphicsDeviceManager = new WpfGraphicsDeviceService(this);

		// wpf and keyboard need reference to the host control in order to receive input
		// this means every WpfGame control will have it's own keyboard & mouse manager which will only react if the mouse is in the control
		_keyboard = new WpfKeyboard(this);
		_mouse = new WpfMouse(this);
		
		// must be called after the WpfGraphicsDeviceService instance was created
		base.Initialize();

		// content loading now possible
	}

	protected override void Update(GameTime time)
	{
		// every update we can now query the keyboard & mouse for our WpfGame
		var mouseState = _mouse.GetState();
		var keyboardState = _keyboard.GetState();
	}

	protected override void Draw(GameTime time)
	{
	}
}

```

Now you can use it in any of your WPF forms:

&lt;MyGame Width="800" Height="480" />

# Features

Some of the Monogame classes are incompatible with WPF (Game always spawns its own window, Mouse doesn't care which control has focus, ..) so they had to be reimplemented.

As a convention, all reimplemented classes will have the prefix Wpf:

* WpfGame as a replacement for Game class. Note that due to WPF limitations the WpfGame will always run at a maximum 60 FPS in fixed step (Update and Draw are always called, no Updates are skipped). It is possible to lower the framerate via TargetElapsedTime)
* WpfMouse and WpfKeyboard provide input per host instance. When multiple WpfGame instances are spawned, only one will receive input at any time
* WpfGraphicsDeviceService as a dummy implementation of IGraphicsDeviceService (required by the content manager)
* WpfGameComponent and WpfDrawableGameComponent as a replacement for the original ones which required a reference to a Game instance

## Mouse behaviour

### Focus

By default the game takes focus on mouse (h)over. This can be disabled via the FocusOnMouseOver property of WpfGame

### Mouse capture

By default the game captures the mouse. This allows capture of mouse events outside the game (e.g. user holds and drags the mouse outside the window, then releases it -> game will still receive mouse up event). The downside is that no overlayed controls (e.g. textboxes on top of the game) will ever receive focus.

Alternatively this can be toggled off via CaptureMouseWithin property of WpfMouse and then allows focus on overlayed controls. The downside is that mouse events outside the game window are no longer registered (e.g. user holds and drags the mouse outside the window, then releases it -> game will still think the mouse is down until the window receives focus again)

# Gotchas

## RenderTargets

Rendertargets work slightly different in this WPF interop.

In a normal monogame the rendertarget would be used like this:

```

	// Draw into rendertarget
	GraphicsDevice.SetRenderTarget(_rendertarget);
	GraphicsDevice.Clear(Color.Transparent);
	_spriteBatch.Begin();
	_spriteBatch.Draw(_texture, Vector2.Zero, Color.White);
	_spriteBatch.End();

	// setting null means we want to draw to the backbuffer again
	GraphicsDevice.SetRenderTarget(null);


	// these draw calls will now render onto backbuffer
	GraphicsDevice.Clear(Color.CornflowerBlue);
	_spriteBatch.Begin();
	_spriteBatch.Draw(this.rendertarget, Vector2.Zero, Color.White);
	_spriteBatch.End();
```

Instead there is always a rendertarget (internally used to display the renderoutput in WPF), thus in a WPF control the code needs to look like this instead:

```

	// get and cache the wpf rendertarget (there is always a default rendertarget)
	var wpfRenderTarget = (RenderTarget2D)GraphicsDevice.GetRenderTargets()[0].RenderTarget;

	// Draw into custom rendertarget
	GraphicsDevice.SetRenderTarget(_rendertarget);
	GraphicsDevice.Clear(Color.Transparent);
	_spriteBatch.Begin();
	_spriteBatch.Draw(_texture, Vector2.Zero, Color.White);
	_spriteBatch.End();

	// instead of setting null, set it back to the wpf rendertarget
	// this will ensure that the output will end up visible to the user
	GraphicsDevice.SetRenderTarget(wpfRenderTarget);


	// these draw calls will now render onto backbuffer
	GraphicsDevice.Clear(Color.CornflowerBlue);
	_spriteBatch.Begin();
	_spriteBatch.Draw(this.rendertarget, Vector2.Zero, Color.White);
	_spriteBatch.End();
```

**The reason for this behaviour that the interop sample cannot use the backbuffer (null) and instead needs to use its own rendertarget.**

## TabControls

It is perfectly possible to use the WpfGame controls inside TabControls.

By default, WPF fully unloads any tab that is deactivated (e.g. when switching to another tab) and fully reloads the tab when switching back.

The WpfGame **does not unload** in these cases.

### WpfGame.Activated/Deactivated

When a WpfGame is hosted inside a tab and the tab is changed, the WpfGame instance is **not unloaded**. Instead, Deactivated is fired. Likewise when the tab is activated again, Activated is fired.

If the parent window loses focus, Deactivated is fired (but only for the active tab) and when the window receives focus, Activated is called again (only for the active tab).

### Initialize/Dispose

Initialize is only called once (per instance, when the window is created) and Dispose is called for every game instance once the window closes.

This means, that initialize is called even for those instances that are in "disabled" tabs. However IsActive can be used to determine whether the current game is inside the active tab (see below).

### WpfGame.IsActive

Update/Draw are still called for all inactive tabs and any inactive tab has the IsActive property on WpfGame set to false. Only the active tab has IsActive set to true (and only when the window is the currently active window).

# Roadmap

* Properly implement GraphicsDeviceService (call all events when appropriate)

# Changelog

**v1.5.3.999 (not an official release)**

* implemented MSAA based on latest monogame develop branch state (requires using this fork with important fixes: https://github.com/MarcStan/MonoGame)

**v1.5.3**

* Now locking D3D11 base image while cloning rendertarget (fixes hitching issues that occured on some systems)

**v1.5.2**

* Fixed bug where User calling Dispose manually would decrement reference counter for graphicsdevice twice ([issue #14](https://gitlab.com/MarcStan/MonoGame.Framework.WpfInterop/issues/14))

**v1.5.1**

* Fixed bug that causes flickering when draw calls put the system under load ([issue #12](https://gitlab.com/MarcStan/MonoGame.Framework.WpfInterop/issues/12))

**v1.5.0**

* Fixed bug that causes crashes when WpfGame was hosted inside TabControls and tabs where switched
* Added Activated/Deativated events on WpfGame that fire on window focus/focus lost (see section Gotchas\TabControls for details when used inside tabs)
* Added IsActive property on WpfGame that indicates whether the current windows is the active one or not (see also section Gotchas\TabvControls for details when used inside tabs)

**v1.4.0**

* WpfGame now has "FocusOnMouseOver" which allows changing the behaviour (defaults to true)
* WpfMouse now has "CaptureMouseWithin" which allows changing the capture behaviour (defaults to true)
* TargetElapsedTime can now be set by the user to reduce the framerate if desired (defaults to 60 fps)

**v1.3.2**

* Fixed mouse scrollwheel value incorrectly resetting whenever any other mouse event fired (move, button down/up)
* Exceptions thrown in Initialize() are no longer silently swallowed (bug in x64 WPF OnLoaded exceptions handling). Exception is now rethrown manually

**v1.3.1**

* Moved to Gitlab and updated all links to Gitlab

**v1.3.0**

* Added support for GameComponents (WpfGameComponent and WpfDrawableGameComponent) that mirror the behaviour of the original ones (which cannot be used due to requiring a reference to Game)

**v1.2.2**

* fixed mistake in readme

**v1.2.1**

* Bugfix in demoscene

**v1.2.0**

* Added dependency on MonoGame.Framework.WindowsDX nuget package to prevent accidental use of different platform versions

**v1.1.1**

* Added SetCursor function to WpfMouse which now allows resetting the cursor (the monogame Mouse.SetPosition function would throw due to not finding a Winforms window)

**v1.1.0**

* New class "WpfGame" that derives from D3D11Host. It provides a cleaner interface and is more similar to the original Game class
* Input is now available via WpfMouse and WpfKeyboard

**v1.0.0**

* Initial release, can render and load content
* Input is not working yet