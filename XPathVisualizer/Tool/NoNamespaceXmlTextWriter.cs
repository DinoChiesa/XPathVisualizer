// NoNamespaceXmlTextWriter.cs
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


using System.Xml;
using System.Globalization;

namespace XPathVisualizer
{
    public class NoNamespaceXmlTextWriter : XmlTextWriter
    {
        public NoNamespaceXmlTextWriter(System.Text.StringBuilder sb,
            System.Xml.XmlWriterSettings settings)
            : base(new System.IO.StringWriter(sb, CultureInfo.InvariantCulture))
        {
            Formatting = (settings.Indent) ? System.Xml.Formatting.Indented
                : System.Xml.Formatting.None;

            IndentChar = settings.IndentChars[0];
            Indentation = settings.IndentChars.Length;
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            base.WriteStartElement("", localName, "");
        }

        bool suppressing = false;
        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            if (prefix == "xmlns" ||
                (prefix == "" && localName == "xmlns"))
            {
                suppressing = true;
                return;
            }

            if (prefix == "xml")
                base.WriteStartAttribute(prefix, localName, ns);
            else
                base.WriteStartAttribute("", localName, "");
        }


        public override void WriteEndAttribute()
        {
            if (suppressing)
            {
                suppressing = false;
                return;
            }
            base.WriteEndAttribute();
        }


        public override void WriteString(string s)
        {
            if (suppressing)
            {
                return;
            }
            base.WriteString(s);
        }
    }

}