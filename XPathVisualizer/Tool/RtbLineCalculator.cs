// RtbLineCalculator.cs
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.XPath;
using System.Drawing;


namespace XPathVisualizer
{

    // This previously was an extension, but it performs much better as
    // a separate class. Key reason: state. For example, this class
    // takes the rtb.Text once, instead of every time through, which
    // provides a big performance advantage.  Also: it stores the last
    // search and starts the search from there, if appropriate.
    internal class LineCalculator
    {
        private int lastLine=Int32.MaxValue;
        private int lastC=-1;
        private string txt;

        public LineCalculator(System.Windows.Forms.RichTextBox rtb)
        {
            txt = rtb.Text;
        }

        public LineCalculator(String text)
        {
            txt = text;
        }

        public int CountLines()
        {
            int lineCount= 0;
            int c = 0, c2=-1;
            do
            {
                c = c2+1;
                c2 = txt.IndexOf('\n', c);

                if (c2 >= 0)
                    lineCount++;

            }
            while (c2 >= 0);
            return lineCount;
        }


        public int GetCharIndexFromLine(int line)
        {
            // The built-in RichTextBox.GetFirstCharIndexFromLine does not
            // work for me. Not sure why.
            line++;
            if (line <= 1) return 0;

            int c = 0;
            int cLine = 0;

            if (line >= lastLine)
            {
                c= lastC;
                cLine= lastLine-1;
            }
            if (cLine + 1 == line)
                return c;


            do
            {
                int delta = txt.IndexOf('\n', c);
                if (delta - c < 0) return -1;
                c = delta + 1;
                cLine++;
            }
            while (cLine + 1 < line);


            lastLine = line;
            lastC = c;

            return c;
        }
    }

}