//#define Trace

// XPathVisualizerTool.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009-2010 Dino Chiesa.
// All rights reserved.
//
// This file is part of the source code disribution for Ionic's
// XPath Visualizer Tool.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Microsoft Public License.
// See the file License.rtf or License.txt for the license details.
// More info on: http://XPathVisualizer.codeplex.com
//
// ------------------------------------------------------------------
//
// Last saved: <2011-November-13 10:43:00>
//
//

using System;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;                    // for the Contains extension
using System.Xml.Linq;                // XElement
using System.Windows.Forms;
using System.Text;
using System.Text.RegularExpressions; // Regex
using CodePlex.XPathParser;
using System.Runtime.InteropServices;

namespace XPathVisualizer
{
    public partial class XPathVisualizerTool : Form
    {
        // Design notes:
        //
        // A major portion of the UI is a CustomTabControl, each tab
        // holds a single control, a RichTextBox.  The CustomTabControl
        // displays good-looking Visual-Studio-2008-like tabs, except
        // that each one has a IE8-like "close" button on the tab.
        //
        // The local variable, richTextBox1, holds a reference to the
        // currently-displayed RichTextBox.  At design time, it is the
        // rtb in the single page, in the TabControl.  But, during form
        // load, at runtime, the TabPage visible during design-time is
        // removed from the TabControl.
        //
        // When the user loads a new XML document, a new RTB is created,
        // dynamically, and then richTextBox1 gets that reference. When the
        // user selects a new tab, richTextBox1 gets the reference to the
        // currently-selected tab.
        //
        // There's a RichTextBoxExtras class, which wraps RTB and provides
        // some additional capabilities, most notably, suppression of
        // redraw, which allows smoother UI experience during the
        // progressive background syntax highlighting. The _rtbe instance
        // refers to richTextBox1, so _rtbe gets reset to null when the
        // selected tab changes.
        //
        // tabState and tabPage are used like local variables, but like
        // richTextBox1, they refer to the currently-displayed tab. The
        // state attached to a tab is stored in TabPage.Tag, and it
        // includes things like an xpathnavigator for the document, the
        // source location for the displayed document (if any), and
        // other things.
        //
        // There's a MRU list on the File... Menu, that presents the
        // recent documents. Every time a file is opened the MRU list
        // gets updated, and that file is put on top of the list. If you
        // click on a file from the Recent list and a tab already holds
        // that file, it isn't opened again; instead the tab holding
        // that file gets activated.
        //
        // When the user changes the xpath, it's automatically evaluated
        // against the document in the currently displayed tab.
        //

        private string fileToLoad;
        private int originalGroupBoxMinHeight;
        private int originalPanel1MinSize;
        private System.Threading.ManualResetEvent wantFormat = new System.Threading.ManualResetEvent(false);
        private DateTime _originDateTime = new System.DateTime(0);
        private System.DateTime _lastRtbKeyPress;
        private System.DateTime _lastXpathChange;
        private bool _keepCursorPosition;
        private int _handlingXpathChangeEvents = 0;
        private XPathParser<XElement> xpathParser = new XPathParser<XElement>();
        private bool isLoading;
        private int extractCount;
        private int _intTicks;
        private bool _lastEventWasAlt;
        private int _lastMenuItemIndex;
        private int tn;
        private bool isDisplayingXmlnsPanel;
        private Color kindaPink = Color.FromArgb(Color.Red.A, 0xFF, 0x99, 0x99);
        private Color badXpathColor = Color.FromArgb((Color.Red.A << 24) | 0xFFDEAD);
        private Color matchingEltColor = Color.FromArgb(Color.Red.A, 0x98, 0xFb, 0x98);
        private XmlReaderSettings readerSettings = new XmlReaderSettings
        {
            ProhibitDtd = false,
            XmlResolver = new Ionic.Xml.XhtmlResolver()
        };
        #if AUTOHIDE
        private bool aMainMenuItemIsDropped;
        #endif

        public XPathVisualizerTool()
        {
            SetupDebugConsole(); // for debugging purposes
            InitializeComponent();
            FixupTitle();
            RememberSizes();
            AdjustSplitterSize();
            FillFormFromRegistry();
            KickoffColorizer();
            DisableMatchButtons();
            SetupAutoHide();
        }

        private void UpdateStatus(string format, params object[] args)
        {
            var s = String.Format(format, args);
            this.lblStatus.Text = s;
            if (tabState != null)
                tabState.status = s;
        }

        private void RememberSizes()
        {
            originalGroupBoxMinHeight = this.groupBox1.MinimumSize.Height;
            originalPanel1MinSize = this.splitContainer3.Panel1MinSize;
        }

        private void FixupTitle()
        {
            var a = System.Reflection.Assembly.GetExecutingAssembly();
            object[] attr = a.GetCustomAttributes(typeof(System.Reflection.AssemblyDescriptionAttribute), true);
            var desc = attr[0] as System.Reflection.AssemblyDescriptionAttribute;
            this.Text = desc.Description + " v" + a.GetName().Version.ToString();
        }

        private static string GetPageMarkup(string uri)
        {
            string pageData = null;
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                pageData = client.DownloadString(uri);
            }
            return pageData;
        }


        private System.Windows.Forms.TabPage CreateNewTabPage()
        {
            var rtb = new Ionic.WinForms.RichTextBoxEx();

            rtb.BackColor = System.Drawing.SystemColors.Window;
            rtb.DetectUrls = false;
            rtb.Dock = System.Windows.Forms.DockStyle.Fill;
            rtb.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            rtb.Location = new System.Drawing.Point(3, 3);
            rtb.Name = "richTextBox1";
            rtb.Size = new System.Drawing.Size(510, 166);
            rtb.TabIndex = 80;
            rtb.Text = "";
            rtb.NumberColor = Color.FromName("DarkGray");
            rtb.NumberBackground1 = System.Drawing.SystemColors.ControlLight;
            rtb.NumberBackground2 = System.Drawing.SystemColors.Window;
            rtb.NumberBorder = System.Drawing.SystemColors.ControlDark;
            rtb.NumberColor = System.Drawing.SystemColors.ControlDark;
            rtb.ShowLineNumbers = true;

            rtb.Leave += new System.EventHandler(this.richTextBox1_Leave);
            rtb.MouseUp += new System.Windows.Forms.MouseEventHandler(this.richTextBox1_MouseUp);

            rtb.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.richTextBox1_KeyPress);

            this.richTextBox1 = rtb;

            var tabPage1 = new System.Windows.Forms.TabPage();
            tabPage1.Controls.Add(rtb);
            tabPage1.Location = new System.Drawing.Point(4, 19);
            tabPage1.Padding = new System.Windows.Forms.Padding(3);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "";
            tabPage1.UseVisualStyleBackColor = true;
            tabPage1.Tag = new TabState { status = "", tabNumber = tn++ };

            this.customTabControl1.Controls.Add(tabPage1);
            this.customTabControl1.SelectTab(tabPage1);

            this.toolTip1.SetToolTip(richTextBox1, "");
            _lastRtbKeyPress = _originDateTime;

            //this.label3.SendToBack();

            return tabPage1;
        }



        private void btnLoadXml_Click(object sender, EventArgs e)
        {
            if (fileToLoad == null) return;
            TabPage tp = null;
            Trace("LoadXml_Click");
            try
            {
                isLoading = true;
                tp = CreateNewTabPage();
                Ionic.User32.BeginUpdate(this.customTabControl1.Handle);
                this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
                if (this.fileToLoad.StartsWith("http://") || this.fileToLoad.StartsWith("https://"))
                {
                    this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
                    richTextBox1.Text = GetPageMarkup(this.fileToLoad);
                    tabState.okToSave = false;
                    var segs = this.fileToLoad.Split("/".ToCharArray());
                    tp.Text = "  " + segs[segs.Length - 1] + "  ";
                    this.Cursor = System.Windows.Forms.Cursors.Default;
                    _fileHistory.Store(fileToLoad);
                }
                else
                {
                    richTextBox1.Text = File.ReadAllText(this.fileToLoad);
                    tabState.okToSave = true;
                    tp.Text = "  " + Path.GetFileName(this.fileToLoad) + "  ";
                    _fileHistory.Store(fileToLoad);
                }

                tabState.src = this.fileToLoad;
                richTextBox1.Select(0, 0);
                wantFormat.Set();
                DisableMatchButtons();
                PreloadXmlns();

                // auto-evaluate any xpath that is present
                tbXpath_TextChanged(null, null);
            }
            catch (Exception exc1)
            {
                Trace("Exception while loading: {0}", exc1.Message);
                richTextBox1.Text = "file read error:  " + exc1.ToString();
                UpdateStatus("Cannot read that file.");
                if (tabPage != null)
                    tabPage.Text = "  error  ";
            }
            finally
            {
                isLoading = false;
                this.Cursor = System.Windows.Forms.Cursors.Default;
                Ionic.User32.EndUpdate(this.customTabControl1.Handle);
            }
        }



        private void richTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            tabState.nav = null;
            _lastRtbKeyPress = System.DateTime.Now;
            if (richTextBox1.Text.Length == 0) return;
            DisableMatchButtons();
            RemoveHighlighting();
            wantFormat.Set();

            // xxx also set a timer to insure we re-apply the XPath expression
            // when done fiddling with the buffer. ? maybe
        }



        private void richTextBox1_Leave(object sender, EventArgs e)
        {
            IntPtr mask = IntPtr.Zero;
            try
            {
                // In case the set of XML namespaces changes,
                // must redisplay everything.
                PreloadXmlns();
            }
            catch (Exception exc1)
            {
                UpdateStatus("Cannot process that XML. ({0})", exc1.Message);
            }
        }


        private void PreloadXmlns()
        {
            try
            {
                // preserve the old table, to reuse prefixes in case
                // they have been changed.
                var oldns = new XmlnsTable(xmlNamespaces);

                // start from scratch on this table
                xmlNamespaces.Clear();
                int c = 1;

                // get all xml-ns decls from the document
                XPathNodeIterator list = nav.Select("//namespace::*[name() != 'xml'][not(../../namespace::*=.)]");
                while (list.MoveNext())
                {
                    XPathNavigator nsNode = list.Current;
                    if (nsNode.NodeType == XPathNodeType.Namespace)
                    {
                        string ns = nsNode.Value;

                        if (!xmlNamespaces.ContainsNs(ns))
                        {
                            // get the prefix - it's either empty or not
                            string origPrefix = nsNode.LocalName;

                            // make sure the prefix is unique
                            int dupes = 0;
                            string actualPrefix = origPrefix;
                            while (actualPrefix == "" || xmlNamespaces.ContainsPrefix(actualPrefix))
                            {
                                if (oldns.ContainsNs(ns) && actualPrefix == "")
                                {
                                    // reuse
                                    actualPrefix = oldns.FindNs(ns).Prefix;
                                }
                                else
                                {
                                    actualPrefix = (origPrefix == "")
                                        ? String.Format("ns{0}", c++)
                                        : String.Format("{0}-{1}", origPrefix, dupes++);
                                }
                            }

                            xmlNamespaces.Add(new XmlnsInfo(actualPrefix, ns, origPrefix=="", (origPrefix == "" && xmlNamespaces.Default == null)));
                        }
                    }
                }
                DisplayXmlnsPrefixList();
                UpdateStatus("OK.");
            }
            catch (System.Exception exc1)
            {
                UpdateStatus("Cannot parse: " + exc1.Message);
            }
        }


        private void AdjustSplitterSize()
        {
            this.splitContainer3.SplitterDistance = this.splitContainer3.Panel1MinSize;
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.SaveFormToRegistry();
        }


        private void ClearTabs()
        {
            while (this.customTabControl1.TabCount > 0)
            {
                var tb = this.customTabControl1.TabPages[this.customTabControl1.TabCount - 1];
                this.customTabControl1.TabPages.Remove(tb);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.FillFormFromRegistry();
            ClearTabs();
            CollapseXmlnsPrefixPanel();

            // process command line
            var args = Environment.GetCommandLineArgs();
            if (args != null && args.Length > 1 && File.Exists(args[1]))
            {
                Trace("Command line args: '{0}' '{1}'", args[0], args[1]);
                // open the specified file.
                this.fileToLoad= args[1];
                btnLoadXml_Click(null, null);
                //this.label3.SendToBack();  // un-obscure the richtextbox
                richTextBox1.Visible = true;
            }
            else
            {
                //this.label3.BringToFront();
                this.progressBar1.Visible = false;
            }
            UpdateStatus("Ready");
        }




        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        private static extern bool
            PathCompactPathEx(StringBuilder pszOut,
                              string pszPath,
                              int cchMax,
                              int reserved);


        private string GetShortDisplayNameForRecentFiles(string longName, int maxLen)
        {
            var sb1 = new StringBuilder(maxLen + maxLen + 2);

            // if (longName.StartsWith("http"))
            // {
            //     if (longName.Length <= maxLen)
            //     {
            //         return longName;
            //     }
            //
            //     sb1.Append(longName.Substring(0, maxLen-3))
            //         .Append("...");
            //     return sb1.ToString();
            // }
            // else
            {
                if ( PathCompactPathEx(sb1, longName, maxLen, 0) )
                    return sb1.ToString();
                return longName;
            }
        }

        private string GetDisplayNameForRecentFiles(string fullName)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            int maxDisplayLength = 40;      // maximum length of file name for display

            if (!fullName.StartsWith("http"))
            {
                // if file is in current directory, show only file name
                FileInfo fileInfo = new FileInfo(fullName);

                if ( fileInfo.DirectoryName == currentDirectory )
                    return GetShortDisplayNameForRecentFiles(fileInfo.Name, maxDisplayLength);
            }

            return GetShortDisplayNameForRecentFiles(fullName, maxDisplayLength);
        }



        private void RecentFile_Clicked(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            if (item == null) return;
            string fname = item.Tag as string;
            if (fname == null) return;

            var selection = from TabPage tp in customTabControl1.TabPages
                where ((TabState)tp.Tag).src == fname
                select tp;

            if (selection.Count()!= 0)
            {
                // a tab for that file is already open
                this.customTabControl1.SelectTab(selection.First());
                return;
            }

            this.fileToLoad = fname;
            if (fileToLoad.StartsWith("http"))
                btnLoadXml_Click(sender, e);
            else if (System.IO.File.Exists(this.fileToLoad))
                btnLoadXml_Click(sender, e);
        }


        private void tsmiEdit_Opening(object sender, EventArgs e)
        {
            if (tabState == null)
            {
                tsmiReindent.Enabled = false;
                tsmiExtractHighlighted.Enabled = false;
                tsmiRemoveSelected.Enabled = false;
                tsmiStripNamespaces.Enabled = false;
                tsmiCopy.Enabled = false;
                tsmiCopyAll.Enabled = false;
                tsmiPaste.Enabled = false;
            }
            else if (tabState.matches == null || tabState.matches.Count == 0)
            {
                tsmiReindent.Enabled = true;
                tsmiExtractHighlighted.Enabled = false;
                tsmiRemoveSelected.Enabled = false;
                tsmiStripNamespaces.Enabled = true;
                tsmiCopy.Enabled = true;
                tsmiCopyAll.Enabled = true;
                tsmiPaste.Enabled = true;
            }
            else
            {
                tsmiReindent.Enabled = true;
                tsmiExtractHighlighted.Enabled = true;
                tsmiRemoveSelected.Enabled = true;
                tsmiStripNamespaces.Enabled = true;
                tsmiCopy.Enabled = true;
                tsmiCopyAll.Enabled = true;
                tsmiPaste.Enabled = true;
            }
        }


        private void tsmiFile_Opening(object sender, EventArgs e)
        {
            tsmiOpenRecent.DropDownItems.Clear();
            var mruList = _fileHistory.GetList();

            // Disable the "open recent" menu item if MRU list is empty
            if (mruList.Count == 0)
            {
                tsmiOpenRecent.Enabled = false;
            }
            else
            {
                tsmiOpenRecent.Enabled = true;

                foreach (var fname in mruList)
                {
                    var item = new ToolStripMenuItem(GetDisplayNameForRecentFiles(fname));
                    item.Tag = fname;
                    item.Click += new EventHandler(this.RecentFile_Clicked);
                    tsmiOpenRecent.DropDownItems.Add(item);
                }
            }

            if (tabPage == null)
            {
                tsmiSaveAs.Enabled = false;
                tsmiSave.Enabled = false;
            }
            else
            {
                tsmiSaveAs.Enabled = true;
                tsmiSave.Enabled = (tabState != null) && (tabState.src != "");
            }
        }



        // workitem 6517
        private void CheckXpathSyntax()
        {
            if (this.tbXpath.InvokeRequired)
            {
                this.tbXpath.Invoke(new Action(this.CheckXpathSyntax));
            }
            else
            {
                int ss = this.tbXpath.SelectionStart;
                int sl = this.tbXpath.SelectionLength;
                // get the text.
                string xpathExpr = this.tbXpath.Text; //Trim();
                // put it back.
                // why? because it's possible the user has pasted in RTF, which won't
                // show up correctly in that one-line RichTextBox. If we paste it here,
                // it will be properly formatted.
                this.tbXpath.Text = xpathExpr;
                this.tbXpath.SelectAll();
                this.tbXpath.SelectionColor = Color.Black;
                this.tbXpath.SelectionFont = this.tbXpath.Font;
                this.tbXpath.Select(ss, sl);
                try
                {
                    XElement xe = xpathParser.Parse(xpathExpr, new XPathTreeBuilder());
                    this.toolTip1.SetToolTip(this.tbXpath, "enter an XPath expression");

                    // the parse succeeded, evaluate it
                    EvalXpath();
                }
                catch (XPathParserException exc1)
                {
                    this.tbXpath.Select(exc1.ErrorStart, exc1.ErrorEnd - exc1.ErrorStart);
                    this.tbXpath.SelectionColor = Color.Red;
                    this.tbXpath.Select(ss, sl);
                    this.toolTip1.SetToolTip(this.tbXpath, exc1.Message);
                    UpdateStatus("--");
                }

                _handlingXpathChangeEvents--;
            }
        }

        // workitem 6517
        private void MaybeAutoEvaluate(Object ignored)
        {
            _handlingXpathChangeEvents++;
            System.Threading.Thread.Sleep(DELAY_IN_MILLISECONDS);
            System.DateTime now = System.DateTime.Now;
            var delta = now - _lastXpathChange;
            if (delta < new System.TimeSpan(0, 0, 0, 0, DELAY_IN_MILLISECONDS))
            {
                _handlingXpathChangeEvents--;
                return;  // still typing
            }

            CheckXpathSyntax();
        }


        private void tbXpath_TextChanged(object sender, EventArgs e)
        {
            if (_handlingXpathChangeEvents > 0) return; // already looking
            _lastXpathChange = System.DateTime.Now;
            _keepCursorPosition = (sender == null);
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(MaybeAutoEvaluate));
        }



        private XmlNamespaceManager GetXmlNamespaceManager()
        {
            var xmlns = new XmlNamespaceManager(nav.NameTable);
            foreach (XmlnsInfo item in xmlNamespaces)
            {
                // XPath 1.0 doesn't support "default" namespaces in xpath queries.
                // see http://www.w3.org/TR/1999/REC-xpath-19991116/#node-tests
                // if (prefix == _xmlnsDefaultPrefix)
                //    xmlns.AddNamespace("", xmlNamespaces[prefix]);
                xmlns.AddNamespace(item.Prefix, item.Ns);
            }
            return xmlns;
        }




        string FixupXpathExpressionWithDefaultNamespace(string expr)
        {
            XmlnsInfo def = tabState.xmlns.Default;
            if (def == null) return expr;

            string prefix = def.Prefix;

            string s = expr;
            s = Regex.Replace(s, "^(?!::)([^/:]+)(?=/)", prefix + ":$1");                             // beginning
            s = Regex.Replace(s, "/([^/:\\*\\(]+)(?=[/\\[])", "/" + prefix + ":$1");                  // segment
            s = Regex.Replace(s, "::([A-Za-z][^/:*]*)(?=/)", "::" + prefix + ":$1");                  // axis specifier
            s = Regex.Replace(s, "\\[([A-Za-z][^/:*\\(]*)(?=[\\[\\]])", "[" + prefix + ":$1");        // within predicate
            s = Regex.Replace(s, "/([A-Za-z][^/:\\*\\(]*)(?!<::)$", "/"+ prefix+":$1");               // end
            s = Regex.Replace(s, "^([A-Za-z][^/:]*)$", prefix + ":$1");                               // edge case
            s = Regex.Replace(s, "([A-Za-z][-A-Za-z]+)\\(([^/:\\.,\\(\\)]+)(?=[,\\)])", "$1(" + prefix + ":$2"); // xpath functions

            return s;
        }


        private void RemoveHighlighting()
        {
            richTextBox1.BeginUpdateAndSaveState();
            richTextBox1.SelectAll();
            richTextBox1.SelectionBackColor = Color.White;
            richTextBox1.Update();
            richTextBox1.EndUpdateAndRestoreState();
        }


        private void EvalXpath()
        {
            List<Tuple<int, int>> matches = null;

            DisableMatchButtons();
            string xpathExpression = this.tbXpath.Text;
            if (String.IsNullOrEmpty(xpathExpression))
            {
                UpdateStatus("Cannot evaluate: There is no XPath expression.");
                return;
            }

            string rtbText = richTextBox1.Text;
            if (String.IsNullOrEmpty(rtbText))
            {
                UpdateStatus("Cannot evaluate: There is no XML document.");
                return;
            }

            IntPtr mask = IntPtr.Zero;
            string elaboratedXpathExpression = null;
            try
            {
                // reset highlighting
                RemoveHighlighting();

                mask = richTextBox1.BeginUpdateAndSuspendEvents();
                this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
                this.tbXpath.BackColor = this.tbXmlns.BackColor; // just in case

                XmlNamespaceManager xmlns = GetXmlNamespaceManager();

                elaboratedXpathExpression = FixupXpathExpressionWithDefaultNamespace(xpathExpression).Trim();

                XPathNodeIterator selection = nav.Select(elaboratedXpathExpression, xmlns);

                if (selection == null || selection.Count == 0)
                {
                    if (elaboratedXpathExpression.Length < 64)
                        UpdateStatus(elaboratedXpathExpression + ": Zero nodes selected");
                    else
                        UpdateStatus("Zero nodes selected");

                    var s = String.Format("{0}\nZero nodes selected",
                                          elaboratedXpathExpression);
                    this.toolTip1.SetToolTip(richTextBox1, s);
                }
                else
                {
                    var s = String.Format("{0}\n{1} {2} selected",
                                          elaboratedXpathExpression,
                                          selection.Count, (selection.Count == 1) ? "node" : "nodes");
                    this.toolTip1.SetToolTip(richTextBox1, s);

                    if (elaboratedXpathExpression.Length < 64)
                        UpdateStatus("{0}: {1} {2} selected",
                                     elaboratedXpathExpression,
                                     selection.Count, (selection.Count == 1) ? "node" : "nodes");
                    else
                        UpdateStatus("{0} {1} selected",
                                     selection.Count, (selection.Count == 1) ? "node" : "nodes");
                    matches = HighlightSelection(selection, xmlns);

                    // remember the successful xpath queries
                    RememberInMruList(_xpathExpressionMruList, xpathExpression);
                }
            }
            catch (Exception exc1)
            {
                string brokenPrefix = IsUnknownNamespacePrefix(exc1);
                if (brokenPrefix != null)
                {
                    int ix = this.tbXpath.Text.IndexOf(brokenPrefix);
                    if (ix == -1)
                    {
                        MessageBox.Show(exc1.Message + "\nxpath: " + elaboratedXpathExpression,
                                    "Exception while evaluating XPath",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Exclamation);
                    }
                    else
                    {
                        this.tbXpath.Select(ix, brokenPrefix.Length);
                        this.tbXpath.BackColor = this.badXpathColor;
                        this.tbXpath.Focus();
                        UpdateStatus("Exception: " + exc1.Message);
                    }
                }
                else if (BadExpression(exc1))
                {
                    this.tbXpath.SelectAll();
                    this.tbXpath.BackColor = this.badXpathColor;
                    this.tbXpath.Focus();
                    UpdateStatus("Exception: " + exc1.Message);
                }
                else
                {
                    MessageBox.Show(exc1.Message + "\nxpath: " + elaboratedXpathExpression,
                                    "Exception while evaluating XPath",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Exclamation);
                }
            }

            finally
            {
                this.Cursor = System.Windows.Forms.Cursors.Default;
                richTextBox1.EndUpdateAndResumeEvents(mask);
            }

            tabState.matches = matches;
            tabState.xpath = this.tbXpath.Text;
            tabState.currentMatch = 0;
            EnableMatchButtons();
            if (_keepCursorPosition) // workitem 7432
            {
                RestoreCaretPosition();
                UpdateMatchCount();
            }
            else
            {
                scrollToCurrentMatch(-1);
            }
        }


        private void RememberInMruList(System.Windows.Forms.AutoCompleteStringCollection list, string value)
        {
            if (list.Contains(value))
                list.Remove(value);

            else if (list.Count >= _MaxMruListSize)
                list.RemoveAt(0);

            list.Add(value);
        }



        /// <summary>
        ///   Highlights the selected nodes in the XML RichTextBox, given the XPathNodeIterator.
        /// </summary>
        /// <remarks>
        ///   This finishes pretty quickly, no need to do it asynchronously.
        /// </remarks>
        /// <param name="selection">the node-set selection</param>
        /// <param name="xmlns">you know</param>
        /// <returns>
        ///   a list of match positions, each of which is a pair of
        ///   ints, describing the begin and end of the match.
        /// </returns>
        private List<Tuple<int, int>> HighlightSelection(XPathNodeIterator selection, XmlNamespaceManager xmlns)
        {
            var mp = ComputePositionsOfSelection(selection, xmlns);

            foreach (var t in mp)
            {
                // do the highlight
                richTextBox1.Select(t.V1, t.V2 - t.V1 + 1);
                richTextBox1.SelectionBackColor = this.matchingEltColor;
            }
            return mp;
        }




        /// <summary>
        /// Computes the positions of the selected nodes in the XML RichTextBox,
        /// given the XPathNodeIterator.
        /// </summary>
        /// <remarks>
        /// This finishes pretty quickly, no need to do it asynchronously.
        /// </remarks>
        /// <param name="selection">the node-set selection</param>
        /// <param name="xmlns">you know</param>
        /// <returns>
        ///   a list of match positions, each of which is a pair of
        ///   ints, describing the begin and end of the match.
        /// </returns>
        private List<Tuple<int, int>> ComputePositionsOfSelection(XPathNodeIterator selection, XmlNamespaceManager xmlns)
        {
            var lc = new LineCalculator(richTextBox1);
            var matchPositions = new List<Tuple<int, int>>();

            // get Text once (it's expensive)
            string rtbText = richTextBox1.Text;
            foreach (XPathNavigator node in selection)
            {
                IXmlLineInfo lineInfo = node as IXmlLineInfo;
                if (lineInfo == null || !lineInfo.HasLineInfo()) continue;

                int ix = lc.GetCharIndexFromLine(lineInfo.LineNumber - 1) +
                    lineInfo.LinePosition - 1 - 1;

                if (ix >= 0)
                {
                    int ix2 = 0;
                    //System.Diagnostics.Debugger.Break();
                    if (node.NodeType == XPathNodeType.Comment)
                    {
                        ix2 = ix + node.Value.Length;
                        ix++;
                    }

                    else if (node.NodeType == XPathNodeType.Text)
                    {
                        //string s = node.Value.XmlEscapeQuotes();
                        //
                        // workitem 6714
                        //
                        // why?  XmlEscapeQuotes just replaces " with
                        // &quot; which screws up the length. Why would
                        // I want to do that?  The only reason: if I am
                        // RE-escaping, in other words if there are
                        // already quotes in the original string. Hmmmm...
                        // Would the node.Value not show that?  I would think so.
                        // The upshot is, I don't know why I would call XmlEscapeQuotes().
                        // It seems to break on my test case.
                        string s = node.Value;
                        ix2 = ix + s.Length;
                        ix++;
                    }
                    else if (node.NodeType == XPathNodeType.Attribute)
                    {
                        string s = node.Value.XmlEscapeQuotes();
                        ix++;
                        ix2 = ix + node.Name.Length + 1;
                        char c = ' ';
                        while (rtbText[ix2] != '\'' && rtbText[ix2] != '"')
                            ix2++;
                        c = rtbText[ix2];
                        ix2++;
                        while (rtbText[ix2] != c) // the matching quote
                            ix2++;
                    }
                    else if (node.NodeType == XPathNodeType.Element)
                    {
                        //System.Diagnostics.Debugger.Break();
                        if (node.MoveToNext())
                        {
                            // The navigator moved to the succeeding
                            // element. Now backup through the text to find
                            // the ending square bracket for *this* element.
                            ix2 = lc.GetCharIndexFromLine(lineInfo.LineNumber - 1) +
                                lineInfo.LinePosition - 1;
                            var c1 = rtbText[ix2];
                            while (c1 != '>' && ix2 > ix)
                            {
                                ix2--;
                                c1 = rtbText[ix2];
                            }
                        }
                        else
                        {
                            // There is no succeeding (sibling) element.
                            //
                            // Manual Labor. Since there is no
                            // XPathNavigator.MoveToEndOfElement(), we look
                            // for the EndElement in the text.  First,
                            // advance past the original node name.  Then,
                            // advance past any child attributes and values,
                            // to either a / or a >.
                            //

                            //
                            // If the succeeding char is not / (meaning an
                            // empty element), then look for the </NodeName>
                            // string.

                            // BUG: this is naive; I think it will break if
                            // there's a > or / inside an attribute value.

                            ix2 = ix + node.Name.Length + 1;
                            var c1 = rtbText[ix2];
                            while (c1 != '>' && c1 != '/')
                            {
                                ix2++;
                                c1 = rtbText[ix2];
                            }
                            if (rtbText[ix2] == '/')
                            {
                                // we're at the end-element
                                ix2++;
                            }
                            else
                            {
                                string subs1 = String.Format("</{0}>", node.Name);
                                int ix3 = rtbText.IndexOf(subs1, ix2);
                                if (ix3 > 0)
                                {
                                    ix2 = ix3 + subs1.Length;
                                }
                                else
                                {
                                    ix2 = rtbText.IndexOf('>', ix2);
                                }
                            }
                        }
                    }

                    // do we need to remember this one?
                    if (ix2 >= ix) // workitem 7427
                    {
                        // Record the location of the match within the doc.
                        Trace("match({0},{1})", ix, ix2);
                        matchPositions.Add(Tuple.New(ix, ix2));
                    }
                }
            }

            return matchPositions;
        }



        /// <summary>
        /// Re-formats (Indents) the text in the XML RichTextBox
        /// </summary>
        /// <remarks>
        /// This finishes pretty quickly, no need to do it asynchronously.
        /// </remarks>
        private void IndentXml()
        {
            String origText = richTextBox1.Text;
            try
            {
                richTextBox1.BeginUpdate();
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.XmlResolver = new Ionic.Xml.XhtmlResolver();
                doc.LoadXml(origText);
                var builder = new System.Text.StringBuilder();
                var settings = new System.Xml.XmlWriterSettings
                    {
                        // OmitXmlDeclaration = true,
                        Indent = true,
                        IndentChars = "  "
                    };

                using (var writer = System.Xml.XmlWriter.Create(builder, settings))
                {
                    doc.Save(writer);
                }
                richTextBox1.Text = builder.ToString();
                richTextBox1.SelectAll();
                richTextBox1.SelectionColor = Color.Black;
                richTextBox1.Select(0, 0); // top of file

                tabState.nav = null; // The spacing changed; invalidate the cached doc.
                wantFormat.Set();
                tabState.matches = null;
                DisableMatchButtons();
                PreloadXmlns();
                UpdateStatus("Formatted.");
            }
            catch (System.Exception exc1)
            {
                // maybe invalid XML...
                richTextBox1.Text = origText;
                UpdateStatus("Exception while loading: " + exc1.Message);
            }
            finally
            {
                richTextBox1.EndUpdate();
            }
        }



        private void labelAsHyperlink_Click(object sender, EventArgs e)
        {
            ToolStripStatusLabel lbl = sender as ToolStripStatusLabel;
            if (lbl == null) return;
            var t= lbl.Text;
            if (!t.StartsWith("http:/") && !t.StartsWith("https:/"))
                t = "http://" + t;
            System.Diagnostics.Process.Start(t);
        }


        private static System.Text.RegularExpressions.Regex re1 =
            new System.Text.RegularExpressions.Regex("Namespace prefix '(.+)' is not defined");

        private string IsUnknownNamespacePrefix(Exception exc1)
        {
            var match = re1.Match(exc1.ToString());
            if (match != null && match.Captures != null && match.Captures.Count != 0)
                return match.Groups[1].Value.ToString();
            return null;
        }


        private bool BadExpression(Exception exc1)
        {
            return exc1.Message.Contains("Expression must evaluate to a node-set");
        }



        private void btnAddNsPrefix_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(this.tbPrefix.Text) && !String.IsNullOrEmpty(this.tbXmlns.Text))
            {
                if (xmlNamespaces.ContainsPrefix(tbPrefix.Text))
                {
                    // Bzzt!
                    this.tbPrefix.SelectAll();
                    this.tbPrefix.BackColor = Color.FromArgb((Color.Red.A << 24) | 0xFFDEAD);
                    this.tbPrefix.Focus();
                }
                else
                {
                    // add it to the list of prefixes, and display the list
                    xmlNamespaces.Add(new XmlnsInfo(tbPrefix.Text, tbXmlns.Text, true));
                    DisplayXmlnsPrefixList();
                    this.tbPrefix.Text = "";
                    this.tbXmlns.Text = "";
                    this.tbPrefix.Focus();
                    this.tbXpath.BackColor = this.tbXmlns.BackColor;
                }
            }
        }


        private void RemovePrefix(string k)
        {
            if (xmlNamespaces.ContainsPrefix(k))
            {
                Trace("RemovePrefix({0})", k);
                xmlNamespaces.RemoveItemWithPrefix(k);
                DisplayXmlnsPrefixList();
            }
        }


        private void ClickXmlns(object sender, string k)
        {
            var chk = sender as System.Windows.Forms.CheckBox;
            xmlNamespaces.ClearDefault(); // always clear
            if (chk.Checked)
            {
                if (xmlNamespaces.ContainsPrefix(k))
                {
                    xmlNamespaces.FindPrefix(k).IsDefault = true;

                    // unset checkbox for all others, like a radio button
                    foreach (Control c in this.groupBox1.Controls)
                    {
                        var chk2 = c as System.Windows.Forms.CheckBox;
                        if (chk2 != null && chk2 != sender)
                        {
                            chk2.Checked = false;
                        }
                    }
                }
            }
        }


        private void XPathVisualizerTool_Resize(object sender, EventArgs e)
        {
            AdjustSplitterSize();
        }


        private void tbPrefix_TextChanged(object sender, EventArgs e)
        {
            if (this.tbPrefix.BackColor != this.tbXmlns.BackColor)
            {
                this.tbPrefix.BackColor = this.tbXmlns.BackColor;
            }
        }

        private void textBox1_Validating(object src,
                                System.ComponentModel.CancelEventArgs e)
        {
            var tb = src as TextBox;
            // check for leagl xmlns name
            if (!Regex.IsMatch(tb.Text, "^([a-zA-Z][a-zA-Z0-9]*)?$"))
                e.Cancel = true;
        }

        private const int XmlNsPanelDeltaY = 20;
        private void DisplayXmlnsPrefixList()
        {
            // NB: the textbox1_Leave event can call PreloadXmlns, which
            // can lead to infinite recursion if we're not careful.

            if (isDisplayingXmlnsPanel) return; // prevent recursion

            isDisplayingXmlnsPanel = true;
            Trace("DisplayXmlnsPrefixList isExpanded({0})",
                  (tabState!=null)
                  ? tabState.xmlnsTableIsExpanded.ToString()
                  : "no");

            int offsetX = 2;  // greater is further right
            int offsetY = 16 + this.tbXmlns.Size.Height;  // greater implies further down
            try
            {
                var keypressed = new KeyPressEventHandler( (src,e) => {
                        // If the ENTER key is pressed, the Handled
                        // property is set to true, to indicate the
                        // event is handled.
                        var tb = src as TextBox;
                        var chk = tb.Tag as CheckBox;
                        var item = chk.Tag as XmlnsInfo;
                        this.toolTip1.SetToolTip(tb, "<Enter> to accept");
                        if (e.KeyChar == (char)Keys.Return)
                        {
                            Trace("<ENTER> in textbox Text='{0}'=>'{1}'",
                                  item.Prefix, tb.Text );
                            chk.Focus();
                            e.Handled = true;
                            return;
                        }
                        if (e.KeyChar == (char)Keys.Escape)
                        {
                            tb.Text = item.Prefix; // revert the textbox
                            this.toolTip1.SetToolTip(tb, "click to modify");
                            tb.BackColor = System.Drawing.Color.White;
                            chk.Focus();
                            e.Handled = true;
                            return;
                        }
                        Trace("KeyPress in textbox Text='{0}'", tb.Text);
                    });
                var tbGotFocus = new EventHandler( (src, e) => {
                        var tb = src as TextBox;
                        Trace("Textbox GotFocus Text='{0}'", tb.Text);
                        var ttip = this.toolTip1.GetToolTip(tb);
                        if (ttip != "duplicate prefix" &&
                            ttip != "illegal prefix")
                            this.toolTip1.SetToolTip(tb, "<Enter> to accept");
                        tb.BackColor =
                        System.Drawing.ColorTranslator.FromHtml("#FFF0F5"); // Light Pink
                    });
                var checkTicked = new EventHandler( (src, e) => {
                        var item = ((CheckBox)src).Tag as XmlnsInfo;
                        ClickXmlns(src, item.Prefix);
                    });

                var tbXmlnsValidating = new System.ComponentModel.CancelEventHandler( (src,e) => {
                        var tb = src as TextBox;
                        Trace("Textbox Validating Text='{0}'", tb.Text);
                        // check for leagl xmlns name
                        if (!Regex.IsMatch(tb.Text, "^[a-zA-Z][a-zA-Z0-9]*$"))
                        {
                            Trace("Textbox Validation failed (regex)");
                            this.toolTip1.SetToolTip(tb, "illegal prefix");
                            e.Cancel = true;
                            return;
                        }

                        // check for duplicates
                        var chk = tb.Tag as CheckBox;
                        var item = chk.Tag as XmlnsInfo;
                        var item2 = xmlNamespaces.FindPrefix(tb.Text);

                        if (item2 == null)
                        {
                            Trace("  no existing ns with that prefix ({0})",
                                  tb.Text);
                        }
                        else if (item.Ns != item2.Ns)
                        {
                            Trace("Textbox Validation failed (duplicate)");
                            this.toolTip1.SetToolTip(tb, "duplicate prefix");
                            e.Cancel = true;
                            return;
                        }

                        Trace("Textbox Validated OK   Text='{0}'=>'{1}'",
                              tb.Text, item.Prefix);

                        item.Prefix = tb.Text; // update the prefix
                        this.toolTip1.SetToolTip(tb, "click to modify");
                        tb.BackColor = System.Drawing.Color.White;
                    });

                var buttonClicked = new EventHandler( (src, e) => {
                        var item = ((Button)src).Tag as XmlnsInfo;
                        RemovePrefix(item.Prefix);
                    });


                //this.BeginUpdate();
                this.SuspendLayout();
                this.groupBox1.SuspendLayout();

                this.groupBox1.Controls.Clear();
                this.groupBox1.Controls.Add(this.pnlInput);

                int count = 0;
                if (xmlNamespaces.Count > 0)
                {
                    // Add a set of controls to the panel for each
                    // key/value pair in the list
                    foreach (var item in xmlNamespaces)
                    {
                        // the leftmost textbox.  It holds the prefix name,
                        // and is conditionally readonly.  It is readonly if
                        // the xmlns prefix has not been contrived.
                        var tb1 = new TextBox
                            {
                                Anchor = (AnchorStyles)(AnchorStyles.Top |
                                     AnchorStyles.Left),
                                Location = new Point(this.tbPrefix.Location.X + offsetX,
                                                                    this.tbPrefix.Location.Y + offsetY + (count * XmlNsPanelDeltaY)),
                                Size = new Size(this.tbPrefix.Size.Width, this.tbPrefix.Size.Height),
                                Text = item.Prefix,
                                ReadOnly = !item.IsContrivedPrefix,
                                CausesValidation = true,
                                TabStop = false,
                            };
                        this.groupBox1.Controls.Add(tb1);
                        this.toolTip1.SetToolTip(tb1, "click to modify");
                        tb1.Validating += tbXmlnsValidating;
                        tb1.KeyPress += keypressed;
                        tb1.GotFocus += tbGotFocus;

                        // the first label.  It's an equals sign, indicating
                        // the prefix assigned to the xml namespace.
                        var lbl1 = new Label
                            {
                                AutoSize = true,
                                Location = new Point(this.tbXmlns.Location.X + offsetX - 14,
                                                                    this.tbXmlns.Location.Y + offsetY + (count * XmlNsPanelDeltaY)),
                                Size = new Size(24, 13),
                                Text = ":=",
                            };
                        this.groupBox1.Controls.Add(lbl1);

                        // second textbox.Holds the xml namespace
                        var tb2 = new TextBox
                            {
                                Anchor = (AnchorStyles)
                                    (AnchorStyles.Top | AnchorStyles.Left |
                                     AnchorStyles.Right),
                                Location = new Point(this.tbXmlns.Location.X + offsetX,
                                                     this.tbXmlns.Location.Y + offsetY + (count * XmlNsPanelDeltaY)),
                                Size = new Size(this.tbXmlns.Size.Width - 18, this.tbXmlns.Size.Height),
                                Text = item.Ns,
                                ReadOnly = true,
                                TabStop = false,
                            };
                        this.groupBox1.Controls.Add(tb2);

                        // checkbox to select the default namespace
                        var chk1 = new CheckBox
                            {
                                Anchor = (AnchorStyles)
                                    (AnchorStyles.Top | AnchorStyles.Right),
                                Location = new Point(this.tbXmlns.Location.X + offsetX +
                                                     this.tbXmlns.Size.Width - 14,
                                                     this.tbXmlns.Location.Y + offsetY + (count * XmlNsPanelDeltaY)),
                                Size = new Size(14, this.tbXmlns.Size.Height),
                                TabStop = true,
                                Checked = item.IsDefault
                            };
                        chk1.Tag = item;
                        chk1.Click += checkTicked;
                        this.toolTip1.SetToolTip(chk1, "checked: use as default ns\nin xpath queries");
                        this.groupBox1.Controls.Add(chk1);

                        // button to delete the namespace and its prefix
                        var btn1 = new Button
                            {
                                Anchor = (AnchorStyles)
                                    (AnchorStyles.Top | AnchorStyles.Right),
                                Location = new Point(this.btnAddNsPrefix.Location.X + offsetX,
                                                     this.btnAddNsPrefix.Location.Y + offsetY + (count * XmlNsPanelDeltaY)),
                                Size = new Size(this.btnAddNsPrefix.Size.Width,
                                                this.btnAddNsPrefix.Size.Height),
                                Text = "X",
                                UseVisualStyleBackColor = true,
                                TabStop = false,
                            };
                        btn1.Tag = item;
                        btn1.Click += buttonClicked;
                        this.toolTip1.SetToolTip(btn1, "remove this ns+prefix");
                        this.groupBox1.Controls.Add(btn1);
                        tb1.Tag = chk1; // associate checkbox to the textbox
                        count++;
                    }
                }

                ProperlyDisplayXmlnsPrefixPanel();

                this.groupBox1.ResumeLayout();
                this.ResumeLayout();
            }
            catch (Exception exc1)
            {
                MessageBox.Show(String.Format("There was a problem ! [problem={0}]",
                                              exc1.Message), "Whoops!", MessageBoxButtons.OK);
            }
            isDisplayingXmlnsPanel = false;
        }


        private void ProperlyDisplayXmlnsPrefixPanel()
        {
            if ((tabState == null) || (!tabState.xmlnsTableIsExpanded))
                CollapseXmlnsPrefixPanel();
            else
                ExpandXmlnsPrefixPanel();

        }

        private void ExpandXmlnsPrefixPanel()
        {
            int n = (this.groupBox1.Controls.Count -1 )/ 4;
            int offsetY = 2;  // greater implies larger panel

            //this.pnlPrefixList.Visible = true;
                    foreach (Control c in this.groupBox1.Controls)
                    {
                        c.Visible = true;
                    }

                    //this.pnlInput.Visible = true;
            btnExpandCollapse.ImageIndex = 0;
            this.toolTip1.SetToolTip(this.btnExpandCollapse, "Collapse");
            //this.splitContainer3.Panel1MinSize = originalPanel1MinSize + (XmlNsPanelDeltaY * n) + offsetY;
            //var sz = this.pnlPrefixList.Size;
            //this.pnlPrefixList.Size = new Size(sz.Width, 2 + (XmlNsPanelDeltaY * n) + offsetY);
            this.splitContainer3.Panel1MinSize = 70 + (XmlNsPanelDeltaY * n) + offsetY;
            this.splitContainer3.SplitterDistance = this.splitContainer3.Panel1MinSize;

            if (tabState != null)
            {
                tabState.xmlnsTableIsExpanded = true;
                Trace("tabstate({0}).xmlnsTableIsExpanded: {1}",
                      tabState.tabNumber, tabState.xmlnsTableIsExpanded);
            }

            // We don't need to explicitly set the size of the groupbox.  Groupbox1
            // is docked at the bottom of SplitContainer3.Panel1, so it grows as we
            // move the splitter.
        }


        private void CollapseXmlnsPrefixPanel()
        {
            //this.pnlPrefixList.Visible = false;
            foreach (Control c in this.groupBox1.Controls)
            {
                c.Visible = false;
            }

            //this.pnlInput.Visible = false;
            btnExpandCollapse.ImageIndex = 1;
            this.toolTip1.SetToolTip(this.btnExpandCollapse, "Expand");
            this.splitContainer3.Panel1MinSize = originalPanel1MinSize - (XmlNsPanelDeltaY);
            this.splitContainer3.SplitterDistance = this.splitContainer3.Panel1MinSize;
            if (tabState != null)
            {
                tabState.xmlnsTableIsExpanded = false;
                Trace("tabstate({0}).xmlnsTableIsExpanded: {1}",
                      tabState.tabNumber, tabState.xmlnsTableIsExpanded);
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            if (this.pnlInput.Visible == true)
                CollapseXmlnsPrefixPanel();
            else
                ExpandXmlnsPrefixPanel();
        }


        private void tsmiReindent_Click(object sender, EventArgs e)
        {
            IndentXml();
        }

        private void tsmiStripNamespaces_Click(object sender, EventArgs e)
        {
            StripXmlNamespaces();
        }

        /// <summary>
        /// Strips namespaces from the XML in the XML RichTextBox
        /// </summary>
        /// <remarks>
        /// This finishes pretty quickly, no need to do it asynchronously.
        /// </remarks>
        private void StripXmlNamespaces()
        {
            try
            {
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.LoadXml(richTextBox1.Text);

                var builder = new System.Text.StringBuilder();
                var settings = new System.Xml.XmlWriterSettings
                    {
                        //OmitXmlDeclaration = true,
                        Indent = true,
                        IndentChars = "  "
                    };

                using (var writer = new NoNamespaceXmlTextWriter(builder, settings))
                {
                    doc.Save(writer);
                }
                richTextBox1.Text = builder.ToString();
                tabState.nav = null; // invalidate the cached doc
                wantFormat.Set();
                tabState.matches = null;
                DisableMatchButtons();
                PreloadXmlns();
            }
            catch (System.Exception)
            {
                // illegal xml... do nothing
            }
        }

        private void tsmiCopyAll_Click(object sender, EventArgs e)
        {
            string txt = richTextBox1.Text;
            Clipboard.SetDataObject(txt, true);
        }

        private void tsmiCopy_Click(object sender, EventArgs e)
        {
            string txt = richTextBox1.SelectedText;
            Clipboard.SetDataObject(txt, true);
        }


        private void tsmiPaste_Click(object sender, EventArgs e)
        {
            string o = Clipboard.GetData(DataFormats.Text) as String;
            if (o != null)
            {
                richTextBox1.SelectedText = o;
                wantFormat.Set();
            }
        }


        private void DisableMatchButtons()
        {
            this.matchPanel.Visible = false;
            if (tabState != null)
                tabState.matches = null;
            this.lblMatch.Text = "";
            this.btn_NextMatch.Enabled = false;
            this.btn_PrevMatch.Enabled = false;
        }

        private void EnableMatchButtons()
        {
            if (tabState.matches != null && tabState.matches.Count > 0)
            {
                this.btn_NextMatch.Enabled = true;
                this.btn_PrevMatch.Enabled = true;
                //tabState.currentMatch = 0;
                tabState.numVisibleLines = richTextBox1.NumberOfVisibleLines;
                tabState.totalLinesInDoc = richTextBox1.Lines.Count();
                this.matchPanel.Visible = true;
            }
            else DisableMatchButtons();
        }


        private void UpdateMatchCount()
        {
            if (tabState.matches == null) return;
            this.lblMatch.Text = String.Format("{0}/{1}",
                                               tabState.currentMatch + 1, tabState.matches.Count);
        }

        private void scrollToCurrentMatch(int prior)
        {
            if (tabState.matches == null) return;
            if (tabState.matches.Count == 0) return;
            Tuple<int, int> position = tabState.matches[tabState.currentMatch];

            Trace("scrollToCurrentMatch(curmatch({0}) position({1},{2}))",
                  tabState.currentMatch, position.V1, position.V2);

            int startLine = richTextBox1.GetLineFromCharIndex(position.V1);

            Trace("scrollToCurrentMatch::startLine({0}) numVisibleLines({1})",
                  startLine, tabState.numVisibleLines);

            UpdateMatchCount();

            if (prior >= 0)
            {
                Tuple<int, int> p2 = tabState.matches[prior];
                richTextBox1.Select(p2.V1, p2.V2 - p2.V1 + 1);
                richTextBox1.SelectionBackColor = this.matchingEltColor;
            }
            richTextBox1.Select(position.V1, position.V2 - position.V1 + 1);
            richTextBox1.SelectionBackColor = this.kindaPink;

            // If the start line is in the middle of the doc...
            //if (startLine > totalLinesInDoc)
            if (startLine > tabState.numVisibleLines - 2)
            {
                // scroll so that the first line is 1/3 the way from the top
                int cix = richTextBox1.GetFirstCharIndexFromLine(startLine - tabState.numVisibleLines / 3 + 1);
                richTextBox1.Select(cix, cix + 1);
            }
            else
            {
                // set the selection at the very beginning
                richTextBox1.Select(0, 1);
            }
            richTextBox1.ScrollToCaret();

            // restore selection:
            richTextBox1.Select(position.V1, 0);
        }


        private void btn_NextMatch_Click(object sender, EventArgs e)
        {
            if (tabState.matches == null) return;
            int p = tabState.currentMatch;
            tabState.currentMatch++;
            if (tabState.currentMatch == tabState.matches.Count)
                tabState.currentMatch = 0;
            scrollToCurrentMatch(p);
        }

        private void btn_PrevMatch_Click(object sender, EventArgs e)
        {
            if (tabState.matches == null) return;
            int p = tabState.currentMatch;
            tabState.currentMatch--;
            if (tabState.currentMatch < 0)
                tabState.currentMatch = tabState.matches.Count - 1;
            Trace("currentMatch = {0}", tabState.currentMatch);
            scrollToCurrentMatch(p);
        }


        private void tbXpath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                EvalXpath();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.N)
            {
                btn_NextMatch_Click(sender, null);
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.P)
            {
                btn_PrevMatch_Click(sender, null);
                e.Handled = true;
            }
        }


        protected override void OnKeyUp(KeyEventArgs e)
        {
            // workitem 6720
            // if (e.Alt && e.KeyCode == Keys.F4)
            // {
            //     // do nothing - normal handling (form exit)
            // }
            // else if (e.Alt && e.KeyCode == Keys.F)
            // {
            // }
            // else if (e.Alt && e.KeyCode == Keys.E)
            // {
            // }
            // else if (e.Alt && e.KeyCode == Keys.H)
            // {
            // }
            // else if (e.Alt)

            if (e.KeyData == Keys.Menu)
            {
                // If the menu has just changed state, do not change back.
                // force a bit of a delay.
                if (this._lastEventWasAlt && this._intTicks <= 1)
                    return;

                this._intTicks = 0;
                // toggle
                if (menuStrip1.Visible)
                {
                    this._lastMenuItemIndex = menuStrip1.FindSelectedIndex();
                    this.menuStrip1.Visible = false;
                    this._lastEventWasAlt = false;
                }
                else
                {
                    if (this._lastMenuItemIndex == -1) this._lastMenuItemIndex = 0;
                    this.menuStrip1.Visible = true;
                    this.menuStrip1.Focus();
                    this.menuStrip1.Items[this._lastMenuItemIndex].Select();
                    this._lastEventWasAlt = true;
                }
                e.Handled = true;
                return;
            }
            this._lastEventWasAlt = false;
        }

        void DropDownOneSubmenu(int ix)
        {
            this.menuStrip1.Visible = true;
            this._lastMenuItemIndex = ix;
            this.menuStrip1.Focus();
            this.menuStrip1.Items[ix].Select();
            ((ToolStripDropDownItem) this.menuStrip1.Items[ix]).ShowDropDown();
            this._lastEventWasAlt = true;
        }

        // see http://stackoverflow.com/questions/2434834/processcmdkey-wait-for-keyup/2435190#2435190
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.N))
            {
                btn_NextMatch_Click(this, null);
                return true;
            }
            if (keyData == (Keys.Control | Keys.P))
            {
                btn_PrevMatch_Click(this, null);
                return true;
            }

            // Each of the next three cases handles a shortcut key for a
            // menu item.  The shortcuts don't "just work" when I do
            // auto-hiding of the menustrip.  So I have to pop the menustrip
            // and dropdown the menuitem explicitly.
            if (keyData == (Keys.Alt | Keys.F))
            {
                DropDownOneSubmenu(0);
                return true;
            }
            if (keyData == (Keys.Alt | Keys.E))
            {
                DropDownOneSubmenu(1);
                return true;
            }
            if (keyData == (Keys.Alt | Keys.H))
            {
                DropDownOneSubmenu(2);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }


        #if NOT
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Because Form.KeyPreview is true, this method gets invoked before
            // the KeyDown event is passed to the control with focus.  This way we
            // can handle keydown events on a form-wide basis.
            if (e.Control && e.KeyCode == Keys.N)
            {
                // ctrl-N - next match
                btn_NextMatch_Click(this, null);
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.P)
            {
                // ctrl-P - previous match
                btn_PrevMatch_Click(this, null);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Menu)
            {
                Console.WriteLine("KeyDown: ALT");
                e.Handled = true;
            }
        }
        #endif

        /// <summary>
        /// Deletes the selected nodes in the XML RichTextBox, given the XPathNodeIterator.
        /// </summary>
        /// <remarks>
        /// This finishes pretty quickly, no need to do it asynchronously.
        /// </remarks>
        private void DeleteSelection()
        {
            if (tabState.matches == null) return;

            int totalRemoved = 0;
            Trace("DeleteSelection(count({0}))", tabState.matches.Count);
            int count = 0;
            foreach (var t in tabState.matches)
            {
                // do the deletion
                Trace("DeleteSelection(match({0},{1}))", t.V1, t.V2);

                int start = t.V1 - totalRemoved;
                int length = t.V2 - t.V1 + 1;
                if (start < 0) continue;
                richTextBox1.Select(start, length);
                richTextBox1.SelectedText = "";
                totalRemoved += length;

                Trace("DeleteSelection(total({0})", totalRemoved);
                count++;
            }
            UpdateStatus("{0} nodes removed.", count);
            tabState.nav = null;
            wantFormat.Set();
            tabState.currentMatch = 0;
            tabState.matches = null;
            DisableMatchButtons();
        }


        private void tsmiRemoveSelected_Click(object sender, EventArgs e)
        {
            if (tabState.matches == null) return;
            DisableMatchButtons();

            IntPtr mask = IntPtr.Zero;
            try
            {
                mask = richTextBox1.BeginUpdateAndSuspendEvents();
                this.Cursor = System.Windows.Forms.Cursors.WaitCursor;

                DeleteSelection();

                // re-format (re-indent) the result
                IndentXml();
            }

            finally
            {
                this.Cursor = System.Windows.Forms.Cursors.Default;
                richTextBox1.EndUpdateAndResumeEvents(mask);
            }
        }


        private String ExtractSelection()
        {
            // This method is for workitem 4285
            if (tabState.matches == null) return null;

            string textExtracted = "";
            try
            {
                richTextBox1.BeginUpdateAndSaveState();
                Trace("ExtractSelection(count({0}))", tabState.matches.Count);
                int count = 0;
                foreach (var m in tabState.matches)
                {
                    // do the extraction
                    int start = m.V1;
                    int length = m.V2 - m.V1 + 1;
                    if (start < 0) continue;
                    Match match = null;
                    richTextBox1.Select(start, length);
                    var t = richTextBox1.SelectedText;
                    if (t.StartsWith("<")) // assume element node, or decl, etc
                        textExtracted += t;

                    else if ((match = Regex.Match(t, "^[ \\\t]*([^ \\\t=]+)[ \\\t]*=(.+)$")).Success) // attr node
                    {
                        var a = match.Groups[1].Value;
                        var attrname = Regex.Replace(a, "^[ \\\t]*([^ \\\t=]+)[ \\\t]*$", "$1");
                        textExtracted += "<" + attrname + ">" +
                            Regex.Replace(match.Groups[2].Value, "^[ \\\t]*([\\\"'])(.+)\\1[ \\\t]*$", "$2") +
                            "</" + attrname + ">\n";
                    }
                    else
                    {
                        // could be a text node, decl, comment, etc.
                        // Treat all as text strings.
                        textExtracted += "<text>" + t + "</text>";
                    }
                    count++;
                }

                UpdateStatus("{0} nodes extracted.", count);
            }
            finally
            {
                richTextBox1.EndUpdateAndRestoreState();
            }

            return textExtracted;
        }



        private String EnvelopeNodes(String t, XmlnsTable xmlns)
        {
            string nsDeclaration = "";
            foreach (var item in xmlns)
            {
                nsDeclaration +=
                String.Format(item.IsDefault
                              ? "xmlns='{1}'\n"
                              : "xmlns:{0}='{1}'\n",
                              item.Prefix, item.Ns);
            }
            return "<root " + nsDeclaration + ">" + t + "</root>";
        }


        private void tsmiExtractHighlighted_Click(object sender, EventArgs e)
        {
            TabPage tp = null;
            try
            {
                isLoading = true;
                var xmlns = new XmlnsTable(tabState.xmlns); // copy
                string text = ExtractSelection();
                if (text == null) return; // flash?
                text = EnvelopeNodes(text, xmlns);
                tp = CreateNewTabPage();
                tp.Text = "  extract " + (++extractCount) + "  ";
                richTextBox1.Text = text;
                tabState.xmlns = xmlns;
                IndentXml();
                tabState.src = "";
                tabState.okToSave = false;
                richTextBox1.Select(0, 0);
                wantFormat.Set();
                DisableMatchButtons();
                PreloadXmlns();
            }
            finally
            {
                isLoading = false;
            }
        }

        private void tsmiOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            // use the file MRU to get the most recent file that has been opened
            _fileHistory.ResetCursor();
            var f = _fileHistory.GetNext();

            if (f != null)
            {
                var d = System.IO.Path.GetDirectoryName(f);
                if (System.IO.Directory.Exists(d))
                    dlg.InitialDirectory = d;
            }

            dlg.Filter = "xml files|*.xml|All Files|*.*";
            dlg.FilterIndex = 1;
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                this.fileToLoad = dlg.FileName;
                if (System.IO.File.Exists(this.fileToLoad))
                    btnLoadXml_Click(sender, e);
            }
        }

        private void tsmiGet_Click(object sender, EventArgs e)
        {
            var getUrl = new Func<String>( () => {
                    // prompt for the url here
                    var f = new System.Windows.Forms.Form();
                    var btnOK = new System.Windows.Forms.Button();
                    var btnCancel = new System.Windows.Forms.Button();
                    var label = new System.Windows.Forms.Label();
                    var txt = new System.Windows.Forms.TextBox();
                    //
                    // tooltip
                    //
                    var tooltip = new System.Windows.Forms.ToolTip();
                    tooltip.AutoPopDelay = 2400;
                    tooltip.InitialDelay = 500;
                    tooltip.ReshowDelay = 500;
                    label.Text = "URL:";
                    label.AutoSize = true;
                    label.Location = new System.Drawing.Point(4, 6);
                    txt.Text = "";
                    txt.TabIndex = 11;
                    txt.Multiline = false;
                    txt.AutoCompleteMode = AutoCompleteMode.Suggest;
                    txt.AutoCompleteSource = AutoCompleteSource.HistoryList;
                    txt.Location = new System.Drawing.Point(54, 8);
                    txt.Size = new System.Drawing.Size(368, 28);
                    tooltip.SetToolTip(txt, "The URL to retrieve via HTTP GET");
                    btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                    btnCancel.Location = new System.Drawing.Point(354, 54);
                    btnCancel.Name = "btnCancel";
                    btnCancel.Size = new System.Drawing.Size(68, 23);
                    btnCancel.TabIndex = 71;
                    btnCancel.Text = "&Cancel";
                    btnCancel.UseVisualStyleBackColor = true;
                    btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
                    btnOK.Location = new System.Drawing.Point(278, 54);
                    btnOK.Name = "btnOK";
                    btnOK.Size = new System.Drawing.Size(68, 23);
                    btnOK.TabIndex = 61;
                    btnOK.Text = "&OK";
                    btnOK.UseVisualStyleBackColor = true;
                    f.Controls.Add(label);
                    f.Controls.Add(txt);
                    f.Controls.Add(btnOK);
                    f.Controls.Add(btnCancel);
                    f.Name = "HttpGet";
                    f.Text = "XPathVisualizer: Retrieve which URL?";
                    f.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                    f.MinimumSize = new System.Drawing.Size(442, 118);
                    f.MaximumSize = new System.Drawing.Size(442, 118);
                    var result = f.ShowDialog();

                    if (result == DialogResult.OK)
                    {
                        if (!String.IsNullOrEmpty(txt.Text))
                            return txt.Text;
                    }
                    return null;
                });

            var url = getUrl();

            if (url != null)
            {
                this.fileToLoad = url;
                btnLoadXml_Click(sender, e);
            }
        }



        private void tsmiSave_Click(object sender, EventArgs e)
        {
            if (tabState.okToSave)
            {
                File.WriteAllText(this.fileToLoad, richTextBox1.Text);
                UpdateStatus("Saved {0} lines to {1}",
                             this.richTextBox1.Lines.Length,
                             GetShortDisplayNameForRecentFiles(fileToLoad, 45));
            }
            else
            {
                tsmiSaveAs_Click(sender, e);
            }
        }


        private void tsmiExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void tsmiNew_Click(object sender, EventArgs e)
        {
            var tp = CreateNewTabPage();
            tp.Text = "  new  ";
            tabState.src = "";
            tabState.okToSave = false;
            DisableMatchButtons();
            PreloadXmlns();
        }


        private void tsmiSaveAs_Click(object sender, EventArgs e)
        {
            try
            {
                var getBaseName = new Func<String>( () => {
                        if (String.IsNullOrEmpty(this.fileToLoad))
                            return "new.xml";
                        if (this.fileToLoad.StartsWith("http"))
                        {
                            int ix = fileToLoad.LastIndexOf("/");
                            var b = fileToLoad.Substring(ix);
                            if (b.EndsWith(".xml"))
                                return b;
                            return b + ".xml";
                        }
                        return this.fileToLoad;
                    });

                var saveFname = getBaseName();

                var dlg1 = new SaveFileDialog
                    {
                        FileName = System.IO.Path.GetFileName(saveFname),
                        InitialDirectory = System.IO.Path.GetDirectoryName(saveFname),
                        OverwritePrompt = true,
                        Title = "XPath Visualizer: Where would you like to save the XML?",
                        Filter = "XML files|*.xml|All files (*.*)|*.*"
                    };

                var result = dlg1.ShowDialog();

                if (result == DialogResult.OK)
                {
                    this.fileToLoad = dlg1.FileName;
                    File.WriteAllText(this.fileToLoad, richTextBox1.Text);
                    tabState.okToSave = true;
                    tabPage.Text = "  " + Path.GetFileName(this.fileToLoad) + "  ";
                    tabState.src = fileToLoad;
                    _fileHistory.Store(fileToLoad);
                }
                UpdateStatus("Saved {0} lines to {1}",
                             this.richTextBox1.Lines.Length,
                             GetShortDisplayNameForRecentFiles(fileToLoad, 45));
            }
            catch (System.Exception exc1)
            {
                MessageBox.Show(exc1.Message,
                                "Exception while saving",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
            }
        }


        // see http://www.bobpowell.net/eventsubscribers.htm
        Delegate[] GetEventSubscribers(object target, string eventName)
        {
            string WinFormsEventName = "Event" + eventName;
            Type t = target.GetType();
            do
            {
                FieldInfo[] fia = t.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (FieldInfo fi in fia)
                {
                    if (fi.Name == eventName)
                    {
                        //we've found the compiler generated event
                        Delegate d = fi.GetValue(target) as Delegate;
                        if (d != null)
                            return d.GetInvocationList();
                    }

                    if (fi.Name == WinFormsEventName)
                    {
                        //we've found an EventHandlerList key
                        //get the list
                        var ehl = (System.ComponentModel.EventHandlerList)target.GetType().GetProperty("Events", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).GetValue(target,null);

                        //and dereference the delegate.
                        Delegate d = ehl[fi.GetValue(target)];
                        if (d != null)
                            return d.GetInvocationList();
                    }
                }
                t = t.BaseType;
            } while (t != null);

            return new Delegate[] { };
        }


        /// <summary>
        ///   Pop a context menu the richtextbox containing XML.
        ///   Workitem 7429.
        /// </summary>
        private void richTextBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (contextMenuStrip1.Items.Count == 0)
                {
                    // fill in the menu once with items duped from the Edit menu
                    foreach (ToolStripItem item in this.tsmiEdit.DropDownItems)
                    {
                        var mi = new System.Windows.Forms.ToolStripMenuItem();
                        mi.Text = item.Text;
                        foreach (Delegate d in GetEventSubscribers(item, "Click"))
                        {
                            mi.Click += (System.EventHandler) d;
                        }
                        contextMenuStrip1.Items.Add(mi);
                    }
                }

                contextMenuStrip1.Show(this.richTextBox1, new Point(e.X, e.Y));
            }
        }


        /// <summary>
        ///   Pop a context menu displaying the MRU list of xpath expressions
        /// </summary>
        private void tbXpath_MouseUp(Object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                mnuXpathMru.Items.Clear();
                int c = _xpathExpressionMruList.Count;
                for (int i = 0; i < c; i++)
                {
                    string s = _xpathExpressionMruList[c - i - 1].ToString();
                    var mi = new System.Windows.Forms.ToolStripMenuItem();
                    mi.Text = s;
                    mi.Click += (src, evt) => { this.tbXpath.Text = (src as ToolStripMenuItem).Text; };
                    mnuXpathMru.Items.Add(mi);
                }
                mnuXpathMru.Show(this.tbXpath, new Point(e.X, e.Y));
            }
        }

        /// <summary>
        ///   Handle ctrl-??? keys.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     ctrl-O for open,
        ///     ctrl-G for http get,
        ///     ctrl-S for save,
        ///     ctrl-F for re-format
        ///     ctrl-L for line numbers
        ///     ctrl-N for new tab
        ///     ctrl-E for extract selected
        ///     ctrl-R for remove selected
        ///   </para>
        /// </remarks>
        protected override bool ProcessDialogKey(Keys keyData)
        {
            // Get the actual integer value of the keystroke
            int keyCode = (int)keyData;

            if ((keyCode & (int)Keys.Shift) != 0)
                return base.ProcessDialogKey(keyData);

            if ((keyCode & (int)Keys.Alt) != 0)
                return base.ProcessDialogKey(keyData);

            if ((keyCode & (int)Keys.Control) != 0)
            {
                // Strip off the modifier keys
                Keys cleanKey = (Keys)(keyCode & 0xFFFF);
                switch (cleanKey)
                {
                    case Keys.O:
                        tsmiOpen_Click(null, null);
                        return true;
                    case Keys.G:
                        tsmiGet_Click(null, null);
                        return true;
                    case Keys.S:
                        tsmiSave_Click(null, null);
                        return true; // handled
                    case Keys.F:
                        IndentXml();
                        return true;
                    case Keys.L:
                        tsmiLineNumbers_Click(null, null);
                        return true;
                    case Keys.N:
                        tsmiNew_Click(null, null);
                        return true;
                    case Keys.E:
                        tsmiExtractHighlighted_Click(null, null);
                        return true;
                    case Keys.R:
                        tsmiRemoveSelected_Click(null, null);
                        return true;
                }
            }

            // chain - handle cut and paste, etc
            return base.ProcessDialogKey(keyData);
        }


        private TabPage tabPage
        {
            get
            {
                int ix = this.customTabControl1.SelectedIndex;
                if (ix < 0) return null;
                var tp = this.customTabControl1.TabPages[ix];
                return tp;
            }
        }



        private TabState tabState
        {
            get
            {
                if (tabPage == null) return null;
                return (tabPage.Tag as TabState);
            }
        }


        private XPathNavigator nav
        {
            get
            {
                // load the Xml doc, create navigator
                if (tabState.nav == null)
                {
                    string rtbText = richTextBox1.Text;
                    var xreader = XmlReader.Create(new StringReader(rtbText), readerSettings);
                    var xpathDoc = new XPathDocument(xreader);
                    tabState.nav = xpathDoc.CreateNavigator();
                }
                return tabState.nav;
            }
            set
            {
                tabState.nav = value;
            }
        }


        /// <summary>
        ///   holds the hash of xmlns prefix to a Tuple containing the
        ///   xmlns itself, as well as a bool indicating whether the
        ///   prefix is contrived or literal.
        ///   </summary>
        private XmlnsTable xmlNamespaces
        {
            get
            {
                if (tabState.xmlns == null)
                    tabState.xmlns = new XmlnsTable();
                return tabState.xmlns;
            }
        }


        private void customTabControl1_BeforeCloseTab(object sender, Ionic.WinForms.BeforeCloseTabEventArgs e)
        {
            DisableMatchButtons();
        }


        private void customTabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabState == null)
            {
                //this.label3.BringToFront();
                UpdateStatus("Ready.");
                return;
            }

            if (!isLoading)
            {
                // When loading, the tab gets selected before any of the other data,
                // like xpath and so on, is available.
                // Subsbequently, the tab state holds real data.
                richTextBox1 = tabPage.Controls[0] as Ionic.WinForms.RichTextBoxEx;
                this.tbXpath.Text = tabState.xpath;
                this.fileToLoad = tabState.src;
                this.lblStatus.Text = tabState.status; // most recent
                Trace("tabstate({0}).xmlnsTableIsExpanded: {1}",
                      tabState.tabNumber, tabState.xmlnsTableIsExpanded);
                DisplayXmlnsPrefixList();
                EnableMatchButtons();
                UpdateMatchCount();
            }
        }

        private void tsmiLineNumbers_Click(object sender, EventArgs e)
        {
            richTextBox1.ShowLineNumbers = !richTextBox1.ShowLineNumbers;
        }

        private void tsmiBasicHelp_Click(object sender, EventArgs e)
        {
            var bh = new BasicHelp();
            bh.AppTitle = "Ionic's XPathVisualizer";
            bh.AppDescription = "A tool for visualizing Xpath queries";
            bh.ShowDialog(this);
        }

        private void tsmiAbout_Click(object sender, EventArgs e)
        {
            AboutBox ab = new AboutBox();
            ab.AppTitle = "Ionic's XPathVisualizer";
            ab.AppDescription = "A tool for visualizing Xpath queries";

            string assemblyCopyright =
                ((AssemblyCopyrightAttribute)
                 Assembly.GetExecutingAssembly().GetCustomAttributes
                 (typeof(AssemblyCopyrightAttribute), false)[0]).Copyright;

            var a = System.Reflection.Assembly.GetExecutingAssembly();
            string version = a.GetName().Version.ToString();

            ab.AppVersion = version;
            ab.AppCopyright = assemblyCopyright;
            ab.AppMoreInfo = "";
            ab.AppDetailsButton = true;
            ab.ShowDialog(this);
        }

#region Menu Auto Hide
        // workitem 6720 - auto-hide of menu
        private void SetupAutoHide()
        {
            SetupAutoHide(true);
        }
        private void SetupAutoHide(bool wantReset)
        {
            this.menuStrip1.Visible = false;
            this.timerMenu.Enabled = true; // receive ticks
            #if AUTOHIDE
            this.aMainMenuItemIsDropped = false;
            #endif
            this._lastEventWasAlt = false;
            this._intTicks = 0;
            if (wantReset)
                this._lastMenuItemIndex = -1;
        }

        private void menuStrip1_MenuActivate(object sender, EventArgs e)
        {
            // reset the count, so we know when to hide the menu
            this._intTicks = 0;
        }

        private void AnyDropDownOpened(object sender, EventArgs e)
        {
            #if AUTOHIDE
            this.aMainMenuItemIsDropped = true;
            #endif
            var mi = sender as ToolStripMenuItem;
            if (mi != null)
            {
                this._lastMenuItemIndex = mi.Index();
            }
        }
        private void AnyDropDownClosed(object sender, EventArgs e)
        {
            #if AUTOHIDE
            this.aMainMenuItemIsDropped = false;
            #endif
        }

        private void menuStrip1_MenuDeactivate(object sender, EventArgs e)
        {
            SetupAutoHide(false);
            this._lastEventWasAlt = true;
            this._intTicks = 0;
        }

        private void timerMenu_Tick(object sender, EventArgs e)
        {
            #if AUTOHIDE

            // At one point I had enabled Autohide of the top
            // menustrip. The way it worked: if you hovered your mouse
            // over where the menustrip was supposed to be, it would
            // appear. If you moved your mose away, it would disappear,
            // after a short delay.
            //
            // In practice, it turned out to annoying. Sometimes it
            // would unexpectedly pop up. Then it would go away
            // magically. In order to uphold the principle of least
            // astonishment, I removed the feature.

            if (menuStrip1.Visible)
            {
                // if any of the popdown menus are visible, suppress autohide
                if (aMainMenuItemIsDropped)
                    return;

                Point ptCursor = PointToClient(MousePosition);
                // If the cursor on the menu, or as much as 2 pixels
                // below the menu
                if (ptCursor.Y <= (menuStrip1.Size.Height + 2))
                    return;

                if (this._intTicks >= 13)
                {
                    menuStrip1.Visible = false; // hide
                    this._intTicks = 0;  // reset counter
                }
                else
                {
                    this._intTicks++;
                }
            }
            else
            {
                // Retrieve the current mouse position relative to the form
                Point ptCursor = PointToClient(MousePosition);
                // If the cursor is within the menu area
                if ((ptCursor.Y <= menuStrip1.Size.Height) & (ptCursor.Y > 0))
                {
                    if (this._intTicks >= 2)
                    {
                        menuStrip1.Visible = true;
                        this._intTicks = 0;
                    }
                    else
                    {
                        this._intTicks++;
                    }
                }
            }
            #else
            this._intTicks++;
            #endif
        }
        #endregion

    }


    /// <summary>
    ///   Holds arbitrary state associated to each TabPage
    /// </summary>
    internal class TabState
    {
        public XmlnsTable xmlns              { get; set; }
        public bool xmlnsTableIsExpanded     { get; set; }
        public String src                    { get; set; }
        public int tabNumber                 { get; set; }
        public List<Tuple<int, int>> matches { get; set; }
        public int currentMatch              { get; set; }
        public String xpath                  { get; set; }
        public String status                 { get; set; }
        public XPathNavigator nav            { get; set; }
        public bool okToSave                 { get; set; }
        public int numVisibleLines;
        public int totalLinesInDoc;
    }

    internal class XmlnsInfo
    {
        public string Prefix          { get;set; }
        public string Ns              { get;set; }
        public bool IsContrivedPrefix { get;set; }
        public bool IsDefault         { get;set; }

        public XmlnsInfo(string prefix, string ns, bool contrived) : this(prefix, ns, contrived, false) { }

        public XmlnsInfo(string prefix, string ns, bool contrived, bool isDefault)
        {
            Prefix = prefix;
            Ns = ns;
            IsContrivedPrefix = contrived;
            IsDefault = isDefault;
        }

        public  XmlnsInfo Clone()
        {
            var item = new XmlnsInfo(this.Prefix, this.Ns, this.IsContrivedPrefix, this.IsDefault);
            return item;
        }
    }


    internal class XmlnsTable : List<XmlnsInfo>
    {
        /// <summary>
        ///    default ctor
        /// </summary>
        public XmlnsTable() { }

        /// <summary>
        /// a copy constructor
        /// </summary>
        public XmlnsTable(XmlnsTable list)
        {
            foreach (XmlnsInfo item in list)
            {
                this.Add(item.Clone());
            }
        }

        public XmlnsInfo Default
        {
            get
            {
                foreach(XmlnsInfo v in this)
                {
                    if (v.IsDefault) return v;
                }
                return null;
            }
        }

        public void ClearDefault()
        {
            foreach(XmlnsInfo v in this)
            {
                v.IsDefault = false;
            }
        }

        public bool ContainsNs(String ns)
        {
            return (FindNs(ns)!= null);
        }

        public bool ContainsPrefix(String prefix)
        {
            return (FindPrefix(prefix)!= null);
        }

        public XmlnsInfo FindPrefix(String prefix)
        {
            foreach(XmlnsInfo v in this)
            {
                if (v.Prefix.Equals(prefix)) return v;
            }
            return null;
        }

        public XmlnsInfo FindNs(String ns)
        {
            foreach(XmlnsInfo v in this)
            {
                if (v.Ns.Equals(ns)) return v;
            }
            return null;
        }


        public void RemoveItemWithPrefix(string prefix)
        {
            var toRemove = new List<XmlnsInfo>();
            foreach(XmlnsInfo v in this)
            {
                if (v.Prefix.Equals(prefix))
                    toRemove.Add(v);
            }

            foreach (var item in toRemove)
                this.Remove(item);
        }
    }

}
