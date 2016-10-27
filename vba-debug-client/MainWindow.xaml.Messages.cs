using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace vba_debug_client
{
	public class WmCopyData
	{
		[DebuggerDisplay("[the string is {lpData}]")]
		struct COPYDATASTRUCT
		{
			public int dwData;
			public int cbData;
			public IntPtr lpData;
		}
		COPYDATASTRUCT cds;

		public string ToString(IntPtr lParam)
		{
			cds = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, typeof(COPYDATASTRUCT));
			return cds.lpData == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(cds.lpData, cds.cbData);
		}

		public WmCopyData()
		{
			cds = new COPYDATASTRUCT();
		}

	}

	public class TestTypeOf
	{
		public struct Teststruct
		{
			public int d1;
			public int d2;
		}
		Teststruct cds;

		public Teststruct ToString (IntPtr lParam)
		{
			var t = typeof(Teststruct);	// todo cannot resolve symbol 'cds'
			cds.d1 = 1;
			cds.d2 = 2;
			return cds;
		}

		public TestTypeOf ()
		{
			cds = new Teststruct();
		}

	}

	public enum ELogger : uint
	{
		NewAction,
		CastAction,
		NewLog,
		InstanceLog,
		CastLog,
		InvokeAsync,
		Delegate,
		NoOpp
	}

	/// <summary>
	/// Provides a mapping between VBA_PRINT message wParam and logger invokation methods
	/// </summary>
	public static class LogInvokers
	{
		public delegate void Log (IntPtr hwnd, IntPtr sender, string p);

		public static Log Logger;

		public static Dispatcher OwnDispatcher;

		private const DispatcherPriority Priority = DispatcherPriority.ApplicationIdle;

		/// <summary>
		/// An invoker manifold for Log delegates
		/// </summary>
		public static Dictionary<ELogger, Log> LogInvoker = new Dictionary<ELogger, Log>
		{
			{
				ELogger.NewAction, (Log) ((hwnd, sender, messageContent) =>
				{
					OwnDispatcher.BeginInvoke(
						new Action(() =>Logger(hwnd, sender, messageContent)),
						Priority
					);
				})
			},
			{
				ELogger.CastAction, (Log) ((hwnd, sender, messageContent) =>
				{
					OwnDispatcher.BeginInvoke(
						method: (Action)(() =>Logger(hwnd, sender, messageContent)),
						priority: Priority
					);
				})
			},
			{
				ELogger.NewLog, (Log) ((hwnd, sender, messageContent) =>
				{
					//requires
					//	private delegate void Log(IntPtr hwnd, string string sender, string p);
					//in owner class
					OwnDispatcher.BeginInvoke(
						new Log(Logger),
						args: new object[] { hwnd, sender, messageContent },
						priority: Priority
					);
				})
			},
			{
				ELogger.InstanceLog, (Log) ((hwnd, sender, messageContent) =>
				{
					//requires
					//	private readonly Log _pLog;	// in owner class
					//and in owner class constructor	
					//	Instance = this;
					//	_pLog = new Log(Logger);
					OwnDispatcher.BeginInvoke(
						Logger, args: new object[] { hwnd, sender, messageContent },
						priority: Priority
					);
				})
			},
			{
				ELogger.CastLog, (Log) ((hwnd, sender, messageContent) =>
				{
					//requires
					//	private delegate void Log(IntPtr hwnd, string string sender, string p);
					//in owner class
					OwnDispatcher.BeginInvoke(
						(Log)(Logger), args: new object[] { hwnd, sender, messageContent },
						priority: Priority
					);
				})
			},
			{
				ELogger.InvokeAsync, (Log) ((hwnd, sender, messageContent) =>
				{
					OwnDispatcher.InvokeAsync(
						() =>Logger(hwnd, sender, messageContent),
						Priority
					);
				})
			},
			//{
			//	ELogger.Delegate, (Log) ((hwnd, sender, messageContent) =>
			//	{
			//		OwnDispatcher.BeginInvoke(
			//			delegate(() => {Logger(hwnd, sender, messageContent); }),
			//			priority: priority
			//		);
			//	})
			//},
			{
				ELogger.NoOpp, (Log) ((hwnd, sender, messageContent) => {})
			},
		};
	}

	public static class Messages
	{
		/// <summary>
		/// From http://wiki.winehq.org/List_Of_Windows_Messages
		/// </summary>
		[Flags]
		public enum WindowMessage : uint
		{
			WM_NULL = 0x0,
			WM_CREATE = 0x0001,
			WM_DESTROY = 0x0002,
			WM_MOVE = 0x0003,
			WM_SIZE = 0x0005,
			WM_ACTIVATE = 0x0006,
			WM_SETFOCUS = 0x0007,
			WM_KILLFOCUS = 0x0008,
			WM_ENABLE = 0x000a,
			WM_SETREDRAW = 0x000b,
			WM_SETTEXT = 0x000c,
			WM_GETTEXT = 0x000d,
			WM_GETTEXTLENGTH = 0x000e,
			WM_PAINT = 0x000f,
			WM_CLOSE = 0x0010,
			WM_QUERYENDSESSION = 0x0011,
			WM_QUIT = 0x0012,
			WM_QUERYOPEN = 0x0013,
			WM_ERASEBKGND = 0x0014,
			WM_SYSCOLORCHANGE = 0x0015,
			WM_ENDSESSION = 0x0016,
			WM_SHOWWINDOW = 0x0018,
			WM_CTLCOLOR = 0x0019,
			WM_WININICHANGE = 0x001a,
			WM_DEVMODECHANGE = 0x001b,
			WM_ACTIVATEAPP = 0x001c,
			WM_FONTCHANGE = 0x001d,
			WM_TIMECHANGE = 0x001e,
			WM_CANCELMODE = 0x001f,
			WM_SETCURSOR = 0x0020,
			WM_MOUSEACTIVATE = 0x0021,
			WM_CHILDACTIVATE = 0x0022,
			WM_QUEUESYNC = 0x0023,
			WM_GETMINMAXINFO = 0x0024,
			WM_PAINTICON = 0x0026,
			WM_ICONERASEBKGND = 0x0027,
			WM_NEXTDLGCTL = 0x0028,
			WM_SPOOLERSTATUS = 0x002a,
			WM_DRAWITEM = 0x002b,
			WM_MEASUREITEM = 0x002c,
			WM_DELETEITEM = 0x002d,
			WM_VKEYTOITEM = 0x002e,
			WM_CHARTOITEM = 0x002f,
			WM_SETFONT = 0x0030,
			WM_GETFONT = 0x0031,
			WM_SETHOTKEY = 0x0032,
			WM_GETHOTKEY = 0x0033,
			WM_QUERYDRAGICON = 0x0037,
			WM_COMPAREITEM = 0x0039,
			WM_GETOBJECT = 0x003d,
			WM_COMPACTING = 0x0041,
			WM_COMMNOTIFY = 0x0044,
			WM_WINDOWPOSCHANGING = 0x0046,
			WM_WINDOWPOSCHANGED = 0x0047,
			WM_POWER = 0x0048,
			WM_COPYGLOBALDATA = 0x0049,
			WM_COPYDATA = 0x004a,
			WM_CANCELJOURNAL = 0x004b,
			WM_NOTIFY = 0x004e,
			WM_INPUTLANGCHANGEREQUEST = 0x0050,
			WM_INPUTLANGCHANGE = 0x0051,
			WM_TCARD = 0x0052,
			WM_HELP = 0x0053,
			WM_USERCHANGED = 0x0054,
			WM_NOTIFYFORMAT = 0x0055,
			WM_CONTEXTMENU = 0x007b,
			WM_STYLECHANGING = 0x007c,
			WM_STYLECHANGED = 0x007d,
			WM_DISPLAYCHANGE = 0x007e,
			WM_GETICON = 0x007f,
			WM_SETICON = 0x0080,
			WM_NCCREATE = 0x0081,
			WM_NCDESTROY = 0x0082,
			WM_NCCALCSIZE = 0x0083,
			WM_NCHITTEST = 0x0084,
			WM_NCPAINT = 0x0085,
			WM_NCACTIVATE = 0x0086,
			WM_GETDLGCODE = 0x0087,
			WM_SYNCPAINT = 0x0088,
			WM_NCMOUSEMOVE = 0x00a0,
			WM_NCLBUTTONDOWN = 0x00a1,
			WM_NCLBUTTONUP = 0x00a2,
			WM_NCLBUTTONDBLCLK = 0x00a3,
			WM_NCRBUTTONDOWN = 0x00a4,
			WM_NCRBUTTONUP = 0x00a5,
			WM_NCRBUTTONDBLCLK = 0x00a6,
			WM_NCMBUTTONDOWN = 0x00a7,
			WM_NCMBUTTONUP = 0x00a8,
			WM_NCMBUTTONDBLCLK = 0x00a9,
			WM_NCXBUTTONDOWN = 0x00ab,
			WM_NCXBUTTONUP = 0x00ac,
			WM_NCXBUTTONDBLCLK = 0x00ad,
			SBM_SETPOS = 0x00e0,
			SBM_GETPOS = 0x00e1,
			SBM_SETRANGE = 0x00e2,
			SBM_GETRANGE = 0x00e3,
			SBM_ENABLE_ARROWS = 0x00e4,
			SBM_SETRANGEREDRAW = 0x00e6,
			SBM_SETSCROLLINFO = 0x00e9,
			SBM_GETSCROLLINFO = 0x00ea,
			SBM_GETSCROLLBARINFO = 0x00eb,
			WM_INPUT = 0x00ff,
			WM_KEYDOWN = 0x0100,
			WM_KEYUP = 0x0101,
			WM_CHAR = 0x0102,
			WM_DEADCHAR = 0x0103,
			WM_SYSKEYDOWN = 0x0104,
			WM_SYSKEYUP = 0x0105,
			WM_SYSCHAR = 0x0106,
			WM_SYSDEADCHAR = 0x0107,
			WM_KEYLAST = 0x0108,
			WM_WNT_CONVERTREQUESTEX = 0x0109,
			WM_CONVERTREQUEST = 0x010a,
			WM_CONVERTRESULT = 0x010b,
			WM_INTERIM = 0x010c,
			WM_IME_STARTCOMPOSITION = 0x010d,
			WM_IME_ENDCOMPOSITION = 0x010e,
			WM_IME_COMPOSITION = 0x010f,
			WM_INITDIALOG = 0x0110,
			WM_COMMAND = 0x0111,
			WM_SYSCOMMAND = 0x0112,
			WM_TIMER = 0x0113,
			WM_HSCROLL = 0x0114,
			WM_VSCROLL = 0x0115,
			WM_INITMENU = 0x0116,
			WM_INITMENUPOPUP = 0x0117,
			WM_SYSTIMER = 0x0118,
			WM_MENUSELECT = 0x011f,
			WM_MENUCHAR = 0x0120,
			WM_ENTERIDLE = 0x0121,
			WM_MENURBUTTONUP = 0x0122,
			WM_MENUDRAG = 0x0123,
			WM_MENUGETOBJECT = 0x0124,
			WM_UNINITMENUPOPUP = 0x0125,
			WM_MENUCOMMAND = 0x0126,
			WM_CHANGEUISTATE = 0x0127,
			WM_UPDATEUISTATE = 0x0128,
			WM_QUERYUISTATE = 0x0129,
			WM_CTLCOLORMSGBOX = 0x0132,
			WM_CTLCOLOREDIT = 0x0133,
			WM_CTLCOLORLISTBOX = 0x0134,
			WM_CTLCOLORBTN = 0x0135,
			WM_CTLCOLORDLG = 0x0136,
			WM_CTLCOLORSCROLLBAR = 0x0137,
			WM_CTLCOLORSTATIC = 0x0138,
			WM_MOUSEMOVE = 0x0200,
			WM_LBUTTONDOWN = 0x0201,
			WM_LBUTTONUP = 0x0202,
			WM_LBUTTONDBLCLK = 0x0203,
			WM_RBUTTONDOWN = 0x0204,
			WM_RBUTTONUP = 0x0205,
			WM_RBUTTONDBLCLK = 0x0206,
			WM_MBUTTONDOWN = 0x0207,
			WM_MBUTTONUP = 0x0208,
			WM_MBUTTONDBLCLK = 0x0209,
			WM_MOUSEWHEEL = 0x020a,
			WM_XBUTTONDOWN = 0x020b,
			WM_XBUTTONUP = 0x020c,
			WM_XBUTTONDBLCLK = 0x020d,
			WM_PARENTNOTIFY = 0x0210,
			WM_ENTERMENULOOP = 0x0211,
			WM_EXITMENULOOP = 0x0212,
			WM_NEXTMENU = 0x0213,
			WM_SIZING = 0x0214,
			WM_CAPTURECHANGED = 0x0215,
			WM_MOVING = 0x0216,
			WM_POWERBROADCAST = 0x0218,
			WM_DEVICECHANGE = 0x0219,
			WM_MDICREATE = 0x0220,
			WM_MDIDESTROY = 0x0221,
			WM_MDIACTIVATE = 0x0222,
			WM_MDIRESTORE = 0x0223,
			WM_MDINEXT = 0x0224,
			WM_MDIMAXIMIZE = 0x0225,
			WM_MDITILE = 0x0226,
			WM_MDICASCADE = 0x0227,
			WM_MDIICONARRANGE = 0x0228,
			WM_MDIGETACTIVE = 0x0229,
			WM_MDISETMENU = 0x0230,
			WM_ENTERSIZEMOVE = 0x0231,
			WM_EXITSIZEMOVE = 0x0232,
			WM_DROPFILES = 0x0233,
			WM_MDIREFRESHMENU = 0x0234,
			WM_IME_REPORT = 0x0280,
			WM_IME_SETCONTEXT = 0x0281,
			WM_IME_NOTIFY = 0x0282,
			WM_IME_CONTROL = 0x0283,
			WM_IME_COMPOSITIONFULL = 0x0284,
			WM_IME_SELECT = 0x0285,
			WM_IME_CHAR = 0x0286,
			WM_IME_REQUEST = 0x0288,
			WM_IMEKEYDOWN = 0x0290,
			WM_IMEKEYUP = 0x0291,
			WM_NCMOUSEHOVER = 0x02a0,
			WM_MOUSEHOVER = 0x02a1,
			WM_NCMOUSELEAVE = 0x02a2,
			WM_MOUSELEAVE = 0x02a3,
			WM_CUT = 0x0300,
			WM_COPY = 0x0301,
			WM_PASTE = 0x0302,
			WM_CLEAR = 0x0303,
			WM_UNDO = 0x0304,
			WM_RENDERFORMAT = 0x0305,
			WM_RENDERALLFORMATS = 0x0306,
			WM_DESTROYCLIPBOARD = 0x0307,
			WM_DRAWCLIPBOARD = 0x0308,
			WM_PAINTCLIPBOARD = 0x0309,
			WM_VSCROLLCLIPBOARD = 0x030a,
			WM_SIZECLIPBOARD = 0x030b,
			WM_ASKCBFORMATNAME = 0x030c,
			WM_CHANGECBCHAIN = 0x030d,
			WM_HSCROLLCLIPBOARD = 0x030e,
			WM_QUERYNEWPALETTE = 0x030f,
			WM_PALETTEISCHANGING = 0x0310,
			WM_PALETTECHANGED = 0x0311,
			WM_HOTKEY = 0x0312,
			WM_PRINT = 0x0317,
			WM_PRINTCLIENT = 0x0318,
			WM_APPCOMMAND = 0x0319,
			WM_HANDHELDFIRST = 0x0358,
			WM_HANDHELDLAST = 0x035f,
			WM_AFXFIRST = 0x0360,
			WM_AFXLAST = 0x037f,
			WM_PENWINFIRST = 0x0380,
			WM_RCRESULT = 0x0381,
			WM_HOOKRCRESULT = 0x0382,
			WM_GLOBALRCCHANGE = 0x0383,
			WM_SKB = 0x0384,
			WM_HEDITCTL = 0x0385,
			WM_PENMISC = 0x0386,
			WM_CTLINIT = 0x0387,
			WM_PENEVENT = 0x0388,
			WM_PENWINLAST = 0x038f,
			DDM_SETFMT = 0x0400,
			VBA_PRINT = 0x401,
			VBA_EOF = 0x402,
			VBA_LOGGING = 0x403,
			VBA_CLEAR = 0x404
		}

		/// <summary>
		/// From https://msdn.microsoft.com/en-us/library/windows/desktop/aa372716(v=vs.85).aspx
		/// </summary>
		[Flags]
		public enum WindowMessageParameter : uint
		{
			PBT_APMQUERYSUSPEND = 0x0,
			PBT_APMBATTERYLOW = 0x9, // Notifies applications that the battery power is low.
			PBT_APMOEMEVENT = 0xb, // Notifies applications that the APM BIOS has signalled  an APM OEM event.
			PBT_APMQUERYSTANDBY = 0x0001, // 
			PBT_APMPOWERSTATUSCHANGE = 0xa, // Notifies applications of a change in the power status of the computer, such as a switch from battery power to A/C. The system also broadcasts this event when remaining battery power slips below the threshold specified by the user or if the battery power changes by a specified percentage.
			PBT_APMQUERYSUSPENDFAILED = 0x218, // Notifies applications that permission to suspend the computer was denied.
			PBT_APMRESUMEAUTOMATIC = 0x12, // Notifies applications that the system is resuming from sleep or hibernation. If the system detects any user activity after broadcasting PBT_APMRESUMEAUTOMATIC, it will broadcast a PBT_APMRESUMESUSPEND event to let applications know they can resume full interaction with the user.
			PBT_APMRESUMECRITICAL = 0x6, // Notifies applications that the system has resumed operation.
			PBT_APMRESUMESUSPEND = 0x7, // Notifies applications that the system has resumed operation after being suspended.
			PBT_APMSUSPEND = 0x4, // Notifies applications that the computer is about to enter a suspended state. 
			PBT_POWERSETTINGCHANGE = 0x8013, // Notifies applications that a power setting change event occurred.
			WM_POWER = 0x48, // Notifies applications that the system, typically a battery-powered personal computer, is about to enter a suspended mode.
			WM_POWERBROADCAST = 0x218, // Notifies applications that a power-management event has occurred.
			BROADCAST_QUERY_DENY = 0x424D5144 //
		}

		public static readonly Dictionary<WindowMessageParameter, string> parameters = new Dictionary<WindowMessageParameter, string>
		{
			{ WindowMessageParameter.PBT_APMQUERYSUSPEND, "PBT_APMQUERYSUSPEND" },
			{ WindowMessageParameter.PBT_APMBATTERYLOW, "PBT_APMBATTERYLOW" },
			{ WindowMessageParameter.PBT_APMOEMEVENT, "PBT_APMOEMEVENT" },
			{ WindowMessageParameter.PBT_APMQUERYSTANDBY, "PBT_APMQUERYSTANDBY" },
			{ WindowMessageParameter.PBT_APMPOWERSTATUSCHANGE, "PBT_APMPOWERSTATUSCHANGE" },
			{ WindowMessageParameter.PBT_APMRESUMEAUTOMATIC, "PBT_APMRESUMEAUTOMATIC" },
			{ WindowMessageParameter.PBT_APMRESUMECRITICAL, "PBT_APMRESUMECRITICAL" },
			{ WindowMessageParameter.PBT_APMRESUMESUSPEND, "PBT_APMRESUMESUSPEND" },
			{ WindowMessageParameter.PBT_APMSUSPEND, "PBT_APMSUSPEND" },
			{ WindowMessageParameter.PBT_POWERSETTINGCHANGE, "PBT_POWERSETTINGCHANGE" },
			{ WindowMessageParameter.WM_POWER, "WM_POWER" },
			{ WindowMessageParameter.WM_POWERBROADCAST, "WM_POWERBROADCAST" },
			{ WindowMessageParameter.BROADCAST_QUERY_DENY, "BROADCAST_QUERY_DENY" }
		};

		public static readonly Dictionary<WindowMessage, string> names = new Dictionary<WindowMessage, string>
		{        
			{ WindowMessage.WM_NULL, "WM_NULL" },
			{ WindowMessage.WM_CREATE, "WM_CREATE" },
			{ WindowMessage.WM_DESTROY, "WM_DESTROY" },
			{ WindowMessage.WM_MOVE, "WM_MOVE" },
			{ WindowMessage.WM_SIZE, "WM_SIZE" },
			{ WindowMessage.WM_ACTIVATE, "WM_ACTIVATE" },
			{ WindowMessage.WM_SETFOCUS, "WM_SETFOCUS" },
			{ WindowMessage.WM_KILLFOCUS, "WM_KILLFOCUS" },
			{ WindowMessage.WM_ENABLE, "WM_ENABLE" },
			{ WindowMessage.WM_SETREDRAW, "WM_SETREDRAW" },
			{ WindowMessage.WM_SETTEXT, "WM_SETTEXT" },
			{ WindowMessage.WM_GETTEXT, "WM_GETTEXT" },
			{ WindowMessage.WM_GETTEXTLENGTH, "WM_GETTEXTLENGTH" },
			{ WindowMessage.WM_PAINT, "WM_PAINT" },
			{ WindowMessage.WM_CLOSE, "WM_CLOSE" },
			{ WindowMessage.WM_QUERYENDSESSION, "WM_QUERYENDSESSION" },
			{ WindowMessage.WM_QUIT, "WM_QUIT" },
			{ WindowMessage.WM_QUERYOPEN, "WM_QUERYOPEN" },
			{ WindowMessage.WM_ERASEBKGND, "WM_ERASEBKGND" },
			{ WindowMessage.WM_SYSCOLORCHANGE, "WM_SYSCOLORCHANGE" },
			{ WindowMessage.WM_ENDSESSION, "WM_ENDSESSION" },
			{ WindowMessage.WM_SHOWWINDOW, "WM_SHOWWINDOW" },
			{ WindowMessage.WM_CTLCOLOR, "WM_CTLCOLOR" },
			{ WindowMessage.WM_WININICHANGE, "WM_WININICHANGE" },
			{ WindowMessage.WM_DEVMODECHANGE, "WM_DEVMODECHANGE" },
			{ WindowMessage.WM_ACTIVATEAPP, "WM_ACTIVATEAPP" },
			{ WindowMessage.WM_FONTCHANGE, "WM_FONTCHANGE" },
			{ WindowMessage.WM_TIMECHANGE, "WM_TIMECHANGE" },
			{ WindowMessage.WM_CANCELMODE, "WM_CANCELMODE" },
			{ WindowMessage.WM_SETCURSOR, "WM_SETCURSOR" },
			{ WindowMessage.WM_MOUSEACTIVATE, "WM_MOUSEACTIVATE" },
			{ WindowMessage.WM_CHILDACTIVATE, "WM_CHILDACTIVATE" },
			{ WindowMessage.WM_QUEUESYNC, "WM_QUEUESYNC" },
			{ WindowMessage.WM_GETMINMAXINFO, "WM_GETMINMAXINFO" },
			{ WindowMessage.WM_PAINTICON, "WM_PAINTICON" },
			{ WindowMessage.WM_ICONERASEBKGND, "WM_ICONERASEBKGND" },
			{ WindowMessage.WM_NEXTDLGCTL, "WM_NEXTDLGCTL" },
			{ WindowMessage.WM_SPOOLERSTATUS, "WM_SPOOLERSTATUS" },
			{ WindowMessage.WM_DRAWITEM, "WM_DRAWITEM" },
			{ WindowMessage.WM_MEASUREITEM, "WM_MEASUREITEM" },
			{ WindowMessage.WM_DELETEITEM, "WM_DELETEITEM" },
			{ WindowMessage.WM_VKEYTOITEM, "WM_VKEYTOITEM" },
			{ WindowMessage.WM_CHARTOITEM, "WM_CHARTOITEM" },
			{ WindowMessage.WM_SETFONT, "WM_SETFONT" },
			{ WindowMessage.WM_GETFONT, "WM_GETFONT" },
			{ WindowMessage.WM_SETHOTKEY, "WM_SETHOTKEY" },
			{ WindowMessage.WM_GETHOTKEY, "WM_GETHOTKEY" },
			{ WindowMessage.WM_QUERYDRAGICON, "WM_QUERYDRAGICON" },
			{ WindowMessage.WM_COMPAREITEM, "WM_COMPAREITEM" },
			{ WindowMessage.WM_GETOBJECT, "WM_GETOBJECT" },
			{ WindowMessage.WM_COMPACTING, "WM_COMPACTING" },
			{ WindowMessage.WM_COMMNOTIFY, "WM_COMMNOTIFY" },
			{ WindowMessage.WM_WINDOWPOSCHANGING, "WM_WINDOWPOSCHANGING" },
			{ WindowMessage.WM_WINDOWPOSCHANGED, "WM_WINDOWPOSCHANGED" },
			{ WindowMessage.WM_POWER, "WM_POWER" },
			{ WindowMessage.WM_COPYGLOBALDATA, "WM_COPYGLOBALDATA" },
			{ WindowMessage.WM_COPYDATA, "WM_COPYDATA" },
			{ WindowMessage.WM_CANCELJOURNAL, "WM_CANCELJOURNAL" },
			{ WindowMessage.WM_NOTIFY, "WM_NOTIFY" },
			{ WindowMessage.WM_INPUTLANGCHANGEREQUEST, "WM_INPUTLANGCHANGEREQUEST" },
			{ WindowMessage.WM_INPUTLANGCHANGE, "WM_INPUTLANGCHANGE" },
			{ WindowMessage.WM_TCARD, "WM_TCARD" },
			{ WindowMessage.WM_HELP, "WM_HELP" },
			{ WindowMessage.WM_USERCHANGED, "WM_USERCHANGED" },
			{ WindowMessage.WM_NOTIFYFORMAT, "WM_NOTIFYFORMAT" },
			{ WindowMessage.WM_CONTEXTMENU, "WM_CONTEXTMENU" },
			{ WindowMessage.WM_STYLECHANGING, "WM_STYLECHANGING" },
			{ WindowMessage.WM_STYLECHANGED, "WM_STYLECHANGED" },
			{ WindowMessage.WM_DISPLAYCHANGE, "WM_DISPLAYCHANGE" },
			{ WindowMessage.WM_GETICON, "WM_GETICON" },
			{ WindowMessage.WM_SETICON, "WM_SETICON" },
			{ WindowMessage.WM_NCCREATE, "WM_NCCREATE" },
			{ WindowMessage.WM_NCDESTROY, "WM_NCDESTROY" },
			{ WindowMessage.WM_NCCALCSIZE, "WM_NCCALCSIZE" },
			{ WindowMessage.WM_NCHITTEST, "WM_NCHITTEST" },
			{ WindowMessage.WM_NCPAINT, "WM_NCPAINT" },
			{ WindowMessage.WM_NCACTIVATE, "WM_NCACTIVATE" },
			{ WindowMessage.WM_GETDLGCODE, "WM_GETDLGCODE" },
			{ WindowMessage.WM_SYNCPAINT, "WM_SYNCPAINT" },
			{ WindowMessage.WM_NCMOUSEMOVE, "WM_NCMOUSEMOVE" },
			{ WindowMessage.WM_NCLBUTTONDOWN, "WM_NCLBUTTONDOWN" },
			{ WindowMessage.WM_NCLBUTTONUP, "WM_NCLBUTTONUP" },
			{ WindowMessage.WM_NCLBUTTONDBLCLK, "WM_NCLBUTTONDBLCLK" },
			{ WindowMessage.WM_NCRBUTTONDOWN, "WM_NCRBUTTONDOWN" },
			{ WindowMessage.WM_NCRBUTTONUP, "WM_NCRBUTTONUP" },
			{ WindowMessage.WM_NCRBUTTONDBLCLK, "WM_NCRBUTTONDBLCLK" },
			{ WindowMessage.WM_NCMBUTTONDOWN, "WM_NCMBUTTONDOWN" },
			{ WindowMessage.WM_NCMBUTTONUP, "WM_NCMBUTTONUP" },
			{ WindowMessage.WM_NCMBUTTONDBLCLK, "WM_NCMBUTTONDBLCLK" },
			{ WindowMessage.WM_NCXBUTTONDOWN, "WM_NCXBUTTONDOWN" },
			{ WindowMessage.WM_NCXBUTTONUP, "WM_NCXBUTTONUP" },
			{ WindowMessage.WM_NCXBUTTONDBLCLK, "WM_NCXBUTTONDBLCLK" },
			{ WindowMessage.SBM_SETPOS, "SBM_SETPOS" },
			{ WindowMessage.SBM_GETPOS, "SBM_GETPOS" },
			{ WindowMessage.SBM_SETRANGE, "SBM_SETRANGE" },
			{ WindowMessage.SBM_GETRANGE, "SBM_GETRANGE" },
			{ WindowMessage.SBM_ENABLE_ARROWS, "SBM_ENABLE_ARROWS" },
			{ WindowMessage.SBM_SETRANGEREDRAW, "SBM_SETRANGEREDRAW" },
			{ WindowMessage.SBM_SETSCROLLINFO, "SBM_SETSCROLLINFO" },
			{ WindowMessage.SBM_GETSCROLLINFO, "SBM_GETSCROLLINFO" },
			{ WindowMessage.SBM_GETSCROLLBARINFO, "SBM_GETSCROLLBARINFO" },
			{ WindowMessage.WM_INPUT, "WM_INPUT" },
			{ WindowMessage.WM_KEYDOWN, "WM_KEYDOWN" },
			{ WindowMessage.WM_KEYUP, "WM_KEYUP" },
			{ WindowMessage.WM_CHAR, "WM_CHAR" },
			{ WindowMessage.WM_DEADCHAR, "WM_DEADCHAR" },
			{ WindowMessage.WM_SYSKEYDOWN, "WM_SYSKEYDOWN" },
			{ WindowMessage.WM_SYSKEYUP, "WM_SYSKEYUP" },
			{ WindowMessage.WM_SYSCHAR, "WM_SYSCHAR" },
			{ WindowMessage.WM_SYSDEADCHAR, "WM_SYSDEADCHAR" },
			{ WindowMessage.WM_KEYLAST, "WM_KEYLAST" },
			{ WindowMessage.WM_WNT_CONVERTREQUESTEX, "WM_WNT_CONVERTREQUESTEX" },
			{ WindowMessage.WM_CONVERTREQUEST, "WM_CONVERTREQUEST" },
			{ WindowMessage.WM_CONVERTRESULT, "WM_CONVERTRESULT" },
			{ WindowMessage.WM_INTERIM, "WM_INTERIM" },
			{ WindowMessage.WM_IME_STARTCOMPOSITION, "WM_IME_STARTCOMPOSITION" },
			{ WindowMessage.WM_IME_ENDCOMPOSITION, "WM_IME_ENDCOMPOSITION" },
			{ WindowMessage.WM_IME_COMPOSITION, "WM_IME_COMPOSITION" },
			{ WindowMessage.WM_INITDIALOG, "WM_INITDIALOG" },
			{ WindowMessage.WM_COMMAND, "WM_COMMAND" },
			{ WindowMessage.WM_SYSCOMMAND, "WM_SYSCOMMAND" },
			{ WindowMessage.WM_TIMER, "WM_TIMER" },
			{ WindowMessage.WM_HSCROLL, "WM_HSCROLL" },
			{ WindowMessage.WM_VSCROLL, "WM_VSCROLL" },
			{ WindowMessage.WM_INITMENU, "WM_INITMENU" },
			{ WindowMessage.WM_INITMENUPOPUP, "WM_INITMENUPOPUP" },
			{ WindowMessage.WM_SYSTIMER, "WM_SYSTIMER" },
			{ WindowMessage.WM_MENUSELECT, "WM_MENUSELECT" },
			{ WindowMessage.WM_MENUCHAR, "WM_MENUCHAR" },
			{ WindowMessage.WM_ENTERIDLE, "WM_ENTERIDLE" },
			{ WindowMessage.WM_MENURBUTTONUP, "WM_MENURBUTTONUP" },
			{ WindowMessage.WM_MENUDRAG, "WM_MENUDRAG" },
			{ WindowMessage.WM_MENUGETOBJECT, "WM_MENUGETOBJECT" },
			{ WindowMessage.WM_UNINITMENUPOPUP, "WM_UNINITMENUPOPUP" },
			{ WindowMessage.WM_MENUCOMMAND, "WM_MENUCOMMAND" },
			{ WindowMessage.WM_CHANGEUISTATE, "WM_CHANGEUISTATE" },
			{ WindowMessage.WM_UPDATEUISTATE, "WM_UPDATEUISTATE" },
			{ WindowMessage.WM_QUERYUISTATE, "WM_QUERYUISTATE" },
			{ WindowMessage.WM_CTLCOLORMSGBOX, "WM_CTLCOLORMSGBOX" },
			{ WindowMessage.WM_CTLCOLOREDIT, "WM_CTLCOLOREDIT" },
			{ WindowMessage.WM_CTLCOLORLISTBOX, "WM_CTLCOLORLISTBOX" },
			{ WindowMessage.WM_CTLCOLORBTN, "WM_CTLCOLORBTN" },
			{ WindowMessage.WM_CTLCOLORDLG, "WM_CTLCOLORDLG" },
			{ WindowMessage.WM_CTLCOLORSCROLLBAR, "WM_CTLCOLORSCROLLBAR" },
			{ WindowMessage.WM_CTLCOLORSTATIC, "WM_CTLCOLORSTATIC" },
			{ WindowMessage.WM_MOUSEMOVE, "WM_MOUSEMOVE" },
			{ WindowMessage.WM_LBUTTONDOWN, "WM_LBUTTONDOWN" },
			{ WindowMessage.WM_LBUTTONUP, "WM_LBUTTONUP" },
			{ WindowMessage.WM_LBUTTONDBLCLK, "WM_LBUTTONDBLCLK" },
			{ WindowMessage.WM_RBUTTONDOWN, "WM_RBUTTONDOWN" },
			{ WindowMessage.WM_RBUTTONUP, "WM_RBUTTONUP" },
			{ WindowMessage.WM_RBUTTONDBLCLK, "WM_RBUTTONDBLCLK" },
			{ WindowMessage.WM_MBUTTONDOWN, "WM_MBUTTONDOWN" },
			{ WindowMessage.WM_MBUTTONUP, "WM_MBUTTONUP" },
			{ WindowMessage.WM_MBUTTONDBLCLK, "WM_MBUTTONDBLCLK" },
			{ WindowMessage.WM_MOUSEWHEEL, "WM_MOUSEWHEEL" },
			{ WindowMessage.WM_XBUTTONDOWN, "WM_XBUTTONDOWN" },
			{ WindowMessage.WM_XBUTTONUP, "WM_XBUTTONUP" },
			{ WindowMessage.WM_XBUTTONDBLCLK, "WM_XBUTTONDBLCLK" },
			{ WindowMessage.WM_PARENTNOTIFY, "WM_PARENTNOTIFY" },
			{ WindowMessage.WM_ENTERMENULOOP, "WM_ENTERMENULOOP" },
			{ WindowMessage.WM_EXITMENULOOP, "WM_EXITMENULOOP" },
			{ WindowMessage.WM_NEXTMENU, "WM_NEXTMENU" },
			{ WindowMessage.WM_SIZING, "WM_SIZING" },
			{ WindowMessage.WM_CAPTURECHANGED, "WM_CAPTURECHANGED" },
			{ WindowMessage.WM_MOVING, "WM_MOVING" },
			{ WindowMessage.WM_POWERBROADCAST, "WM_POWERBROADCAST" },
			{ WindowMessage.WM_DEVICECHANGE, "WM_DEVICECHANGE" },
			{ WindowMessage.WM_MDICREATE, "WM_MDICREATE" },
			{ WindowMessage.WM_MDIDESTROY, "WM_MDIDESTROY" },
			{ WindowMessage.WM_MDIACTIVATE, "WM_MDIACTIVATE" },
			{ WindowMessage.WM_MDIRESTORE, "WM_MDIRESTORE" },
			{ WindowMessage.WM_MDINEXT, "WM_MDINEXT" },
			{ WindowMessage.WM_MDIMAXIMIZE, "WM_MDIMAXIMIZE" },
			{ WindowMessage.WM_MDITILE, "WM_MDITILE" },
			{ WindowMessage.WM_MDICASCADE, "WM_MDICASCADE" },
			{ WindowMessage.WM_MDIICONARRANGE, "WM_MDIICONARRANGE" },
			{ WindowMessage.WM_MDIGETACTIVE, "WM_MDIGETACTIVE" },
			{ WindowMessage.WM_MDISETMENU, "WM_MDISETMENU" },
			{ WindowMessage.WM_ENTERSIZEMOVE, "WM_ENTERSIZEMOVE" },
			{ WindowMessage.WM_EXITSIZEMOVE, "WM_EXITSIZEMOVE" },
			{ WindowMessage.WM_DROPFILES, "WM_DROPFILES" },
			{ WindowMessage.WM_MDIREFRESHMENU, "WM_MDIREFRESHMENU" },
			{ WindowMessage.WM_IME_REPORT, "WM_IME_REPORT" },
			{ WindowMessage.WM_IME_SETCONTEXT, "WM_IME_SETCONTEXT" },
			{ WindowMessage.WM_IME_NOTIFY, "WM_IME_NOTIFY" },
			{ WindowMessage.WM_IME_CONTROL, "WM_IME_CONTROL" },
			{ WindowMessage.WM_IME_COMPOSITIONFULL, "WM_IME_COMPOSITIONFULL" },
			{ WindowMessage.WM_IME_SELECT, "WM_IME_SELECT" },
			{ WindowMessage.WM_IME_CHAR, "WM_IME_CHAR" },
			{ WindowMessage.WM_IME_REQUEST, "WM_IME_REQUEST" },
			{ WindowMessage.WM_IMEKEYDOWN, "WM_IMEKEYDOWN" },
			{ WindowMessage.WM_IMEKEYUP, "WM_IMEKEYUP" },
			{ WindowMessage.WM_NCMOUSEHOVER, "WM_NCMOUSEHOVER" },
			{ WindowMessage.WM_MOUSEHOVER, "WM_MOUSEHOVER" },
			{ WindowMessage.WM_NCMOUSELEAVE, "WM_NCMOUSELEAVE" },
			{ WindowMessage.WM_MOUSELEAVE, "WM_MOUSELEAVE" },
			{ WindowMessage.WM_CUT, "WM_CUT" },
			{ WindowMessage.WM_COPY, "WM_COPY" },
			{ WindowMessage.WM_PASTE, "WM_PASTE" },
			{ WindowMessage.WM_CLEAR, "WM_CLEAR" },
			{ WindowMessage.WM_UNDO, "WM_UNDO" },
			{ WindowMessage.WM_RENDERFORMAT, "WM_RENDERFORMAT" },
			{ WindowMessage.WM_RENDERALLFORMATS, "WM_RENDERALLFORMATS" },
			{ WindowMessage.WM_DESTROYCLIPBOARD, "WM_DESTROYCLIPBOARD" },
			{ WindowMessage.WM_DRAWCLIPBOARD, "WM_DRAWCLIPBOARD" },
			{ WindowMessage.WM_PAINTCLIPBOARD, "WM_PAINTCLIPBOARD" },
			{ WindowMessage.WM_VSCROLLCLIPBOARD, "WM_VSCROLLCLIPBOARD" },
			{ WindowMessage.WM_SIZECLIPBOARD, "WM_SIZECLIPBOARD" },
			{ WindowMessage.WM_ASKCBFORMATNAME, "WM_ASKCBFORMATNAME" },
			{ WindowMessage.WM_CHANGECBCHAIN, "WM_CHANGECBCHAIN" },
			{ WindowMessage.WM_HSCROLLCLIPBOARD, "WM_HSCROLLCLIPBOARD" },
			{ WindowMessage.WM_QUERYNEWPALETTE, "WM_QUERYNEWPALETTE" },
			{ WindowMessage.WM_PALETTEISCHANGING, "WM_PALETTEISCHANGING" },
			{ WindowMessage.WM_PALETTECHANGED, "WM_PALETTECHANGED" },
			{ WindowMessage.WM_HOTKEY, "WM_HOTKEY" },
			{ WindowMessage.WM_PRINT, "WM_PRINT" },
			{ WindowMessage.WM_PRINTCLIENT, "WM_PRINTCLIENT" },
			{ WindowMessage.WM_APPCOMMAND, "WM_APPCOMMAND" },
			{ WindowMessage.WM_HANDHELDFIRST, "WM_HANDHELDFIRST" },
			{ WindowMessage.WM_HANDHELDLAST, "WM_HANDHELDLAST" },
			{ WindowMessage.WM_AFXFIRST, "WM_AFXFIRST" },
			{ WindowMessage.WM_AFXLAST, "WM_AFXLAST" },
			{ WindowMessage.WM_PENWINFIRST, "WM_PENWINFIRST" },
			{ WindowMessage.WM_RCRESULT, "WM_RCRESULT" },
			{ WindowMessage.WM_HOOKRCRESULT, "WM_HOOKRCRESULT" },
			{ WindowMessage.WM_GLOBALRCCHANGE, "WM_GLOBALRCCHANGE" },
			{ WindowMessage.WM_SKB, "WM_SKB" },
			{ WindowMessage.WM_HEDITCTL, "WM_HEDITCTL" },
			{ WindowMessage.WM_PENMISC, "WM_PENMISC" },
			{ WindowMessage.WM_CTLINIT, "WM_CTLINIT" },
			{ WindowMessage.WM_PENEVENT, "WM_PENEVENT" },
			{ WindowMessage.WM_PENWINLAST, "WM_PENWINLAST" },
			{ WindowMessage.DDM_SETFMT, "DDM_SETFMT" },
			{ WindowMessage.VBA_PRINT, "VBA_PRINT" },
		};
	}
}


