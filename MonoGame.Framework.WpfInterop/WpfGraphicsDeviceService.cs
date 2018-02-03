using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace MonoGame.Framework.WpfInterop
{
	/// <summary>
	/// The <see cref="Microsoft.Xna.Framework.Content.ContentManager"/> needs a <see cref="IGraphicsDeviceService"/> to be in the <see cref="System.ComponentModel.Design.IServiceContainer"/>. This class fulfills this purpose.
	/// </summary>
	public class WpfGraphicsDeviceService : IGraphicsDeviceService, IGraphicsDeviceManager
	{
		/// TODO: rename this class to WpfGraphicsDeviceManager as that's the monogame name for it as well
		/// 
		private readonly WpfGame _host;

		#region Constructors

		/// <summary>
		/// Create a new instance of the dummy. The constructor will autom. add the instance itself to the <see cref="D3D11Host.Services"/> container of <see cref="host"/>.
		/// </summary>
		/// <param name="host"></param>
		public WpfGraphicsDeviceService(WpfGame host)
		{
			_host = host ?? throw new ArgumentNullException(nameof(host));

			if (host.Services.GetService(typeof(IGraphicsDeviceService)) != null)
				throw new NotSupportedException("A graphics device service is already registered.");

			GraphicsDevice = host.GraphicsDevice;
			host.Services.AddService(typeof(IGraphicsDeviceService), this);
			ApplyChanges();
		}

		#endregion

		#region Events

		[Obsolete("Dummy implementation will never call DeviceCreated")]
		public event EventHandler<EventArgs> DeviceCreated;

		[Obsolete("Dummy implementation will never call DeviceDisposing")]
		public event EventHandler<EventArgs> DeviceDisposing;

		[Obsolete("Dummy implementation will never call DeviceReset")]
		public event EventHandler<EventArgs> DeviceReset;

		[Obsolete("Dummy implementation will never call DeviceResetting")]
		public event EventHandler<EventArgs> DeviceResetting;

		#endregion

		#region Properties

		public GraphicsDevice GraphicsDevice { get; }

		public bool PreferMultiSampling { get; set; }

		public int PreferredBackBufferWidth => (int)_host.ActualWidth;

		public int PreferredBackBufferHeight => (int)_host.ActualHeight;

		#endregion

		#region Methods

		public bool BeginDraw()
		{
			return true;
		}

		public void CreateDevice()
		{

		}

		public void EndDraw()
		{

		}

		public void ApplyChanges()
		{
			// set to windows limit, if gpu doesn't support it, monogame will autom. scale it down to the next supported level
			const int msaaLimit = 32;
			var w = Math.Max((int) _host.ActualWidth, 1);
			var h = Math.Max((int) _host.ActualHeight, 1);
			var pp = new PresentationParameters
			{
				MultiSampleCount = PreferMultiSampling ? msaaLimit : 0,
				BackBufferWidth = w,
				BackBufferHeight = h,
				DeviceWindowHandle = IntPtr.Zero
			};
			// TODO: would be so easy to just call reset. but for some reason monogame doesn't want the WindowHandle to be null on reset (but it's totally fine to be null on create)
			//GraphicsDevice.Reset(pp);
			// manually work around it by telling our base implementation to handle the changes
			_host.RecreateGraphicsDevice(pp);
		}

		#endregion
	}
}