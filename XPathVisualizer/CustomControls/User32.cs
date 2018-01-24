// User32.cs
// ------------------------------------------------------------------
//
// Wrapper for User32.dll methods, etc.
//
// Time-stamp: <2010-April-23 18:49:20>
// ------------------------------------------------------------------
//
// Copyright (c) 2010 by Dino Chiesa
// All rights reserved!
//
// ------------------------------------------------------------------

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Ionic
{
    public static class User32
    {
        public enum Styles : uint
        {
            WS_OVERLAPPED =        0x00000000,
            WS_POPUP =             0x80000000,
            WS_CHILD =             0x40000000,
            WS_MINIMIZE =          0x20000000,
            WS_VISIBLE =           0x10000000,
            WS_DISABLED =          0x08000000,
            WS_CLIPSIBLINGS =      0x04000000,
            WS_CLIPCHILDREN =      0x02000000,
            WS_MAXIMIZE =          0x01000000,
            WS_CAPTION =           0x00C00000,
            WS_BORDER =            0x00800000,
            WS_DLGFRAME =          0x00400000,
            WS_VSCROLL =           0x00200000,
            WS_HSCROLL =           0x00100000,
            WS_SYSMENU =           0x00080000,
            WS_THICKFRAME =        0x00040000,
            WS_GROUP =             0x00020000,
            WS_TABSTOP =           0x00010000,
            GWL_STYLE = 0xFFFFFFF0,
        }

        public enum Msgs
        {
            // GetWindow
            GW_HWNDFIRST           = 0,
            GW_HWNDLAST            = 1,
            GW_HWNDNEXT            = 2,
            GW_HWNDPREV            = 3,
            GW_OWNER               = 4,
            GW_CHILD               = 5,

            // Window messages - WinUser.h
            WM_NULL                = 0x0000,
            WM_CREATE              = 0x0001,
            WM_DESTROY             = 0x0002,
            WM_MOVE                = 0x0003,
            WM_SIZE                = 0x0005,
            WM_KILLFOCUS           = 0x0008,
            WM_SETREDRAW           = 0x000B,
            WM_GETTEXT             = 0x000D,
            WM_GETTEXTLENGTH       = 0x000E,
            WM_PAINT               = 0x000F,
            WM_ERASEBKGND          = 0x0014,
            WM_SHOWWINDOW          = 0x0018,

            WM_FONTCHANGE          = 0x001d,
            WM_SETCURSOR           = 0x0020,
            WM_MOUSEACTIVATE       = 0x0021,
            WM_CHILDACTIVATE       = 0x0022,

            WM_DRAWITEM            = 0x002B,
            WM_MEASUREITEM         = 0x002C,
            WM_DELETEITEM          = 0x002D,
            WM_VKEYTOITEM          = 0x002E,
            WM_CHARTOITEM          = 0x002F,

            WM_SETFONT             = 0x0030,
            WM_COMPAREITEM         = 0x0039,
            WM_WINDOWPOSCHANGING   = 0x0046,
            WM_WINDOWPOSCHANGED    = 0x0047,
            WM_NOTIFY              = 0x004E,
            WM_NOTIFYFORMAT        = 0x0055,
            WM_STYLECHANGING       = 0x007C,
            WM_STYLECHANGED        = 0x007D,
            WM_NCMOUSEMOVE         = 0x00A0,
            WM_NCLBUTTONDOWN       = 0x00A1,

            WM_NCCREATE            = 0x0081,
            WM_NCDESTROY           = 0x0082,
            WM_NCCALCSIZE          = 0x0083,
            WM_NCHITTEST           = 0x0084,
            WM_NCPAINT             = 0x0085,
            WM_GETDLGCODE          = 0x0087,

            // from WinUser.h and RichEdit.h
            EM_GETSEL              = 0x00B0,
            EM_SETSEL              = 0x00B1,
            EM_GETRECT             = 0x00B2,
            EM_SETRECT             = 0x00B3,
            EM_SETRECTNP           = 0x00B4,
            EM_SCROLL              = 0x00B5,
            EM_LINESCROLL          = 0x00B6,
            //EM_SCROLLCARET       = 0x00B7,
            EM_GETMODIFY           = 0x00B8,
            EM_SETMODIFY           = 0x00B9,
            EM_GETLINECOUNT        = 0x00BA,
            EM_LINEINDEX           = 0x00BB,
            EM_SETHANDLE           = 0x00BC,
            EM_GETHANDLE           = 0x00BD,
            EM_GETTHUMB            = 0x00BE,
            EM_LINELENGTH          = 0x00C1,
            EM_LINEFROMCHAR        = 0x00C9,
            EM_GETFIRSTVISIBLELINE = 0x00CE,
            EM_SETMARGINS          = 0x00D3,
            EM_GETMARGINS          = 0x00D4,
            EM_POSFROMCHAR         = 0x00D6,
            EM_CHARFROMPOS         = 0x00D7,

            WM_KEYFIRST            = 0x0100,
            WM_KEYDOWN             = 0x0100,
            WM_KEYUP               = 0x0101,
            WM_CHAR                = 0x0102,
            WM_DEADCHAR            = 0x0103,
            WM_SYSKEYDOWN          = 0x0104,
            WM_SYSKEYUP            = 0x0105,
            WM_SYSCHAR             = 0x0106,
            WM_SYSDEADCHAR         = 0x0107,

            WM_COMMAND             = 0x0111,
            WM_SYSCOMMAND          = 0x0112,
            WM_TIMER               = 0x0113,
            WM_HSCROLL             = 0x0114,
            WM_VSCROLL             = 0x0115,
            WM_UPDATEUISTATE       = 0x0128,
            WM_QUERYUISTATE        = 0x0129,
            WM_MOUSEFIRST          = 0x0200,
            WM_MOUSEMOVE           = 0x0200,
            WM_LBUTTONDOWN         = 0x0201,
            WM_LBUTTONUP           = 0x0202,
            WM_PARENTNOTIFY        = 0x0210,

            WM_NEXTMENU            = 0x0213,
            WM_SIZING              = 0x0214,
            WM_CAPTURECHANGED      = 0x0215,
            WM_MOVING              = 0x0216,

            WM_IME_SETCONTEXT      = 0x0281,
            WM_IME_NOTIFY          = 0x0282,
            WM_IME_CONTROL         = 0x0283,
            WM_IME_COMPOSITIONFULL = 0x0284,
            WM_IME_SELECT          = 0x0285,
            WM_IME_CHAR            = 0x0286,
            WM_IME_REQUEST         = 0x0288,
            WM_IME_KEYDOWN         = 0x0290,
            WM_IME_KEYUP           = 0x0291,
            WM_NCMOUSEHOVER        = 0x02A0,
            WM_NCMOUSELEAVE        = 0x02A2,
            WM_MOUSEHOVER          = 0x02A1,
            WM_MOUSELEAVE          = 0x02A3,

            WM_CUT                 = 0x0300,
            WM_COPY                = 0x0301,
            WM_PASTE               = 0x0302,
            WM_CLEAR               = 0x0303,
            WM_UNDO                = 0x0304,
            WM_RENDERFORMAT        = 0x0305,
            WM_RENDERALLFORMATS    = 0x0306,
            WM_DESTROYCLIPBOARD    = 0x0307,
            WM_DRAWCLIPBOARD       = 0x0308,
            WM_PAINTCLIPBOARD      = 0x0309,
            WM_VSCROLLCLIPBOARD    = 0x030A,
            WM_SIZECLIPBOARD       = 0x030B,
            WM_ASKCBFORMATNAME     = 0x030C,
            WM_CHANGECBCHAIN       = 0x030D,
            WM_HSCROLLCLIPBOARD    = 0x030E,
            WM_QUERYNEWPALETTE     = 0x030F,
            WM_PALETTEISCHANGING   = 0x0310,
            WM_PALETTECHANGED      = 0x0311,
            WM_HOTKEY              = 0x0312,

            WM_USER                = 0x0400,
            EM_SCROLLCARET         = (WM_USER + 49),

            EM_CANPASTE            = (WM_USER + 50),
            EM_DISPLAYBAND         = (WM_USER + 51),
            EM_EXGETSEL            = (WM_USER + 52),
            EM_EXLIMITTEXT         = (WM_USER + 53),
            EM_EXLINEFROMCHAR      = (WM_USER + 54),
            EM_EXSETSEL            = (WM_USER + 55),
            EM_FINDTEXT            = (WM_USER + 56),
            EM_FORMATRANGE         = (WM_USER + 57),
            EM_GETCHARFORMAT       = (WM_USER + 58),
            EM_GETEVENTMASK        = (WM_USER + 59),
            EM_GETOLEINTERFACE     = (WM_USER + 60),
            EM_GETPARAFORMAT       = (WM_USER + 61),
            EM_GETSELTEXT          = (WM_USER + 62),
            EM_HIDESELECTION       = (WM_USER + 63),
            EM_PASTESPECIAL        = (WM_USER + 64),
            EM_REQUESTRESIZE       = (WM_USER + 65),
            EM_SELECTIONTYPE       = (WM_USER + 66),
            EM_SETBKGNDCOLOR       = (WM_USER + 67),
            EM_SETCHARFORMAT       = (WM_USER + 68),
            EM_SETEVENTMASK        = (WM_USER + 69),
            EM_SETOLECALLBACK      = (WM_USER + 70),
            EM_SETPARAFORMAT       = (WM_USER + 71),
            EM_SETTARGETDEVICE     = (WM_USER + 72),
            EM_STREAMIN            = (WM_USER + 73),
            EM_STREAMOUT           = (WM_USER + 74),
            EM_GETTEXTRANGE        = (WM_USER + 75),
            EM_FINDWORDBREAK       = (WM_USER + 76),
            EM_SETOPTIONS          = (WM_USER + 77),
            EM_GETOPTIONS          = (WM_USER + 78),
            EM_FINDTEXTEX          = (WM_USER + 79),

            // Tab Control Messages - CommCtrl.h
            TCM_DELETEITEM         = 0x1308,
            TCM_INSERTITEM         = 0x133E,
            TCM_GETITEMRECT        = 0x130A,
            TCM_GETCURSEL          = 0x130B,
            TCM_SETCURSEL          = 0x130C,
            TCM_ADJUSTRECT         = 0x1328,
            TCM_SETITEMSIZE        = 0x1329,
            TCM_SETPADDING         = 0x132B,

            // olectl.h
            OCM__BASE              = (WM_USER+0x1c00),
            OCM_COMMAND            = (OCM__BASE + WM_COMMAND),
            OCM_DRAWITEM           = (OCM__BASE + WM_DRAWITEM),
            OCM_MEASUREITEM        = (OCM__BASE + WM_MEASUREITEM),
            OCM_DELETEITEM         = (OCM__BASE + WM_DELETEITEM),
            OCM_VKEYTOITEM         = (OCM__BASE + WM_VKEYTOITEM),
            OCM_CHARTOITEM         = (OCM__BASE + WM_CHARTOITEM),
            OCM_COMPAREITEM        = (OCM__BASE + WM_COMPAREITEM),
            OCM_HSCROLL            = (OCM__BASE + WM_HSCROLL),
            OCM_VSCROLL            = (OCM__BASE + WM_VSCROLL),
            OCM_PARENTNOTIFY       = (OCM__BASE + WM_PARENTNOTIFY),
            OCM_NOTIFY             = (OCM__BASE + WM_NOTIFY),

        }

        public const int SCF_SELECTION = 0x0001;

        /* Edit control EM_SETMARGIN parameters */
        public const int EC_LEFTMARGIN = 0x0001;
        public const int EC_RIGHTMARGIN = 0x0002;

        [Flags]
        public enum Flags
        {
            // SetWindowPos Flags - WinUser.h
            SWP_NOSIZE         = 0x0001,
            SWP_NOMOVE         = 0x0002,
            SWP_NOZORDER       = 0x0004,
            SWP_NOREDRAW       = 0x0008,
            SWP_NOACTIVATE     = 0x0010,
            SWP_FRAMECHANGED   = 0x0020,
            SWP_SHOWWINDOW     = 0x0040,
            SWP_HIDEWINDOW     = 0x0080,
            SWP_NOCOPYBITS     = 0x0100,
            SWP_NOOWNERZORDER  = 0x0200,
            SWP_NOSENDCHANGING = 0x0400,
        };



        private static Type tmsgs = typeof (Msgs);


        public static string Mnemonic(int z)
        {
            foreach (int ix in Enum.GetValues(tmsgs))
            {
                if (z == ix)
                    return Enum.GetName(tmsgs, ix);
            }

            return z.ToString("X4");
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public IntPtr hwnd, hwndInsertAfter;
            public int x ,y ,cx ,cy ,flags;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        public struct STYLESTRUCT
        {
            public int styleOld;
            public int styleNew;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        public struct CREATESTRUCT
        {
            public IntPtr lpCreateParams;
            public IntPtr hInstance;
            public IntPtr hMenu;
            public IntPtr hwndParent;
            public int cy;
            public int cx;
            public int y;
            public int x;
            public int style;
            public string lpszName;
            public string lpszClass;
            public int dwExStyle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CHARFORMAT
        {
            public int cbSize;
            public UInt32 dwMask;
            public UInt32 dwEffects;
            public Int32 yHeight;
            public Int32 yOffset;
            public Int32 crTextColor;
            public byte bCharSet;
            public byte bPitchAndFamily;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] szFaceName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTL
        {
            public Int32 X;
            public Int32 Y;
        }


        public static void BeginUpdate(IntPtr hWnd)
        {
            SendMessage(hWnd, (int)Msgs.WM_SETREDRAW, 0, IntPtr.Zero);
        }

        public static void EndUpdate(IntPtr hWnd)
        {
            SendMessage(hWnd, (int)Msgs.WM_SETREDRAW, 1, IntPtr.Zero);
        }


        [DllImport("User32.dll", EntryPoint="SendMessage", CharSet=CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd,
                                                [MarshalAs(UnmanagedType.I4)] Msgs msg,
                                                int  wParam,
                                                IntPtr lParam);


        [DllImport("User32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd,
                                                [MarshalAs(UnmanagedType.I4)] Msgs msg,
                                                int  wParam,
                                                int lParam);


        [DllImport("User32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, int msg, int wparam, IntPtr lparam);

        [DllImport("User32.dll", EntryPoint="SendMessage", CharSet=CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, int msg, int wparam, int lparam);

        [DllImport("User32.dll", EntryPoint="SendMessage", CharSet=CharSet.Auto)]
        public static extern int SendMessageRef(IntPtr hWnd, int msg, out int wparam, out int lparam);

        [DllImport("User32.dll",CharSet = CharSet.Auto)]
        public static extern IntPtr GetWindow(IntPtr hWnd, int uCmd);

        [DllImport("User32.dll",CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, char[] className, int maxCount);

        [DllImport("user32.dll")]
        //[return: MarshalAs(UnmanagedType.Bool)]
        public static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
                                              int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError=true)]
        public static extern uint GetWindowLong(IntPtr hWnd, uint nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, uint nIndex, uint dwNewLong);
    }

}
