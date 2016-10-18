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

		public readonly LogInvokers.Log PLog;

		private ELogger _invoker;

		public MainWindow()
		{
			InitializeComponent();
			Title = "VBA Debug Log";
			LogInvokers.Instance = Instance = this;
			PLog = new LogInvokers.Log(Instance.LogDebug);
			_invoker = ELogger.CastAction;
			_logging = true;
		}


		public void LogTest(IntPtr hwnd, string message, string code, string p2)
		{
			var msg = logBox.Text + _hwndSource.Handle.ToString("X") + "\t" + hwnd.ToString("X") + "\t" + message + "\t" + code + "\t" + p2 + "\n";
			logBox.Text = msg;
			logBox.ScrollToEnd();
		}

		private readonly StringBuilder _content = new StringBuilder();
		private int _newLines = 0;
		private const int PAGE = 100;
		public void LogDebug (IntPtr hwnd, string message, string code, string p2)
		{
			if(p2 != null)
				_content.AppendLine(p2);
			if (p2 != null && PAGE > ++_newLines) return;
				logBox.Text = _content.ToString();
				logBox.ScrollToEnd();
				_newLines = 0;
		}

		private Boolean _logging;

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
			switch ((WindowMessage)msg)
			{
				case WindowMessage.WM_COPYDATA:
					if (!Instance._logging) return IntPtr.Zero;
					// todo implement a custom data structure inside the COPYDATASTRUCT to transmit metta data
					var cds = new COPYDATASTRUCT();
					//var t = typeof(cds);
					var t = cds.GetType();
					cds = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, t);
					var messageContent = Marshal.PtrToStringAnsi(cds.lpData, cds.cbData);
					var message = "";
					try
					{
						message = Messages.names[(WindowMessage)msg];
					}
					catch
					{
						message = "UNKNOWN MESSAGE\t:\t" + msg;
					}

					var subCode = "";
					try
					{
						subCode = Messages.parameters[(WindowMessageParameter)wParam.ToInt32()];
					}
					catch
					{
						subCode = "UNKNOWN CODE\t:\t" + wParam;
					}

					try
					{
						if (null != messageContent) LogInvokers.LogInvoker[Instance._invoker](hwnd, message, subCode, messageContent);
					}
					catch
					{
						// todo handle exception
						break;
					}

					break;

				case WindowMessage.VBA_LOGGING:
					Instance.pause_Click(Instance.pause, new RoutedEventArgs(Button.ClickEvent));
					break;

				case WindowMessage.VBA_PRINT:
					Instance._invoker = (ELogger)wParam.ToInt32();
					break;

				case WindowMessage.VBA_CLEAR:
					Instance.clear_Click(Instance.clear, new RoutedEventArgs(Button.ClickEvent));
					break;

				case WindowMessage.VBA_EOF:	// todo refactor as null message in WM_COPYDATA
					try
					{
						LogInvokers.LogInvoker[Instance._invoker](hwnd, "", "", null);
					}
					catch
					{
						// todo handle exception
						break;
					}

					break;

				default:
					return IntPtr.Zero;
			}
			
			return IntPtr.Zero;

		}

		private void clear_Click(object sender, RoutedEventArgs e)
		{
			summary.Text = logBox.Text = "";
			_content.Clear();
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

		private ELogger _invoker;

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
			if (!Instance.Enabled) return IntPtr.Zero;
			switch ((WindowMessage)msg)
			{
				case WindowMessage.WM_COPYDATA:
					// todo implement a custom data structure inside the COPYDATASTRUCT to transmit metta data
					var cds = new COPYDATASTRUCT();
					//var t = typeof(cds);
					var t = cds.GetType();
					cds = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, t);
					var messageContent = Marshal.PtrToStringAnsi(cds.lpData, cds.cbData);
					var message = "";
					try
					{
						message = Messages.names[(WindowMessage)msg];
					}
					catch
					{
						message = "UNKNOWN MESSAGE\t:\t" + msg;
					}

					var subCode = "";
					try
					{
						subCode = Messages.parameters[(WindowMessageParameter)wParam.ToInt32()];
					}
					catch
					{
						subCode = "UNKNOWN CODE\t:\t" + wParam;
					}

					try
					{
						if (null != messageContent) LogInvokers.LogInvoker[Instance._invoker](hwnd, message, subCode, messageContent);
					}
					catch
					{
						// todo handle exception
						break;
					}

					break;

				case WindowMessage.VBA_PRINT:
					Instance._invoker = (ELogger)wParam.ToInt32();
					break;

				case WindowMessage.VBA_EOF:
					try
					{
						LogInvokers.LogInvoker[Instance._invoker](hwnd, "", "", null);
					}
					catch
					{
						// todo handle exception
						break;
					}

					break;

				default:
					return IntPtr.Zero;
			}

			return IntPtr.Zero;

		}
	} 
}