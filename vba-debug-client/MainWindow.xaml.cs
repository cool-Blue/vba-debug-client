using System;
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

		private readonly LogInvokers _logInvokers;

		private EInvoker _invokerType;

		private string _loggerTransport;

		private readonly Logger _logger;

		public MainWindow()
		{
			InitializeComponent();
			Title = "VBA Debug Log";
			Instance = this;
			_logger = new Logger(logBox);
			_logInvokers = new LogInvokers(Dispatcher, _logger.Loggers["text"]);	// bind the dictionary to the inst context
			_invokerType = EInvoker.CastAction;
			_logging = true;
		}

		public void LogTest(IntPtr hwnd, string code, string p2)
		{
			var msg = logBox.Text + _hwndSource.Handle.ToString("X") + "\t" + hwnd.ToString("X") + "\t" + code + "\t" + p2 + "\n";
			logBox.Text = msg;
			logBox.ScrollToEnd();
		}
		private Boolean _logging;
		private class Logger
		{
			// todo move Log delegate declaration here

			private readonly TextBox _logBox;

			public void Clear ()
			{
				_content.Clear();
				_logBox.Text = "";
			}

			public Dictionary<string, LogInvokers.Log> Loggers;

			private readonly StringBuilder _content = new StringBuilder();
			private int _newLines;
			private const int PAGE = 100;

			public Logger (TextBox logBox)
			{
				_logBox = logBox;
				Loggers = new Dictionary<string, LogInvokers.Log>
				{
					{
						"text", (IntPtr hwnd, IntPtr sender, string p2) =>
						{
							if (p2 != null)
								_content.AppendLine(p2);
							if (p2 != null && PAGE > ++_newLines) return;
							_logBox.Text = _content.ToString();
							_logBox.ScrollToEnd();
							_newLines = 0;
						}
					},
					{
						"json", (IntPtr hwnd, IntPtr sender, string p2) =>
						{
							if (p2 != null)
								_content.AppendLine(p2);
							if (p2 != null && PAGE > ++_newLines) return;
							_logBox.Text = _content.ToString();
							_logBox.ScrollToEnd();
							_newLines = 0;
						}
					}
				};
			}

			public enum logEdge : uint
			{
				callProc,
				logReport,
				ExitProc
			}

			private class LoggerMessage
			{
				public logEdge edge { get; set; }
				public string caller { get; set; }
				public string timestamp { get; set; }
				public string context { get; set; }
				public string message { get; set; }
				public string error { get; set; }
				public string dt { get; set; }
			}
		}

		private void pause_Click(object sender, RoutedEventArgs e)
		{
			string content;
			Toggle(ref _logging, out content, new List<string> {"Logging", "Paused"});
			pause.Content = content;
		}
		private static void Toggle (ref Boolean flag, out string value, IReadOnlyList<string> options)
		{
			flag = !flag;
			value = flag ? options[0] : options[1];
		}
		private void clear_Click (object sender, RoutedEventArgs e)
		{
			summary.Text = "";
			_logger.Clear();
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

		private static readonly WmCopyData wmCopyData = new WmCopyData();

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
			// todo try single argument overload
		{
			switch ((Messages.WindowMessage)msg)
			{
				case Messages.WindowMessage.WM_COPYDATA:
					if (!Instance._logging) return IntPtr.Zero;

					// wParam is the hwnd of the sender
					// lParam is a pointer to a COPYDATASTRUCT struct that packages the message data
					// the this case uses the second an third elements to marshal a string from the package
					// todo implement a custom data structure inside the COPYDATASTRUCT to transmit metta data
					//	cancelled in favour of json serialisation
					// todo encapsulate this in a message class
					// todo impliment a receivedData event

					try
					{
						var messageContent = wmCopyData.ToString(lParam);
						var sender = wParam;

						Instance._logInvokers.LoggerTypes[Instance._invokerType](hwnd, sender, messageContent);
					}
					catch
					{
						// todo handle exception
						break;
					}

					break;

				case Messages.WindowMessage.VBA_LOGGING:
					// pause or un-pause logging
					Instance.pause_Click(Instance.pause, new RoutedEventArgs(Button.ClickEvent));
					break;

				case Messages.WindowMessage.VBA_PRINT:
					// select the delegate to be used for logging
					Instance._invokerType = (EInvoker)wParam.ToInt32();
					break;

				case Messages.WindowMessage.VBA_CLEAR:
					Instance.clear_Click(Instance.clear, new RoutedEventArgs(Button.ClickEvent));
					break;

				case Messages.WindowMessage.VBA_TRANSPORT:
					// select text or json transport mode
					// todo add selector in excel
					// todo move to subclass of WM_COPYDATA
					Instance._loggerTransport = Instance._logger.Loggers.Keys.ToList()[wParam.ToInt32()];
					break;

				default:
					return IntPtr.Zero;
			}
			
			return IntPtr.Zero;

		}
	}
}

namespace vba_debug_client
{
	class MessageOnlyWindow
	{
		private readonly IntPtr HWND_MESSAGE = new IntPtr(-3);
		private HwndSource _hwndSource;

		public string Title { get; set; }
		public Boolean Enabled { get; set; }

		public static MessageOnlyWindow Instance;

		private EInvoker _invoker;

		public MessageOnlyWindow (Dispatcher output, Delegate log)
		{
			Enabled = false;
			Title = "VBA Debug Log";
			var windowState = new HwndSourceParameters(Title) {ParentWindow = HWND_MESSAGE};
			_hwndSource = new HwndSource(windowState);
			_hwndSource.AddHook(WndProc);
			Instance = this;
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
		private static IntPtr WndProc (IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{

			return IntPtr.Zero;

		}
	} 
}