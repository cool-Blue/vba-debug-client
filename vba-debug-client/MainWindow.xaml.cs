﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace vba_debug_client
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private HwndSource _hwndSource;

		public static MainWindow Instance;

		private readonly Log _pLog;

		public MainWindow()
		{
			InitializeComponent();
			Title = "VBA Debug Log";
			Instance = this;
			_pLog = new Log(Instance.LogTest);
		}


		delegate void del();

		private delegate void Log(IntPtr hwnd, string message, string code, string p);

		private void LogTest(IntPtr hwnd, string message, string code, string p2)
		{
			var msg = logBox.Text + _hwndSource.Handle.ToString("X") + "\t" + hwnd.ToString("X") + "\t" + message + "\t" + code + "\t" + p2 + "\n";
			logBox.Text = msg;
			logBox.ScrollToEnd();
		}

		private Boolean _logging;

		private void pause_Click(object sender, RoutedEventArgs e)
		{
			_logging = !_logging;
			pause.Content = _logging ? "Pause" : "Log";
		}

		/// <summary>
		/// AddHook Handle WndProc messages in WPF
		/// This cannot be done in a Window's constructor as a handle window handle won't at that point, so there won't be a HwndSource.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			_hwndSource = PresentationSource.FromVisual(this) as HwndSource;
			if (_hwndSource == null) return;
			_hwndSource.AddHook(WndProc);
			description.Text = "Handle: " + _hwndSource.Handle.ToString("X") + "\t";
		}
		/// <summary>
		/// WndProc matches the HwndSourceHook delegate signature so it can be passed to AddHook() as a callback. This is the same as overriding a Windows.Form's WncProc method.
		/// </summary>
		/// <param name="hwnd">The window handle</param>
		/// <param name="msg">The message ID</param>
		/// <param name="wParam">The message's wParam value, historically used in the win32 api for handles and integers</param>
		/// <param name="lParam">The message's lParam value, historically used in the win32 api to pass pointers</param>
		/// <param name="handled">A value that indicates whether the message was handled</param>
		/// <returns></returns>
		private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (!Instance._logging) return IntPtr.Zero;

			switch ((WindowMessage)msg)
			{
				case WindowMessage.WM_COPYDATA:
					break;
				case WindowMessage.VBA_PRINT:
					break;
				default:
					return IntPtr.Zero;
			}
			if (!Instance._logging || (WindowMessage) msg != WindowMessage.WM_COPYDATA) return IntPtr.Zero;
			var message = "";
			var subCode = "";
			COPYDATASTRUCT cds = new COPYDATASTRUCT();
			//var t = typeof(cds);
			var t = cds.GetType();
			cds = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, t);
			var messageContent = Marshal.PtrToStringAnsi(cds.lpData, cds.cbData); ;
			try
			{
				message = Messages.names[(WindowMessage)msg];

			}
			catch
			{
				message = "UNKNOWN MESSAGE\t:\t" + msg;
			}

			try
			{
				subCode = Messages.parameters[(WindowMessageParameter)wParam.ToInt32()];
			}
			catch
			{
				subCode = "UNKNOWN CODE\t:\t" + wParam;
			}

			//Yep
			Instance.Dispatcher.BeginInvoke(
				new Action(() => Instance.LogTest(hwnd, message, subCode, messageContent)),
				DispatcherPriority.Background
			);

			//Yep
			Instance.Dispatcher.BeginInvoke(
				method: (Action)(() => Instance.LogTest(hwnd, message, subCode, messageContent)),
				priority: DispatcherPriority.Background
			);

			//Yep
			//requires
			//	private delegate void Log(IntPtr hwnd, string message, string code, string p);
			//in owner class
			Instance.Dispatcher.BeginInvoke(
				new Log(Instance.LogTest),
				args: new object[] {hwnd, message, subCode, messageContent},
				priority: DispatcherPriority.Background
			);

			//Yep
			//requires
			//	private readonly Log _pLog;	// in owner class
			//and in owner class constructor	
			//	Instance = this;
			//	_pLog = new Log(Instance.LogTest);
			Instance.Dispatcher.BeginInvoke(
				Instance._pLog,
				args: new object[] { hwnd, message, subCode, messageContent },
				priority: DispatcherPriority.Background
			);

			//Yep
			//requires
			//	private delegate void Log(IntPtr hwnd, string message, string code, string p);
			//in owner class
			Instance.Dispatcher.BeginInvoke(
				(Log)(Instance.LogTest),
				args: new object[] { hwnd, message, subCode, messageContent },
				priority: DispatcherPriority.Background
			);

			//Yep
			//Action del = () => Instance.LogTest(hwnd, message, subCode, messageContent);
			//var del = new Action(() => Instance.LogTest(hwnd, message, subCode, messageContent));
			//Instance.Dispatcher.BeginInvoke(
			//    del,
			//    DispatcherPriority.Background
			//);

			//Yep
			//delegate void del(); in parent Class
			//Instance.Dispatcher.BeginInvoke(
			//    (del)(() => Instance.LogTest(hwnd, message, subCode, messageContent)),
			//    DispatcherPriority.Background
			//);

			var delLog = new Action<IntPtr, string, string, string>(Instance.LogTest);
			//var del = new Action(() => Instance.LogTest(hwnd, message, subCode, messageContent));
			//Nope
			//Instance.Dispatcher.BeginInvoke(
			//    delLog, hwnd, message, subCode, messageContent,
			//    DispatcherPriority.Background
			//);

			//Nope
			//Instance.Dispatcher.BeginInvoke(
			//    Log(hwnd, message, subCode, messageContent),
			//    DispatcherPriority.Background
			//);
			
			return IntPtr.Zero;

		}

		private void clear_Click(object sender, RoutedEventArgs e)
		{
			logBox.Text = "";
		}

	}
}

namespace vba_debug_client
{
	class MessageWindow : Window
	{
		private readonly IntPtr HWND_MESSAGE = new IntPtr(-3); 
		private HwndSourceParameters _windowState;
		private HwndSource _hwndSource;

		Boolean Logging { get; set; }
		private Delegate _log;

		public static MessageWindow Instance;

		public MessageWindow(Dispatcher output, Delegate log)
		{
			Logging = false;
			_log = log;
			Title = "VBA Debug Log";
			_windowState = new HwndSourceParameters(Title) {ParentWindow = HWND_MESSAGE};
			_hwndSource = new HwndSource(_windowState);
			Instance = this;
		}

		private void DispatchLog(string data)
		{
			
		}

		/// <summary>
		/// AddHook Handle WndProc messages in WPF
		/// This cannot be done in a Window's constructor as a handle window handle won't at that point, so there won't be a HwndSource.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			_hwndSource = PresentationSource.FromVisual(this) as HwndSource;
			if (_hwndSource == null) return;
			_hwndSource.AddHook(WndProc);
		}
		/// <summary>
		/// WndProc matches the HwndSourceHook delegate signature so it can be passed to AddHook() as a callback. This is the same as overriding a Windows.Form's WncProc method.
		/// </summary>
		/// <param name="hwnd">The window handle</param>
		/// <param name="msg">The message ID</param>
		/// <param name="wParam">The message's wParam value, historically used in the win32 api for handles and integers</param>
		/// <param name="lParam">The message's lParam value, historically used in the win32 api to pass pointers</param>
		/// <param name="handled">A value that indicates whether the message was handled</param>
		/// <returns></returns>
		private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (!Instance.Logging || (WindowMessage)msg != WindowMessage.WM_COPYDATA) return IntPtr.Zero;
			var message = "";
			var subCode = "";
			var cds = new COPYDATASTRUCT();
			var t = cds.GetType();
			cds = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, t);
			var messageContent = Marshal.PtrToStringAnsi(cds.lpData, cds.cbData); ;
			try
			{
				message = Messages.names[(WindowMessage)msg];

			}
			catch
			{
				message = "UNKNOWN MESSAGE\t:\t" + msg;
			}

			try
			{
				subCode = Messages.parameters[(WindowMessageParameter)wParam.ToInt32()];
			}
			catch
			{
				subCode = "UNKNOWN CODE\t:\t" + wParam;
			}

			//DispatchLog(hwnd, message, subCode, messageContent);

			return IntPtr.Zero;

		}
	} 
}