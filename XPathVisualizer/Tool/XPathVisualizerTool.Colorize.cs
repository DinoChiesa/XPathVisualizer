// XPathVisualizerTool.Colorize.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa.
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


using System;
using System.Xml;                 // for XmlReader, etc
using System.Collections.Generic; // List
using System.IO;                  // StringReader
using System.Threading;           // ManualResetEvent
using System.Drawing;             // for Color
using System.ComponentModel;      // BackgroundWorker
using System.Runtime.InteropServices;

namespace XPathVisualizer
{
    public partial class XPathVisualizerTool
    {

        /// <summary>
        ///   Update the progressbar
        /// </summary>
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage >= this.progressBar1.Minimum &&
                e.ProgressPercentage <= this.progressBar1.Maximum)
                this.progressBar1.Value = e.ProgressPercentage;

            if (e.ProgressPercentage != 100)
            {
                if (this.progressBar1.Visible != true)
                {
                    this.lblStatus.Text = "Formatting...";
                    this.progressBar1.Visible = true;
                }
            }
            else
            {
                this.progressBar1.Visible = false;
                // only reset the status string if it has not changed in the interim
                if (this.lblStatus.Text == "Formatting...")
                    this.lblStatus.Text = String.Format("Done formatting. {0} lines", this.richTextBox1.Lines.Length);
                // workitem 7432
                tbXpath_TextChanged(null,null);
            }
        }




        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        public void KickoffColorizer()
        {
            if (backgroundWorker1 != null)
                return;

            // this worker never completes, never returns
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            backgroundWorker1.WorkerSupportsCancellation = false;
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.ProgressChanged += this.backgroundWorker1_ProgressChanged;
            backgroundWorker1.DoWork += this.DoBackgroundColorizing;
            backgroundWorker1.RunWorkerAsync();
        }



        public class FormatChange
        {
            public FormatChange(int start, int length, System.Drawing.Color color)
            {
                Start = start;
                Length = length;
                ForeColor = color;
            }

            public int Start;
            public int Length;
            public System.Drawing.Color ForeColor;
        }



        private void NotifyStopFormatting(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<String>(this.NotifyStopFormatting),
                                         new object[] { message });
            }
            else
            {
                this.progressBar1.Visible = false;
                this.lblStatus.Text = message;
            }
        }



        private void ApplyChanges(List<FormatChange> list)
        {
            if (this.richTextBox1.InvokeRequired)
            {
                this.richTextBox1.Invoke(new Action<List<FormatChange>>(this.ApplyChanges),
                                         new object[] { list });
            }
            else
            {
                // The RichTextBox is a RichEdit Win32 control. When the
                // selection changes and it has focus, the control will
                // auto-scroll.  The way to prevent that is to call
                // BeginUpdate/EndUpdate.
                richTextBox1.BeginUpdateAndSaveState();

                foreach (var change in list)
                {
                    richTextBox1.SetSelectionColor(change.Start, change.Start + change.Length, change.ForeColor);
                }

                richTextBox1.EndUpdateAndRestoreState();
            }
        }



        private void ResetXmlTextBox()
        {
            if (this.richTextBox1.InvokeRequired)
            {
                this.richTextBox1.Invoke(new Action(this.ResetXmlTextBox));
            }
            else
            {
                richTextBox1.BeginUpdateAndSaveState();

                // get the text, and put it back.  why? because it's
                // possible to paste in RTF, including fonts and
                // formatting.  This won't show up correctly. This
                // strips any RTF formatting.
                string txt = this.richTextBox1.Text;
                this.richTextBox1.Text = txt;

                this.richTextBox1.SelectAll();
                this.richTextBox1.SelectionFont = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

                this.richTextBox1.SelectionBackColor = Color.White;

                richTextBox1.EndUpdateAndRestoreState();
            }
        }

        private void SaveCaretPosition()
        {
            savedPosition = this.richTextBox1.SelectionStart;
        }
        private void RestoreCaretPosition()
        {
            this.richTextBox1.SelectionStart = savedPosition;
            this.richTextBox1.SelectionLength = 0;
        }


        void InvokeActionProperly(Action x)
        {
            if (this.richTextBox1.InvokeRequired)
            {
                this.richTextBox1.Invoke(x);
                return;
            }
            x();
        }

        void suspend()
        {
            this.richTextBox1.SuspendLineNumberPainting();
        }
        void resume()
        {
            this.richTextBox1.ResumeLineNumberPainting();
        }

        private int savedPosition;
        private const int DELAY_IN_MILLISECONDS = 650;
        private int progressCount = 0;

        private void DoBackgroundColorizing(object sender, DoWorkEventArgs e)
        {
            // Design Notes:
            // -------------------------------------------------------------------
            //
            // It takes a long time, maybe 10s or more, to colorize the XML
            // syntax in an xml file 100k in size.  Therefore, to maintain UI
            // responsiveness, it's necessary to perform the syntax highlighting
            // asynchronously.
            //
            // This method runs endlessly. The first thing it does is wait for a
            // signal on the wantFormat event. This event is set when an XML file
            // is loaded, or when the text in the richtextbox changes.
            //
            // When the signal is received, execution continues, and the
            // highlighting begins. The approach is this: read a segment of the
            // XML, decide how to highlight it, then place a description of that
            // formatting change into a list.  Then, continue by reading the next
            // segment of XML, until the entire XML document has been processed.
            //
            // On an interval that is normally every 1/48th of the lines - if
            // there are 960 lines, then every 20 lines - this method does two
            // things: call the progress update for the BG worker, and also apply
            // the queued changes.  Using this approach the progress bar magically
            // appears while highlighting is happening, and disappears when
            // highlighting finishes.
            //
            // The reason for the batch approach: the control.Invoke() method,
            // necessary to apply changes to the richtextbox, can be costly. Batching
            // changes amortizes the cost of that method across a number of format
            // changes.
            //
            // The ApplyChanges method applies the batch of changes.  It first
            // does a richtextbox.Invoke() to get on the proper thread.  Once
            // there, it saves the scroll and selection state, applies each
            // formatting change in the list to the rtb text, restores the scroll
            // and selection state, and then calls Refresh() on the RTB.  After
            // that method returns, this method clears the list of format changes
            // and continues reading and analyzing the next segment of XML.
            //
            // If at any time, the user changes the RTB text, either through a new
            // load of an XML file, or through direct editing in the richtextbox,
            // the main UI signals the wantFormat event again.  This method treats
            // the raising of that signal as a "cancel-and-restart" message.  Upon
            // detecting that signal, this method stops working, if it was
            // working, and then starts reading and highlighting at the beginning
            // again.
            //
            // There's also some logic to delay the formatting, to wait for the
            // user to stop typing, if that is what caused the wantFormat to get
            // signalled.
            //
            // When this method finishes highlighting, this method waits for the
            // wantFormat signal again.  This method never returns.
            //
            BackgroundWorker self = sender as BackgroundWorker;

            //             var xmlReaderSettings = new XmlReaderSettings
            //                 {
            //                     ProhibitDtd = false,
            //                     XmlResolver = new Ionic.Xml.XhtmlResolver()
            //
            //                     // this works in .NET 4.0 ??
            //                     //DtdProcessing = DtdProcessing.Parse,
            //                     //XmlResolver =
            //                     //new XmlPreloadedResolver(new XmlXapResolver(),
            //                     //XmlKnownDtds.Xhtml10)
            //                 };

            do
            {
                InvokeActionProperly(SaveCaretPosition);
                try
                {
                    InvokeActionProperly(resume);
                    wantFormat.WaitOne();
                    wantFormat.Reset();
                    progressCount = 0;

                    var list = new List<FormatChange>();

                    // We want a re-format, but let's wait til
                    // the user stops typing...
                    if (_lastRtbKeyPress != _originDateTime)
                    {
                        System.Threading.Thread.Sleep(DELAY_IN_MILLISECONDS);
                        System.DateTime now = System.DateTime.Now;
                        var _delta = now - _lastRtbKeyPress;
                        if (_delta < new System.TimeSpan(0, 0, 0, 0, DELAY_IN_MILLISECONDS))
                            continue;
                    }

                    InvokeActionProperly(suspend);

                    // Get the text ONCE.  In a RichTextBox, this is
                    // expensive, so we do it once, until a change is detected
                    // in the richtextbox.
                    string txt = (this.richTextBox1.InvokeRequired)
                        ? (string)this.richTextBox1.Invoke((System.Func<string>)(() => this.richTextBox1.Text))
                        : this.richTextBox1.Text;

                    ResetXmlTextBox();

                    var lc = new LineCalculator(txt);
                    float maxLines = (float)lc.CountLines();

                    int reportingInterval = (maxLines > 96)
                        ? (int)(maxLines / 48)
                        : 4;

                    int lastReport = -1;
                    var sr = new StringReader(txt);

                    XmlReader reader = XmlReader.Create(sr, readerSettings);

                    IXmlLineInfo rinfo = (IXmlLineInfo)reader;
                    if (!rinfo.HasLineInfo()) continue;

                    int rcount = 0;
                    int ix = 0;
                    int t = 0;
                    while (reader.Read())
                    {
                        if ((rcount % 8) == 0)
                        {
                            // If another format is pending, that means
                            // the text has changed and we should stop this
                            // formatting effort and start again.
                            if (wantFormat.WaitOne(0, false))
                                break;
                        }
                        rcount++;

                        // report progress
                        if ((rinfo.LineNumber / reportingInterval) > lastReport)
                        {
                            int pct = (int)((float)rinfo.LineNumber / maxLines * 100);
                            self.ReportProgress(pct);
                            lastReport = (rinfo.LineNumber / reportingInterval);
                            ApplyChanges(list);
                            list.Clear();
                            progressCount++;
                        }

                        switch (reader.NodeType)
                        {
                            case XmlNodeType.XmlDeclaration: // The node is an declaration.
                                ix = lc.GetCharIndexFromLine(rinfo.LineNumber - 1) +
                                    rinfo.LinePosition - 1;
                                t = 2 + reader.Name.Length + 1 + reader.Value.Length + 2;
                                list.Add(new FormatChange(ix - 2, t, Color.Blue));
                                break;

                            case XmlNodeType.DocumentType: // DTD
                                break;

                            case XmlNodeType.ProcessingInstruction:
                                // eg, <?xml-stylesheet type="text/xsl" href="http://www.codeplex.com/rss.xsl"?>
                                ix = lc.GetCharIndexFromLine(rinfo.LineNumber - 1) +  rinfo.LinePosition - 1;
                                t = 2 + reader.Name.Length + 1 + reader.Value.Length + 2;
                                list.Add(new FormatChange(ix - 2, t, Color.Blue));
                                break;

                            case XmlNodeType.Element: // The node is an element.
                                ix = lc.GetCharIndexFromLine(rinfo.LineNumber - 1) +
                                    rinfo.LinePosition - 1;

                                list.Add(new FormatChange(ix - 1, 1, Color.Blue));
                                list.Add(new FormatChange(ix, reader.Name.Length, Color.DarkRed));

                                if (reader.HasAttributes)
                                {
                                    reader.MoveToFirstAttribute();
                                    do
                                    {
                                        //string s = reader.Value;
                                        ix = lc.GetCharIndexFromLine(rinfo.LineNumber - 1) +
                                            + rinfo.LinePosition - 1;
                                        list.Add(new FormatChange(ix, reader.Name.Length, Color.Red));

                                        ix += reader.Name.Length;

                                        ix = txt.IndexOf('=', ix);

                                        // make the equals sign blue
                                        list.Add(new FormatChange(ix, 1, Color.Blue));

                                        // skip over the quote char (it remains black)
                                        ix = txt.IndexOf(reader.QuoteChar, ix);
                                        ix++;
                                        // highlight the value of the attribute as blue
                                        if (txt.Substring(ix).StartsWith(reader.Value))
                                        {
                                            list.Add(new FormatChange(ix, reader.Value.Length, Color.Blue));
                                        }
                                        else
                                        {
                                            // Difference in escaping.  The InnerXml may include
                                            // \" where &quot; is in the doc.
                                            string s = reader.Value.XmlEscapeQuotes();
                                            int delta = s.Length - reader.Value.Length;
                                            list.Add(new FormatChange(ix, reader.Value.Length + delta, Color.Blue));
                                        }

                                    }
                                    while (reader.MoveToNextAttribute());

                                    ix = txt.IndexOf('>', ix);

                                    // the close-angle-bracket
                                    if (txt[ix - 1] == '/')
                                    {
                                        list.Add(new FormatChange(ix - 1, 2, Color.Blue));
                                    }
                                    else
                                    {
                                        list.Add(new FormatChange(ix, 1, Color.Blue));
                                    }
                                }
                                break;

                            case XmlNodeType.Text:
                                // Leave these black - no addl formatting
                                break;

                            case XmlNodeType.EndElement:
                                ix = lc.GetCharIndexFromLine(rinfo.LineNumber - 1) +
                                    +rinfo.LinePosition - 1;

                                list.Add(new FormatChange(ix - 2, 2, Color.Blue));
                                list.Add(new FormatChange(ix, reader.Name.Length, Color.DarkRed));
                                list.Add(new FormatChange(ix + reader.Name.Length, 1, Color.Blue));
                                break;

                            case XmlNodeType.Attribute:
                                // These are handed within XmlNodeType.Element
                                break;

                            case XmlNodeType.Comment:
                                ix = lc.GetCharIndexFromLine(rinfo.LineNumber - 1) +
                                    +rinfo.LinePosition - 1;
                                list.Add(new FormatChange(ix, reader.Value.Length, Color.Green));
                                break;
                        }
                    }

                    // in case there are more
                    ApplyChanges(list);
                    self.ReportProgress(100);
                }
                catch (Exception exc1)
                {
                    // this can happen when editing an XML file, and the
                    // xurrent state of the document is not valid xml.
                    NotifyStopFormatting(exc1.Message);

                    // System.Windows.Forms.MessageBox.Show("Exception: " + exc1.Message,
                    //                 "Exception while formatting",
                    //                 System.Windows.Forms.MessageBoxButtons.OK,
                    //                 System.Windows.Forms.MessageBoxIcon.Error);
                }
                finally
                {
                    //RestoreCaretPosition();
                }
            }
            while (true);

        }
    }
}

