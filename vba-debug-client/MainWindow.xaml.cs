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
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Newtonsoft.Json;


namespace vba_debug_client
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public static MainWindow Instance;

		private string _loggerTransport;

		private readonly Logger _logger;

		private readonly SetFocus _setFocus;

		private readonly ToggleButton _logAll;

		private readonly ToggleButton _logging;

		public MainWindow()
		{
			InitializeComponent();
			Title = "VBA Debug Log";
			this.ShowActivated = true;
			Instance = this;
			_logger = new Logger(logBox);
			_loggerTransport = "text";
			_setFocus = new SetFocus(toExcel);
			_logging = new ToggleButton(pause, new List<string> {"Logging", "Paused"});
			_logAll = new ToggleButton(logAll, new List<string> {"Log All", "Log VBA"}, false);
		}

		private class Logger
		{
			// todo move Log delegate declaration here

			internal delegate void Log(IntPtr hwnd, IntPtr sender, string p);

			private readonly TextBox _logBox;

			public readonly Dictionary<string, Log> Loggers;

			private readonly StringBuilder _content = new StringBuilder();
			private int _newLines;
			private const int PAGE = 100;

			public void Clear()
			{
				_content.Clear();
				_logBox.Text = "";
			}

			public void LogTest(IntPtr hwnd, int msg, string message, IntPtr wParam, IntPtr lParam)
			{
				var l = hwnd.ToString("X") + "\t" + msg.ToString("X4") + ": " + message + "\t" + wParam.ToString("X") + "\t" +
				        lParam.ToString("X");
				if (message != null)
				{
					_content.AppendLine(l);
					testLogging = true;
				}
				if (message != null && PAGE > ++_newLines) return;
				_logBox.Text = _content.ToString();
				_logBox.ScrollToEnd();
				_newLines = 0;
				testLogging = false;

			}

			public Boolean testLogging { get; private set; }

			public void TestLogFlush()
			{
				LogTest(IntPtr.Zero, 0, null, IntPtr.Zero, IntPtr.Zero);

			}

			public Logger(TextBox logBox)
			{
				_logBox = logBox;
				var state = new LoggerState();

				Loggers = new Dictionary<string, Log>
				{
					{
						"text",
						(IntPtr hwnd, IntPtr sender, string p2) => _output(p2)
					},
					{
						"json",
						(IntPtr hwnd, IntPtr sender, string p2) => _output(state.ToString(p2))
					}
				};
			}

			private void _output(string l)
			{
				if (l != null)
					_content.AppendLine(l);
				if (l != null && PAGE > ++_newLines) return;
				_logBox.Text = _content.ToString();
				_logBox.ScrollToEnd();
				_newLines = 0;
			}

			private class LoggerState
			{

				public enum logEdge : uint
				{
					callProc,
					logReport,
					ExitProc
				}

				public class LoggerMessage
				{
					public int callDepth { get; set; }
					public logEdge edge { get; set; }
					public string caller { get; set; }
					public string timestamp { get; set; }
					public string context { get; set; }
					public string message { get; set; }
					public string error { get; set; }
					public string dt { get; set; }
				}

				private const int TsLen = 12;
				private const int TxtWidth = 90;
				private LoggerMessage _msg;
				private string _error;

				public string ToString(string json)
				{
					if (json == null)
						return null;

					var output = new StringBuilder();
					try
					{

						_msg = JsonConvert.DeserializeObject<LoggerState.LoggerMessage>(json);
						output.AppendFormat("{0,-" + (TsLen + _msg.callDepth) + "}", _msg.timestamp);
						output.AppendFormat("{0," + (_msg.caller.Length + 1) + "}:", _msg.edge != logEdge.logReport ? _msg.caller : " ");
						output.AppendFormat(" {0}", _msg.context == "" ? _msg.message : _msg.context + "  " + _msg.message);
						if (_msg.error.Length == 0)
						{
							output.Replace(output.ToString(), String.Format("{0,-" + TxtWidth + "}" + "{1}", output, _msg.dt));
						}
						else
						{
							_error = String.Format("{0,-" + (TsLen) + "}{1}{2}", " ", new string('*', _msg.callDepth), _msg.error);
							if (_msg.dt.Length != 0)
							{
								_error = String.Format("{0,-" + TxtWidth + "}" + "{1}", _error, _msg.dt);
							}
							output.AppendLine(_error);
						}


					}
					catch
					{
						output.AppendFormat("ERROR: Invalid Json\n\t{0}", json);
					}
					return output.ToString();

				}

				public LoggerState()
				{
				}
			}

		}

		private class ToggleButton
		{
			private readonly Button _b;
			private readonly List<string> _options;
			public Boolean On { get; private set; }

			public ToggleButton(Button b, List<string> options, Boolean on = true)
			{
				_b = b;
				_options = options;
				_b.Click += Click;
				On = on;
			}

			public void Click(object sender, RoutedEventArgs e)
			{
				On = !On;
				_b.Content = On ? _options[0] : _options[1];
				;
			}

		}

		private void clear_Click(object sender, RoutedEventArgs e)
		{
			_logger.Clear();
		}

		private class SetFocus
		{
			public SetFocus(Button b)
			{
				_b = b;
				_b.Click += toExcel_click;
			}

			private Button _b;

			public IntPtr HWnd { set; private get; }

			private void toExcel_click(object sender, RoutedEventArgs e)
			{
				Win32.SetForegroundWindow(HWnd);
				Console.WriteLine(HWnd.ToString("X"));
			}
		}

		private HwndSource _hwndSource;

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

		private static readonly Dictionary<Messages.WindowMessage, HwndSourceHook> ProcessMessage =
			new Dictionary<Messages.WindowMessage, HwndSourceHook>
			{
				{
					Messages.WindowMessage.VBA_REQ_FOREGROUND,
					(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
					{
						Win32.SetForegroundWindow(wParam);
						Instance._setFocus.HWnd = wParam;
						return IntPtr.Zero;
					}
				},
				{
					Messages.WindowMessage.WM_COPYDATA,
					(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
					{
						if (!Instance._logging.On) return IntPtr.Zero;

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

							Instance._logger.Loggers[Instance._loggerTransport](hwnd, sender, messageContent);
						}
						catch
						{
							// todo handle exception
						}
						return IntPtr.Zero;
					}
				},
				{
					Messages.WindowMessage.VBA_LOGGING,
					(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
					{
						// pause or un-pause logging
						Instance._logging.Click(Instance.pause, new RoutedEventArgs(ButtonBase.ClickEvent));
						if (Instance._logger.testLogging)
							Instance._logger.TestLogFlush();
						return IntPtr.Zero;
					}
				},
				{
					Messages.WindowMessage.VBA_CLEAR,
					(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
					{
						Instance.clear_Click(Instance.clear, new RoutedEventArgs(ButtonBase.ClickEvent));
						return IntPtr.Zero;
					}
				},
				{
					Messages.WindowMessage.VBA_TRANSPORT,
					(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
					{
						// select text or json transport mode
						// todo add selector in excel
						// todo move to subclass of WM_COPYDATA
						Instance._loggerTransport = Instance._logger.Loggers.Keys.ToList()[(int) wParam];

						if (Instance._logger.testLogging)
							Instance._logger.TestLogFlush();
						return IntPtr.Zero;
					}
				},
				{
					Messages.WindowMessage.VBA_DEFAULT,
					(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
					{
						handled = false;
						try
						{

							if (!Instance._logAll.On) return IntPtr.Zero;

							var message = Messages.names.ContainsKey((Messages.WindowMessage) msg)
								? Messages.names[(Messages.WindowMessage) msg]
								: "UNKNOWN MESSAGE";

							Instance._logger.LogTest(hwnd, msg, message, wParam, lParam);

						}
						catch
						{
							// it really doesn't matter
						}
						return IntPtr.Zero;
					}
				},

			};


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
			HwndSourceHook p;
			handled = true;
			return ProcessMessage.TryGetValue((Messages.WindowMessage)msg, out p)
				? p(hwnd, msg, wParam, lParam, ref handled)
				: ProcessMessage[Messages.WindowMessage.VBA_DEFAULT](hwnd, msg, wParam, lParam, ref handled);
			
		}

	}
}

namespace vba_debug_client
{
	class MessageOnlyWindow
	{
		private readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

		public string Title { get; set; }
		public Boolean Enabled { get; set; }

		public static MessageOnlyWindow Instance;

		public MessageOnlyWindow (HwndSourceHook wndProc)
		{
			var windowState = new HwndSourceParameters(Title) {ParentWindow = HWND_MESSAGE};
			var hwndSource = new HwndSource(windowState);

			Enabled = false;
			Title = "VBA Debug Log";

			hwndSource.AddHook(wndProc);
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