// XPathVisualizerTool.FormState.cs
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
// flymake: msbuild XPathVisualizer.csproj /t:CheckSyntax /property:FlymakeCheck=@@SRC@@ /property:FlymakeExclude=@@ORIG@@
// compile: msbuild
//

using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;

namespace XPathVisualizer
{
    public partial class XPathVisualizerTool
    {
        private MruList<String> _fileHistory;

        private void SlurpFileHistory()
        {
            _fileHistory = new MruList<String>(10);
            var mruPath = _AppRegyPath + "\\MRU";
            var mruKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(mruPath, true);
            if (mruKey == null) return;

            // restore the history, in reverse order, so the last one stored,
            // is treated as the most recent item.
            for (int i=9; i >= 0; i--)
            {
                string n = new String((char)(i+65),1);
                string priorFile = (string) mruKey.GetValue(n);

                if (!String.IsNullOrEmpty(priorFile))
                {
                    _fileHistory.Store(priorFile);
                }
            }
        }


        private void StoreFileHistory()
        {
            var mruPath = _AppRegyPath + "\\MRU";
            var mruKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(mruPath, true);
            if (mruKey == null)
                mruKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(mruPath);

            var list = _fileHistory.GetList();

            for (int i=0; i < list.Count; i++)
            {
                string n = new String((char)(i+65),1);
                mruKey.SetValue(n, list[i]);
            }
        }



        private void SaveFormToRegistry()
        {
            if (AppCuKey == null) return;

            StoreFileHistory();

            if (!String.IsNullOrEmpty(this.tbXpath.Text))
                AppCuKey.SetValue(_rvn_XPathExpression, this.tbXpath.Text);

            if (!String.IsNullOrEmpty(this.tbPrefix.Text))
                AppCuKey.SetValue(_rvn_Prefix, this.tbPrefix.Text);

            if (!String.IsNullOrEmpty(this.tbXmlns.Text))
                AppCuKey.SetValue(_rvn_Xmlns, this.tbXmlns.Text);

            AppCuKey.SetValue(_rvn_LastRun, System.DateTime.Now.ToString("yyyy MMM dd HH:mm:ss"));
            int x = (Int32)AppCuKey.GetValue(_rvn_Runs, 0);
            x++;
            AppCuKey.SetValue(_rvn_Runs, x);

            // store the size of the form
            int w = 0, h = 0, left = 0, top = 0;
            if (this.Bounds.Width < this.MinimumSize.Width || this.Bounds.Height < this.MinimumSize.Height)
            {
                // RestoreBounds is the size of the window prior to last minimize action.
                // But the form may have been resized since then!
                w = this.RestoreBounds.Width;
                h = this.RestoreBounds.Height;
                left = this.RestoreBounds.Location.X;
                top = this.RestoreBounds.Location.Y;
            }
            else
            {
                w = this.Bounds.Width;
                h = this.Bounds.Height;
                left = this.Location.X;
                top = this.Location.Y;
            }
            AppCuKey.SetValue(_rvn_Geometry,
                              String.Format("{0},{1},{2},{3},{4}",
                                            left, top, w, h, (int)this.WindowState));

            // workitem 3392
            // store the position of splitter
            // AppCuKey.SetValue(_rvn_Splitter, this.splitContainer3.SplitterDistance.ToString());

            // the Xpath expression MRU list
            var converted = _xpathExpressionMruList.ToList().ConvertAll(z => z.XmlEscapeIexcl());
            string history = String.Join("¡", converted.ToArray());
            AppCuKey.SetValue(_rvn_History, history);

        }


        private void FillFormFromRegistry()
        {
            if (!stateLoaded)
            {
                if (AppCuKey != null)
                {
                    // fill the various textboxes
                    SlurpFileHistory();

                    var s = (string)AppCuKey.GetValue(_rvn_XPathExpression);
                    if (s != null) this.tbXpath.Text = s;

                    s = (string)AppCuKey.GetValue(_rvn_Prefix);
                    if (s != null) this.tbPrefix.Text = s;

                    s = (string)AppCuKey.GetValue(_rvn_Xmlns);
                    if (s != null) this.tbXmlns.Text = s;


                    // get the MRU list of XPath expressions
                    _xpathExpressionMruList = new System.Windows.Forms.AutoCompleteStringCollection();
                    string historyString = (string)AppCuKey.GetValue(_rvn_History, "");
                    if (!String.IsNullOrEmpty(historyString))
                    {
                        string[] items = historyString.Split('¡');
                        if (items != null && items.Length > 0)
                        {
                            //_xpathExpressionMruList.AddRange(items);
                            foreach (string item in items)
                                _xpathExpressionMruList.Add(item.XmlUnescapeIexcl());

                            // insert the most recent expression into the box?
                            this.tbXpath.Text = _xpathExpressionMruList[_xpathExpressionMruList.Count-1];
                        }
                    }


                    // set the geometry of the form
                    s = (string)AppCuKey.GetValue(_rvn_Geometry);
                    if (!String.IsNullOrEmpty(s))
                    {
                        int[] p = Array.ConvertAll<string, int>(s.Split(','),
                                                                new Converter<string, int>((t) => { return Int32.Parse(t); }));
                        if (p != null && p.Length == 5)
                        {
                            this.Bounds = ConstrainToScreen(new System.Drawing.Rectangle(p[0], p[1], p[2], p[3]));
                        }
                    }


                    // workitem 3392 - don't need this
                    // set the splitter
                    // s = (string)AppCuKey.GetValue(_rvn_Splitter);
                    // if (!String.IsNullOrEmpty(s))
                    //   {
                    //     try
                    //     {
                    //         int x = Int32.Parse(s);
                    //         this.splitContainer3.SplitterDistance = x;
                    //     }
                    //     catch { }
                    // }

                    stateLoaded = true;

                }

            }

        }


        private System.Drawing.Rectangle ConstrainToScreen(System.Drawing.Rectangle bounds)
        {
            Screen screen = Screen.FromRectangle(bounds);
            System.Drawing.Rectangle workingArea = screen.WorkingArea;
            int width = Math.Min(bounds.Width, workingArea.Width);
            int height = Math.Min(bounds.Height, workingArea.Height);
            // mmm....minimax
            int left = Math.Min(workingArea.Right - width, Math.Max(bounds.Left, workingArea.Left));
            int top = Math.Min(workingArea.Bottom - height, Math.Max(bounds.Top, workingArea.Top));
            return new System.Drawing.Rectangle(left, top, width, height);
        }


        public Microsoft.Win32.RegistryKey AppCuKey
        {
            get
            {
                if (_appCuKey == null)
                {
                    _appCuKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_AppRegyPath, true);
                    if (_appCuKey == null)
                        _appCuKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_AppRegyPath);
                }
                return _appCuKey;
            }
            set { _appCuKey = null; }
        }

        private Microsoft.Win32.RegistryKey _appCuKey;
        private static string _AppRegyPath = "Software\\Dino Chiesa\\XPathVisualizer";
        private string _rvn_XPathExpression = "XPathExpression";
        private string _rvn_Prefix = "Prefix";
        private string _rvn_Xmlns = "Xmlns";
        private string _rvn_Geometry = "Geometry";
        private string _rvn_History = "History";
        private string _rvn_LastRun = "LastRun";
        private string _rvn_Runs = "Runs";
        private readonly int _MaxMruListSize = 14;
        //private Ionic.Utils.MruList<String> _xpathExpressionMruList;
        private System.Windows.Forms.AutoCompleteStringCollection _xpathExpressionMruList;
        private bool stateLoaded;
    }



    /// <summary>
    ///   implements a LIFO Most-recently-used list.
    /// </summary>
    public class MruList<T>
    {
        private LinkedList<T> _items;
        private int _maxCapacity;
        private LinkedListNode<T> cursor;

        public MruList(int capacity)
        {
            _maxCapacity = capacity;
            cursor = null;
            _items = new LinkedList<T>();
        }

        /// <summary>
        ///   Clears the circular buffer of all items.
        /// </summary>
        public void Clear()
        {
            _items = new LinkedList<T>();
            cursor = null;
        }


        //private LinkedListNode<T> FindNode(T value)
        //{
        //    LinkedListNode<T> node = items.First;
        //    while (node != null)
        //    {
        //        bool found = (compareFunc!=null)
        //            ? compareFunc(value, node.Value)
        //            : value.Equals(node.Value);
        //        if (found)
        //            return node;
        //        node = node.Next;
        //    }
        //    return null;
        //}

        public void ResetCursor()
        {
            cursor= null;
        }

        /// <summary>
        ///   stores one item in the circular buffer.
        /// </summary>
        public T Store(T value)
        {
            cursor = null;
            LinkedListNode<T> node = _items.Find(value);
            if (node != null)
            {
                _items.Remove(node);
                _items.AddFirst(node);
                return node.Value;
            }

            if (_items.Count == _maxCapacity)
                _items.RemoveLast();

            _items.AddFirst(value);
            return value;
        }


        /// <summary>
        ///   Gets the contents of the circular buffer, in an ordered list.
        /// </summary>
        public List<T> GetList()
        {
            List<T> list = new List<T>();
            LinkedListNode<T> node = _items.First;
            while (node != null)
            {
                list.Add(node.Value);
                node = node.Next;
            }
            return list;
        }


        /// <summary>
        ///   Stores a range of items into the circ buffer.
        /// </summary>
        public void StoreRange(T[] range)
        {
            // add in reverse order to preserve order
            for(int i= range.Length-1; i >=0; i--)
                Store(range[i]);
            cursor = null;
        }


        public T GetNext()
        {
            // advance to the next
            if (cursor != null)
                cursor = cursor.Next;

            // if non-null, return it
            if (cursor != null)
                return cursor.Value;

            // reset to the beginning
            cursor = _items.First;
            if (cursor != null)
                return cursor.Value;

            // no items in the list
            return default(T);
        }
    }


#if NOT
    public class CapacityBoundedCache<TKey, TValue>
    {
        private LinkedList<TKey> _keys;
        private Dictionary<TKey, TValue> _items;
        private int _maxCapacity;

        public CapacityBoundedCache(int capacity)
        {
            _maxCapacity = capacity;
            _keys = new LinkedList<TKey>();
            _items = new Dictionary<TKey, TValue>(capacity);
        }

        public int Capacity
        {
            get { return _maxCapacity; }
        }

        public TKey Store(TKey key, TValue value)
        {
            LinkedListNode<TKey> keynode = _keys.Find(key);
            if (keynode != null)
            {
                _keys.Remove(keynode);
                _keys.AddFirst(keynode);
                _items[key] = value;
                return keynode.Value;  // the key
            }

            if (_keys.Count == _maxCapacity)
                _keys.RemoveLast();

            _keys.AddFirst(key);
            _items[key] = value;
            return key;
        }

        public bool ContainsKey(TKey key)
        {
            LinkedListNode<TKey> keynode = _keys.Find(key);
            return (keynode != null);
        }

        public TValue GetValue(TKey key)
        {
            if (ContainsKey(key))
                return _items[key];

            return default(TValue);
        }

        public IEnumerable<TKey> Keys
        {
            get { return _items.Keys; }
        }
    }
#endif

}
