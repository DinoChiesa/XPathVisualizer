// CustomTabControl.cs
//
// This tab control provides an extension of the WinForms TabControl to
// provide an improved look-and-feel:
//
//   - it has the stylized tabs, as with Visual Studio 2008, with tabs that
//     oerlap each other, and are curevd, rather than the square tabs on the
//     .NET built-in TabControl.
//
//   - it displays a "close button" on the far right of each tab, like IE8,
//     and UNLIKE Visual Studio 2008, to allow convenient closing of tabs.  It also
//     provides a before-closing event for that purpose.
//
//
// This TabControl supports TabAlignment.Top and TabAlignment.Bottom.  Does
// not support TabAlignment.Right or TabAlignment.Left.  It does not support
// custom close images - they are always X's.  And I haven't tested this
// TabControl with images on the tabs. I have nevver tested it with
// RightToLeft text.
//
//
// There are a number of custom TabControl derivatives available. This one
// provided the look and feel I wanted.  Thanks to the authors of these
// related articles:
//
//   shapes of tabs:
//      http://www.codeproject.com/KB/dotnet/CustomTabControl.aspx
//   closable tabs:
//      http://www.codeproject.com/KB/miscctrl/closabletabs.aspx
//
// =======================================================
//
// Build it with the associated makefile, then add it to the Visual Studio
// Toolbox, and use it in any WinForms application.
//
// Thu, 15 Apr 2010  14:56
//
//
//
// =======================================================
// Copyright (c) 2010 Dino Chiesa
// All rights reserved.
//
// ------------------------------------------------------------------
// This code module is licensed under the Microsoft Public License.
//
// Microsoft Public License (Ms-PL)
//
// This license governs use of the accompanying software. If you use the software,
// you accept this license. If you do not accept the license, do not use the
// software.
//
// 1. Definitions
//
// The terms "reproduce," "reproduction," "derivative works," and "distribution"
// have the same meaning here as under U.S. copyright law.  A "contribution" is
// the original software, or any additions or changes to the software.  A
// "contributor" is any person that distributes its contribution under this
// license.  "Licensed patents" are a contributor's patent claims that read
// directly on its contribution.
//
//
// 2. Grant of Rights
//
// (A) Copyright Grant- Subject to the terms of this license, including the
// license conditions and limitations in section 3, each contributor grants you a
// non-exclusive, worldwide, royalty-free copyright license to reproduce its
// contribution, prepare derivative works of its contribution, and distribute its
// contribution or any derivative works that you create.
//
// (B) Patent Grant- Subject to the terms of this license, including the license
// conditions and limitations in section 3, each contributor grants you a
// non-exclusive, worldwide, royalty-free license under its licensed patents to
// make, have made, use, sell, offer for sale, import, and/or otherwise dispose of
// its contribution in the software or derivative works of the contribution in the
// software.
//
//
// 3. Conditions and Limitations
//
// (A) No Trademark License- This license does not grant you rights to use any
// contributors' name, logo, or trademarks.
//
// (B) If you bring a patent claim against any contributor over patents that you
// claim are infringed by the software, your patent license from such contributor
// to the software ends automatically.
//
// (C) If you distribute any portion of the software, you must retain all
// copyright, patent, trademark, and attribution notices that are present in the
// software.
//
// (D) If you distribute any portion of the software in source code form, you may
// do so only under this license by including a complete copy of this license with
// your distribution. If you distribute any portion of the software in compiled or
// object code form, you may only do so under a license that complies with this
// license.
//
// (E) The software is licensed "as-is." You bear the risk of using it. The
// contributors give no express warranties, guarantees or conditions. You may have
// additional consumer rights under your local laws which this license cannot
// change. To the extent permitted under your local laws, the contributors exclude
// the implied warranties of merchantability, fitness for a particular purpose and
// non-infringement.
//
// ------------------------------------------------------------------
//

using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System;
using System.Reflection;


namespace Ionic.WinForms
{

    /// <summary>
    ///   A TabControl that draws the tabs in a VS2005/8 visual style.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     See http://www.codeproject.com/KB/dotnet/CustomTabControl.aspx for
    ///     the original.  This one has been modified to eliminate the
    ///     visual anomalies that occur when adding/removing tabs, scrolling
    ///     left and right, and so on.
    ///   </para>
    /// </remarks>
    [ToolboxBitmap(typeof(TabControl))]
    public class CustomTabControl : TabControl
    {
        /// <summary>
        ///   Fires before a tab will be closed. To cancel the tab closure,
        ///   set Cancel = true in the EventArgs.
        /// </summary>
        public event EventHandler<BeforeCloseTabEventArgs> BeforeCloseTab;


        // private, protected fields
        protected Int32 _priorSelectedIndex;
        protected Int16 _priorFirstTab;
        protected TabAlignment _priorAlignment;
        UpDown _scroller;
        protected TabControlDisplayManager _DisplayManager = TabControlDisplayManager.Custom;
        private  System.Timers.Timer _hoverTimer;
        private Image CloseImage;
        private Image CloseImageGray;
        private Image CloseImageHover;
        private Point ImageOffset;
        private MouseHoverState mhs;
        private int _hoverInterval;

        public CustomTabControl() : base()
        {
            if (this._DisplayManager.Equals(TabControlDisplayManager.Custom))
            {
                this.SetStyle(ControlStyles.UserPaint, true);
                this.ItemSize = new Size(0, 15);
                this.Padding = new Point(9,0);
            }

            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.ResizeRedraw = true;
            _priorSelectedIndex = -1;
            _priorAlignment = TabAlignment.Left;

            ImageOffset = new System.Drawing.Point(16, 2);

            // We are simulating a close button with the close images. The
            // image is a stylized X from the VS2008 image library.  There are
            // three versions - a regular CloseImage, the greyed version for
            // tabs that are not currently selected, and the hover image,
            // which is a highlighted (red) form that gets displayed when the
            // mouse hovers over the X. The effect is to make the image itself
            // highlight like a normal button might.
            //
            // OnMouseHover doesn't really work for the purposes of the close
            // "button".  The close image isn't really a Control, so it gets no
            // Hover events.  The TabControl is a Control, and it gets hover
            // events, but only one, on entry to the control.  Moving the
            // mouse around on different tabs doesn't send a new hover event.
            // Therefore, we need to track the mouse events, and simulate the
            // effect.  We use a timer for that purpose.  It gets reset
            // whenever the mouse moves and is present on the close image. If
            // the mouse stops moving for a period of _hoverInterval ms, then
            // the timer fires and the close image is painted with the hover
            // image (a red X).  If the mouse moves away from the closeimage
            // on the selected tab, the timer gets disabled.
            _hoverInterval = 110;
            _hoverTimer = new System.Timers.Timer
                {
                    Interval = _hoverInterval,
                    AutoReset = false
                        };

            // Hook up the Elapsed event for the timer.
            _hoverTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
            mhs = new MouseHoverState();


            // get the images
            System.Reflection.Assembly assembly = this.GetType().Assembly;
            System.IO.Stream s1 =
                assembly.GetManifestResourceStream("CustomControls.Resources.Close.png");
            var b = new Bitmap(s1);
            s1.Dispose();
            //b.MakeTransparent();
            CloseImage = b;

            s1 = assembly.GetManifestResourceStream("CustomControls.Resources.Close-grayed.png");
            b = new Bitmap(s1);
            s1.Dispose();
            //b.MakeTransparent();
            CloseImageGray= b;

            s1 = assembly.GetManifestResourceStream("CustomControls.Resources.Close-hover.png");
            b = new Bitmap(s1);
            s1.Dispose();
            //b.MakeTransparent();
            CloseImageHover= b;

            //System.Console.WriteLine("Images: Close({0}) Gray({1}) Hover({2})", CloseImage, CloseImageGray, CloseImageHover);
        }




        private bool PointIsOnCloseImage(Point p, int tabIndex)
        {
            Rectangle r = GetTabRect(tabIndex);
            // make the "hit area" 1px larger than the actual image
            r.Offset(r.Width - ImageOffset.X-1, ImageOffset.Y-1);
            r.Width = CloseImage.Width+2;
            r.Height = CloseImage.Height+2;
            return r.Contains(p);
        }


        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (CloseImage != null)
            {
                Point p = e.Location;
                for (int i = 0; i < TabCount; i++)
                {
                    if (PointIsOnCloseImage(p, i))
                    {
                        CloseTab(i);
                        return;
                    }
                }
            }
            base.OnMouseClick(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            Unhover("Leave");
        }


        private void Unhover(string label)
        {
            lock(mhs)
            {
                _hoverTimer.Enabled = false;
                mhs.IsHovering = false;
                mhs.EnableHover = false;
            }

            if (mhs.TabIndex < this.TabCount)
                PaintCloseImage();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (CloseImageHover != null)
            {
                if (PointIsOnCloseImage(e.Location, this.SelectedIndex))
                {
                    // reset the timer. It fires when the mouse stops moving.
                    _hoverTimer.Interval = _hoverInterval;
                    _hoverTimer.Enabled = true;
                    lock(mhs)
                    {
                        mhs.TabIndex = this.SelectedIndex;
                        mhs.EnableHover = true;
                    }
                }
                else
                {
                    Unhover("Move");
                }
            }
            base.OnMouseMove(e);
        }


        private void OnTimedEvent(object source, System.Timers.ElapsedEventArgs e)
        {
            Action a = () =>
                {
                    lock(mhs)
                    {
                        mhs.IsHovering = (mhs.TabIndex == this.SelectedIndex)
                        ? mhs.EnableHover
                        : false;
                    }
                    PaintCloseImage();
                };

            this.Invoke(a, new object[]{ } );
        }

        private void PaintCloseImage()
        {
            // It can be that the forms gets a MouseLeave when the user closes the current tab.
            // Need to check to see if the index is still valid.
            if (mhs.TabIndex >=0 && mhs.TabIndex < this.TabCount)
            {
                var g = this.CreateGraphics();
                PaintCloseImage(g, mhs.TabIndex, true);
                g.Dispose();
            }
        }


        private void CloseTab(int tabIndex)
        {
            if (BeforeCloseTab != null)
            {
                var e = new BeforeCloseTabEventArgs { TabIndex = tabIndex };
                BeforeCloseTab(this, e);
                if (e.Cancel) return;
            }
            TabPages.Remove(TabPages[tabIndex]);

            // Select a new tab
            if (tabIndex > 0)
                this.SelectedIndex = tabIndex - 1;
            else if (this.TabCount >0)
                this.SelectedIndex = 0;
            else
                this.SelectedIndex = -1;

            lock(mhs)
            {
                mhs.IsHovering = false;
                mhs.EnableHover = false;
                mhs.TabIndex = -1;
            }
        }


        [System.ComponentModel.DefaultValue(typeof(TabControlDisplayManager), "Custom")]
        public TabControlDisplayManager DisplayManager
        {
            get
            {
                return this._DisplayManager;
            }
            set
            {
                if (this._DisplayManager != value)
                {
                    if (this._DisplayManager.Equals(TabControlDisplayManager.Custom))
                    {
                        this.SetStyle(ControlStyles.UserPaint, true);
                        this.ItemSize = new Size(0, 25);
                        this.Padding = new Point(9,0);
                    }
                    else
                    {
                        this.SetStyle(ControlStyles.UserPaint, false);
                        this.ItemSize = new Size(0, 0);
                        this.Padding = new Point(6,3);
                    }
                }
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            if (this.DesignMode == true)
                PaintTransparentBackgroundImpl(pevent.Graphics);
            else
                this.PaintTransparentBackground(pevent.Graphics, this.ClientRectangle);
        }


        protected void PaintTransparentBackground(Graphics g, Rectangle clipRect)
        {
            if ((this.Parent != null))
            {
                clipRect.Offset(this.Location);
                PaintEventArgs e = new PaintEventArgs(g, clipRect);
                GraphicsState state = g.Save();
                g.SmoothingMode = SmoothingMode.HighSpeed;
                try
                {
                    g.TranslateTransform((float)-this.Location.X, (float)-this.Location.Y);
                    this.InvokePaintBackground(this.Parent, e);
                    this.InvokePaint(this.Parent, e);
                }

                finally
                {
                    g.Restore(state);
                    clipRect.Offset(-this.Location.X, -this.Location.Y);
                }
            }
            else
                PaintTransparentBackgroundImpl(g);
        }



        private void PaintTransparentBackgroundImpl(Graphics g)
        {
            var backBrush = new LinearGradientBrush(this.Bounds,
                                                    SystemColors.ControlLightLight,
                                                    SystemColors.ControlLight,
                                                    LinearGradientMode.Vertical);
            g.FillRectangle(backBrush, this.Bounds);
            backBrush.Dispose();
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            int first = FirstVisibleTab;

            this.PaintTransparentBackground(e.Graphics, this.ClientRectangle);
            this.PaintAllTabs(e, first);
            this.PaintTabPageBorder(e);
            this.EraseLineForSelectedTab(e, first);
        }


        private void PaintAllTabs(System.Windows.Forms.PaintEventArgs e, int first)
        {
            if (this.TabCount < 1) return;

            for (int i = 0; i < this.TabCount ; i++)
                this.PaintTab(e, i, first);

            // When paint happens, the tab to the left of the selected
            // tab, and the tab left of the previously selected tab, need
            // to be redrawn. We can use Invalidate() to ask for that.
            // But, we must do this only if the selection has not changed,
            // otherwise we get into a forever-repeating paint+invalidate
            // loop.

            // Has the selection changed?
            if (this._priorSelectedIndex != this.SelectedIndex)
            {
                Action<int> InvalidateOneTab = (x) => {
                    // Need to invalidate the tab of the given index.
                    // Actually only need the right most 10px, or so.
                    // Also need to descend into the tab page, to
                    // insure the control redraws the border nicely.
                    var r1 = this.GetTabRect(x);
                    var r2 = (this.Alignment == System.Windows.Forms.TabAlignment.Top)
                    ? new Rectangle(r1.Right-10, r1.Y+3, 10, r1.Height - 1)
                    : new Rectangle(r1.Right-10, r1.Y-2, 10, r1.Height+1);
                    this.Invalidate(r2);
                };

                // invalidate the tab left of the selected tab
                if (this.SelectedIndex > 0)
                    InvalidateOneTab(this.SelectedIndex-1);

                // invalidate the tab left of the prior selected tab
                if (this._priorSelectedIndex > 0)
                    InvalidateOneTab(this._priorSelectedIndex-1);

                // remember the prior selected tab
                this._priorSelectedIndex = this.SelectedIndex;
            }
        }


        private Size GetScrollerPositionOffset()
        {
            if (this.Alignment == TabAlignment.Top)
                return new Size(-4,4);
            return new Size(-4,-4);
        }


        private void PaintTab(System.Windows.Forms.PaintEventArgs e, int index, int first)
        {
            GraphicsPath path = this.GetPath(index);
            PaintTabBackground(e.Graphics, index, path);
            PaintTabBorder(e.Graphics, index, path);
            PaintTabText(e.Graphics, index, first);
            PaintTabImage(e.Graphics, index);
            PaintCloseImage(e.Graphics, index, false);
        }

        private void PaintTabBackground(System.Drawing.Graphics graph, int index, System.Drawing.Drawing2D.GraphicsPath path)
        {
            System.Drawing.Brush buttonBrush = (index == this.SelectedIndex)
                ? (System.Drawing.Brush) new System.Drawing.SolidBrush(SystemColors.ControlLightLight)
                : (System.Drawing.Brush) new LinearGradientBrush(this.GetTabRect(index),
                                                                 SystemColors.ControlLightLight,
                                                                 SystemColors.ControlLight,
                                                                 LinearGradientMode.Vertical);

            graph.FillPath(buttonBrush, path);
            buttonBrush.Dispose();
        }

        private void PaintTabBorder(System.Drawing.Graphics graph, int index, System.Drawing.Drawing2D.GraphicsPath path)
        {
            Pen borderPen = new Pen((index == this.SelectedIndex) ? ThemedColors.ToolBorder : SystemColors.ControlDark);

            graph.DrawPath(borderPen, path);
            borderPen.Dispose();
        }


        private void PaintTabImage(System.Drawing.Graphics g, int index)
        {
            if (ImageList == null) return;
            Image tabImage = null;
            int ix = this.TabPages[index].ImageIndex;
            if (ix > -1)
                tabImage = this.ImageList.Images[ix];
            else if (this.TabPages[index].ImageKey.Trim().Length > 0)
                tabImage = this.ImageList.Images[this.TabPages[index].ImageKey];

            if ( tabImage == null) return;

            // ToDo:  verify that this works. I think it may need to be adjusted.
            Rectangle rect = this.GetTabRect(index);
            g.DrawImage(tabImage, rect.Right - rect.Height - 4, 4, rect.Height - 2, rect.Height - 2);
        }


        /// <summary>
        ///   Paint the close image on the tab, on the right hand side.
        /// </summary>
        private void PaintCloseImage(System.Drawing.Graphics g, int index, bool wantInvalidate)
        {
            Image img = (index == this.SelectedIndex)
                ? ((mhs.IsHovering && mhs.TabIndex == index) ? CloseImageHover : CloseImage)
                : CloseImageGray;

            var r= GetTabRect(index);

            var p = (this.Alignment == System.Windows.Forms.TabAlignment.Top)
                ? new Point(r.X + r.Width - ImageOffset.X, r.Y + ImageOffset.Y)
                : new Point(r.X + r.Width - ImageOffset.X, r.Y + ImageOffset.Y-1);

            g.DrawImage(img,p);

            if (wantInvalidate)
            {
                // invalidate to insure the screen gets updated
                Rectangle r2 = new Rectangle(p.X-1,p.Y-1, CloseImage.Width+4, CloseImage.Height+4);
                this.Invalidate(r2);
            }
        }


        private void PaintTabText(System.Drawing.Graphics g, int index, int first)
        {
            string tabtext = this.TabPages[index].Text;

            // This is sort of a hack.
            // Add spaces to make the 1st tab wider.  The problem is, the
            // automaticall-determined width for the first tab isn't wide
            // enough to display the text, because this TabControl trims
            // off the top left corner for aesthetic purposes.  Other tabs
            // are fine, because they actually get larger, rather than
            // smaller.  So add spaces if it's the first, and trim spaces
            // if not.  The pitfall with this approach is that if the
            // application is depending on a particular value for the tab
            // Text property - it will change secretly. Oh well.
            if (index == first)
            {
                // painting text for the first tab
                if (!tabtext.EndsWith("   "))
                    tabtext = this.TabPages[index].Text = tabtext + "   ";
            }
            else
            {
                if (tabtext.EndsWith("   "))
                    tabtext = this.TabPages[index].Text = tabtext.Trim();
            }


            Rectangle r = this.GetTabRect(index);
            Rectangle r2;
            Font tabFont = this.Font;
            if (index == this.SelectedIndex)
            {
                tabFont = new Font(this.Font, FontStyle.Bold);

                if (this.Alignment == System.Windows.Forms.TabAlignment.Top)
                {
                    r2 = (index == first)
                        ? new Rectangle(r.Left + r.Height+8, r.Top + 1, r.Width - r.Height - 5, r.Height)
                        : new Rectangle(r.Left + 12, r.Top + 1, r.Width - 12, r.Height);
                }
                else
                {
                    r2 = (index == first)
                        ? new Rectangle(r.Left + r.Height+8, r.Top, r.Width - r.Height - 5, r.Height)
                        : new Rectangle(r.Left + 12, r.Top, r.Width - 10, r.Height);
                }
            }
            else
            {
                if (this.Alignment == System.Windows.Forms.TabAlignment.Top)
                {
                    r2 = (index == first)
                        ? new Rectangle(r.Left + r.Height+8, r.Top + 1, r.Width - r.Height-5, r.Height)
                        : new Rectangle(r.Left + 14, r.Top + 1, r.Width - 10, r.Height);
                }
                else
                {
                    r2 = (index == first)
                        ? new Rectangle(r.Left + r.Height+8, r.Top, r.Width - r.Height-5, r.Height)
                        : new Rectangle(r.Left + 14, r.Top, r.Width - 10, r.Height);
                }
            }

            // allow room for the close image
            r2.Inflate(-6,0); // shrink
            r2.Offset(-12,0); // slide

            if (r2.Right > this.Right) return; // don't draw it...

            var format = new System.Drawing.StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    };

            Brush forebrush = (this.TabPages[index].Enabled)
                ? SystemBrushes.ControlText
                : SystemBrushes.ControlDark;

            g.DrawString(tabtext, tabFont, forebrush, r2, format);
        }



        private void PaintTabPageBorder(System.Windows.Forms.PaintEventArgs e)
        {
            if (this.TabCount > 0 && this.SelectedIndex >=0)
            {
                Rectangle borderRect= this.TabPages[this.SelectedIndex].Bounds;
                borderRect.Inflate(1, 1);
                ControlPaint.DrawBorder(e.Graphics, borderRect, ThemedColors.ToolBorder, ButtonBorderStyle.Solid);
            }
        }


        private void EraseLineForSelectedTab(System.Windows.Forms.PaintEventArgs e, int first)
        {
            if (this.SelectedIndex == -1) return;

            Rectangle selrect = this.GetTabRect(this.SelectedIndex);
            int rightmost = (selrect.Right+4 > this.Right) ? (this.Right-4) : selrect.Right;
            int delta = (this.SelectedIndex == first) ? 2 : 6 - selrect.Height;

            if (this.Alignment == System.Windows.Forms.TabAlignment.Top)
                e.Graphics.DrawLine(SystemPens.ControlLightLight, selrect.Left + delta, selrect.Bottom + 1, rightmost - 2, selrect.Bottom + 1);
            else
                e.Graphics.DrawLine(SystemPens.ControlLightLight, selrect.Left + delta, selrect.Top - 2, rightmost - 2, selrect.Top - 2);
        }



        private int FirstVisibleTab
        {
            get
            {
                for (int i=0; i < this.TabCount; i++)
                {
                    if (GetTabRect(i).Left >=0) return i;
                }
                return this.TabCount;
            }
        }

        private int LastVisibleTab
        {
            get
            {
                if (this.TabCount == 0) return -1;
                for (int i=this.TabCount-1; i >= 0; i--)
                {
                    if (GetTabRect(i).Right < this.Right) return i;
                }
                return 0;
            }
        }


        /// <summary>
        ///   This method returns the shape of the tab given the index.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     The custom shape of the tabs is the main purpose of this
        ///     TabControl.  This method has to consider: whether the tab is
        ///     currently selected, whether the tab is the first one shown on
        ///     the left, whether the TabControl is using Top or Bottom
        ///     Alignment.  It then produces a path that outlines the tab.
        ///     This path is later filled and outlined, by the caller.
        ///   </para>
        /// </remarks>
        private GraphicsPath GetPath(int index)
        {
            int first = FirstVisibleTab;
            var path = new GraphicsPath();
            path.Reset();

            if (index < first) return path;

            var rect = this.GetTabRect(index);
            int rightmost = (rect.Right > this.Right-4) ? (this.Right-4) : rect.Right;

            if (this.Alignment == System.Windows.Forms.TabAlignment.Bottom)
            {
                if (index == first)
                {
                    // (index == first) implies the leftmost tab. This tab is always displayed the same way,
                    // selected or not.
                    path.AddLine(rect.Left + 1, rect.Top-1, rect.Left + rect.Height, rect.Bottom - 2);  // left diagonal
                    path.AddLine(rect.Left + rect.Height + 4, rect.Bottom, rightmost - 3, rect.Bottom); // bottom horizontal
                    path.AddLine(rightmost - 1, rect.Bottom - 2, rightmost - 1, rect.Top-1);            // right vertical
                    return path;
                }

                // The other tabs are displayed differently depending on their selection state.
                // If the tab is selected, then its left diagonal overlaps the tab to the left.
                if (index == this.SelectedIndex)
                {
                    path.AddLine(rect.Left - 9, rect.Top-2, rect.Left + 4, rect.Bottom-2);     // left diagonal
                    path.AddLine(rect.Left + 8, rect.Bottom, rightmost - 3, rect.Bottom);      // bottom horizontal
                    path.AddLine(rightmost - 1, rect.Bottom - 2, rightmost - 1, rect.Top + 1); // rt vertical
                    path.AddLine(rightmost - 1, rect.Top-2, rect.Left - 9, rect.Top-2);        // top horizontal
                    return path;
                }

                path.AddLine(rect.Left, rect.Bottom - 6, rect.Left + 4, rect.Bottom - 2);  // truncated left diagonal
                path.AddLine(rect.Left + 8, rect.Bottom, rightmost - 3, rect.Bottom);      // bottom horizontal
                path.AddLine(rightmost - 1, rect.Bottom - 2, rightmost - 1, rect.Top + 1); // right vertical
                path.AddLine(rightmost - 1, rect.Top-2, rect.Left, rect.Top-2);            // top horizontal
                return path;
            }

            if (index == first)
            {
                // (index == first) implies the leftmost tab. This tab is always displayed the same way,
                // selected or not.
                path.AddLine(rect.Left + 1, rect.Bottom + 1, rect.Left + rect.Height, rect.Top + 2); // left diagonal
                path.AddLine(rect.Left + rect.Height + 4, rect.Top, rightmost - 3, rect.Top);        // top horizontal
                path.AddLine(rightmost - 1, rect.Top + 2, rightmost - 1, rect.Bottom + 1);           // right vertical
                return path;
            }

            // The other tabs are displayed differently depending on their selection state.
            // If the tab is selected, then its left diagonal overlaps the tab to the left.
            if (index == this.SelectedIndex)
            {
                path.AddLine(rect.Left - 9, rect.Bottom , rect.Left + 4, rect.Top + 2);       // left diagonal
                path.AddLine(rect.Left + 8, rect.Top, rightmost - 3, rect.Top);               // top horizontal
                path.AddLine(rightmost - 1, rect.Top + 2, rightmost - 1, rect.Bottom + 1);    // rt vertical
                path.AddLine(rightmost - 1, rect.Bottom + 1, rect.Left - 9, rect.Bottom + 1); // bottom horizontal
                return path;
            }

            path.AddLine(rect.Left, rect.Top + 6, rect.Left + 4, rect.Top + 2);        // truncated left diagonal
            path.AddLine(rect.Left + 8, rect.Top, rightmost - 3, rect.Top);            // top horizontal
            path.AddLine(rightmost - 1, rect.Top + 2, rightmost - 1, rect.Bottom + 1); // right vertical
            path.AddLine(rightmost - 1, rect.Bottom + 1, rect.Left, rect.Bottom + 1);  // bottom horizontal
            return path;
        }


        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            _priorFirstTab = (Int16)FirstVisibleTab;
        }


        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            this.OnFontChanged(EventArgs.Empty);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            IntPtr hFont = this.Font.ToHfont();
            User32.SendMessage(this.Handle, (int)User32.Msgs.WM_SETFONT, hFont, (IntPtr)(-1));
            User32.SendMessage(this.Handle, (int)User32.Msgs.WM_FONTCHANGE, IntPtr.Zero, IntPtr.Zero);
            this.UpdateStyles();
            this.ItemSize = new Size(0, this.Font.Height + 2);
        }


        protected override void WndProc(ref Message m)
        {
            Int16 hiword;
            switch (m.Msg)
            {
                case (int)User32.Msgs.WM_HSCROLL:
                    // Handle WM_HSCROLL notifications,
                    // from the captive updown control. This happens when the
                    // tabcontrol scrolls left or right, when there are
                    // too many tabs to show at once. Need to invalidate
                    // tabs as we scroll left, because of the special
                    // way we display the first (leftmost) tab.
                    hiword = (Int16)((m.WParam.ToInt32() & 0xFFFF0000) >> 16);
                    //Int16 loword = (Int16)(m.WParam.ToInt32() & 0x0000FFFF);
                    if (_priorFirstTab > hiword)
                    {
                        // left button clicked
                        var r1 = this.GetTabRect(_priorFirstTab);
                        var r2 = new Rectangle(r1.X-10, r1.Y, r1.Width+10, r1.Height+1);
                        this.Invalidate(r2);
                    }
                    else if (_priorFirstTab < hiword)
                    {
                        // the right button was clicked.
                        // Invalidate the tab scrolling in
                        // from the right.
                        int ix = LastVisibleTab;
                        if (ix >=0)
                        {
                            var r1 = this.GetTabRect(ix);
                            this.Invalidate(r1);
                        }
                    }
                    _priorFirstTab = hiword;
                    break;

                case (int)User32.Msgs.WM_PARENTNOTIFY:
                    // Handle notification of WM_CREATE - the scroller is being added to
                    // the tab control.  We want to relocate the thing, so we grab
                    // this message, and move it.
                    {
                        hiword = (Int16)((m.WParam.ToInt32() & 0xFFFF0000) >> 16);
                        Int16 loword = (Int16)(m.WParam.ToInt32() & 0x0000FFFF);
                        // System.Console.WriteLine("WM_PARENTNOTIFY hi({0:X4}) lo({1}) LP({2:X8})",
                        //                          hiword, User32.Mnemonic(loword),
                        //                          m.LParam.ToInt32());
                        if (loword == (int)User32.Msgs.WM_CREATE)
                        {
                            char[] className = new char[64];
                            int length = User32.GetClassName(m.LParam, className, 63);
                            string s = new string(className, 0, length);
                            // System.Console.WriteLine("  WM_CREATE for {0}.", s);

                            if (s == "msctls_updown32")
                            {
                                // The child scroller control is being created.
                                if (_scroller != null)
                                    _scroller.ReleaseHandle();
                                _scroller = new UpDown(m.LParam, GetScrollerPositionOffset);
                            }
                        }
                    }
                    break;
            }

            base.WndProc(ref m);
        }



        public enum TabControlDisplayManager
        {
            Default,
            Custom
        }


        class UpDown : NativeWindow
        {
            Func<Size> GetOffset;

            public UpDown(IntPtr hwnd, Func<Size> getOffset) : base()
            {
                this.AssignHandle(hwnd);
                GetOffset = getOffset;
            }


            protected override void WndProc(ref Message m)
            {
                switch (m.Msg)
                {
                    case (int)User32.Msgs.WM_DESTROY:
                        this.ReleaseHandle();
                        break;
                    case (int)User32.Msgs.WM_NCDESTROY:
                        this.ReleaseHandle();
                        break;

                    case (int)User32.Msgs.WM_NCPAINT:
                    case (int)User32.Msgs.WM_PAINT:
                        break;

                    case (int)User32.Msgs.WM_WINDOWPOSCHANGING:
                        // move the control just a bit
                        User32.WINDOWPOS wp = (User32.WINDOWPOS)(m.GetLParam(typeof(User32.WINDOWPOS)));
                        var sz = GetOffset(); // obtain from parent
                        wp.x += sz.Width;
                        wp.y += sz.Height;
                        System.Runtime.InteropServices.Marshal.StructureToPtr(wp, m.LParam, true);
                        break;
                }

                base.WndProc(ref m);
            }
        }


    }

    public class BeforeCloseTabEventArgs : EventArgs
    {
        public int TabIndex
        {
            get;set;
        }
        public bool Cancel
        {
            get;set;
        }
    }


    class MouseHoverState
    {
        public int TabIndex;
        public bool IsHovering;
        public bool EnableHover;
    }


}
