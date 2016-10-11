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

namespace vba_debug_client
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private HwndSource hwndSource;

		public static MainWindow instance;
		public MainWindow()
		{
			InitializeComponent();
			instance = this;

		}


		/// <summary>
		/// AddHook Handle WndProc messages in WPF
		/// This cannot be done in a Window's constructor as a handle window handle won't at that point, so there won't be a HwndSource.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			hwndSource = PresentationSource.FromVisual(this) as HwndSource;
			if (hwndSource != null)
			{
				hwndSource.AddHook(WndProc);
				description.Text = "Handle: " + hwndSource.Handle.ToString("X") + "\t";
			}
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
			if (instance.Logging && (WindowMessage)msg == WindowMessage.WM_COPYDATA)
			{
				var message = "";
				var subCode = "";
				COPYDATASTRUCT cds = new COPYDATASTRUCT();
				var t = cds.GetType();
				cds = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, t);
				string messageContent = Marshal.PtrToStringAnsi(cds.lpData, cds.cbData); ;
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

				instance.logMessage(hwnd, message, subCode, messageContent);

			}

			return IntPtr.Zero;
		}

		private void logMessage(IntPtr hwnd, string message, string code, string p2)
		{
			logBox.Text = logBox.Text + hwndSource.Handle.ToString("X") + "\t" + hwnd.ToString("X") + "\t" + message + "\t" + code + "\t" + p2 +"\n";
			logBox.ScrollToEnd();
		}

		private Boolean Logging = false;

		private void pause_Click(object sender, RoutedEventArgs e)
		{
			Logging = !Logging;
			pause.Content = Logging ? "Pause" : "Log";
		}

		private void clear_Click(object sender, RoutedEventArgs e)
		{
			logBox.Text = "";
		}

	}
}