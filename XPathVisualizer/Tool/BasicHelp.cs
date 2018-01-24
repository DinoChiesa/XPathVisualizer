using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.IO;

namespace XPathVisualizer
{
    public partial class BasicHelp : Form
    {
        private Dictionary<String,String> _EntryAssemblyAttribCollection;

        public BasicHelp()
        {
            InitializeComponent();
        }

        // <summary>
        // single line of text to show in the application title section of the about box dialog
        // </summary>
        // <remarks>
        // defaults to "%title%"
        // %title% = Assembly: AssemblyTitle
        // </remarks>
        public string AppTitle
        {
            get
            {
                return AppTitleLabel.Text;
            }
            set
            {
                AppTitleLabel.Text = value;
            }
        }

        // <summary>
        // single line of text to show in the description section of the about box dialog
        // </summary>
        // <remarks>
        // defaults to "%description%"
        // %description% = Assembly: AssemblyDescription
        // </remarks>
        public string AppDescription
        {
            get
            {
                return AppDescriptionLabel.Text;
            }
            set
            {
                if (value == ""){
                    AppDescriptionLabel.Visible = false;
                }else{
                    AppDescriptionLabel.Visible = true;
                    AppDescriptionLabel.Text = value;
                }
            }
        }

        // <summary>
        // intended for the default 32x32 application icon to appear in the upper left of the about dialog
        // </summary>
        // <remarks>
        // if you open this form using .ShowDialog(Owner), the icon can be derived from the owning form
        // </remarks>
        public Image AppImage
        {
            get{
                return ImagePictureBox.Image;
            }
            set{
                ImagePictureBox.Image = value;
            }
        }


        // <summary>
        // returns DateTime this Assembly was last built. Will attempt to calculate from build number, if possible.
        // If not, the actual LastWriteTime on the assembly file will be returned.
        // </summary>
        // <param name="a">Assembly to get build date for</param>
        // <param name="ForceFileDate">Don't attempt to use the build number to calculate the date</param>
        // <returns>DateTime this assembly was last built</returns>
        private DateTime AssemblyBuildDate(Assembly a , bool ForceFileDate)
        {
            var lastWrite = new Func<Assembly,DateTime>( (ass) => {
                    if (ass.Location == null || ass.Location == "")
                        return DateTime.MaxValue;
                    try{
                        return File.GetLastWriteTime(ass.Location);
                    }catch(Exception){
                    }
                    return DateTime.MaxValue;
                });
            Version AssemblyVersion = a.GetName().Version;
            DateTime dt;

            if (ForceFileDate){
                dt = lastWrite(a);
            }else{
                dt = DateTime.Parse("01/01/2000").AddDays(AssemblyVersion.Build).AddSeconds(AssemblyVersion.Revision * 2);
                if (TimeZone.IsDaylightSavingTime(dt, TimeZone.CurrentTimeZone.GetDaylightChanges(dt.Year))){
                    dt = dt.AddHours(1);
                }
                if (dt > DateTime.Now || AssemblyVersion.Build < 730 || AssemblyVersion.Revision == 0){
                    dt = lastWrite(a);
                }
            }

            return dt;
        }


        // <summary>
        // returns string name / string value pair of all attribs
        // for specified assembly
        // </summary>
        // <remarks>
        // note that Assembly* values are pulled from AssemblyInfo file in project folder
        //
        // Trademark       = AssemblyTrademark string
        // Debuggable      = true
        // GUID            = 7FDF68D5-8C6F-44C9-B391-117B5AFB5467
        // CLSCompliant    = true
        // Product         = AssemblyProduct string
        // Copyright       = AssemblyCopyright string
        // Company         = AssemblyCompany string
        // Description     = AssemblyDescription string
        // Title           = AssemblyTitle string
        // </remarks>
        private Dictionary<String,String> AssemblyAttribs(Assembly a)
        {
            string TypeName;
            string Name;
            string Value;
            var nvc = new Dictionary<String,String>();
            Regex r = new Regex(@"(\.Assembly|\.)(?<Name>[^.]*)Attribute$", RegexOptions.IgnoreCase);

            foreach (object attrib in a.GetCustomAttributes(false))
            {
                TypeName = attrib.GetType().ToString();
                Name = r.Match(TypeName).Groups["Name"].ToString();
                Value = "";
                switch (TypeName)
                {
                    case "System.CLSCompliantAttribute":
                        Value = ((CLSCompliantAttribute)attrib).IsCompliant.ToString(); break;
                    case "System.Diagnostics.DebuggableAttribute":
                        Value = ((System.Diagnostics.DebuggableAttribute)attrib).IsJITTrackingEnabled.ToString(); break;
                    case "System.Reflection.AssemblyCompanyAttribute":
                        Value = ((AssemblyCompanyAttribute)attrib).Company.ToString(); break;
                    case "System.Reflection.AssemblyConfigurationAttribute":
                        Value = ((AssemblyConfigurationAttribute)attrib).Configuration.ToString(); break;
                    case "System.Reflection.AssemblyCopyrightAttribute":
                        Value = ((AssemblyCopyrightAttribute)attrib).Copyright.ToString(); break;
                    case "System.Reflection.AssemblyDefaultAliasAttribute":
                        Value = ((AssemblyDefaultAliasAttribute)attrib).DefaultAlias.ToString(); break;
                    case "System.Reflection.AssemblyDelaySignAttribute":
                        Value = ((AssemblyDelaySignAttribute)attrib).DelaySign.ToString(); break;
                    case "System.Reflection.AssemblyDescriptionAttribute":
                        Value = ((AssemblyDescriptionAttribute)attrib).Description.ToString(); break;
                    case "System.Reflection.AssemblyFileVersionAttribute":
                        Value = ((AssemblyFileVersionAttribute)attrib).Version.ToString(); break;
                    case "System.Reflection.AssemblyInformationalVersionAttribute":
                        Value = ((AssemblyInformationalVersionAttribute)attrib).InformationalVersion.ToString(); break;
                    case "System.Reflection.AssemblyKeyFileAttribute":
                        Value = ((AssemblyKeyFileAttribute)attrib).KeyFile.ToString(); break;
                    case "System.Reflection.AssemblyProductAttribute":
                        Value = ((AssemblyProductAttribute)attrib).Product.ToString(); break;
                    case "System.Reflection.AssemblyTrademarkAttribute":
                        Value = ((AssemblyTrademarkAttribute)attrib).Trademark.ToString(); break;
                    case "System.Reflection.AssemblyTitleAttribute":
                        Value = ((AssemblyTitleAttribute)attrib).Title.ToString(); break;
                    case "System.Resources.NeutralResourcesLanguageAttribute":
                        Value = ((System.Resources.NeutralResourcesLanguageAttribute)attrib).CultureName.ToString(); break;
                    case "System.Resources.SatelliteContractVersionAttribute":
                        Value = ((System.Resources.SatelliteContractVersionAttribute)attrib).Version.ToString(); break;
                    case "System.Runtime.InteropServices.ComCompatibleVersionAttribute":
                        {
                            System.Runtime.InteropServices.ComCompatibleVersionAttribute x;
                            x = ((System.Runtime.InteropServices.ComCompatibleVersionAttribute)attrib);
                            Value = x.MajorVersion + "." + x.MinorVersion + "." + x.RevisionNumber + "." + x.BuildNumber; break;
                        }
                    case "System.Runtime.InteropServices.ComVisibleAttribute":
                        Value = ((System.Runtime.InteropServices.ComVisibleAttribute)attrib).Value.ToString(); break;
                    case "System.Runtime.InteropServices.GuidAttribute":
                        Value = ((System.Runtime.InteropServices.GuidAttribute)attrib).Value.ToString(); break;
                    case "System.Runtime.InteropServices.TypeLibVersionAttribute":
                        {
                            System.Runtime.InteropServices.TypeLibVersionAttribute x;
                            x = ((System.Runtime.InteropServices.TypeLibVersionAttribute)attrib);
                            Value = x.MajorVersion + "." + x.MinorVersion; break;
                        }
                    case "System.Security.AllowPartiallyTrustedCallersAttribute":
                        Value = "(Present)"; break;
                    default:
                        // debug.writeline("** unknown assembly attribute '" + TypeName + "'")
                        Value = TypeName; break;
                }

                if (Name == null)
                    nvc.Add(TypeName, Value);
                else
                    nvc.Add(Name, Value);
            }

            // add some extra values that are not in the AssemblyInfo, but nice to have
            // codebase
            try{
                nvc.Add("CodeBase", a.CodeBase.Replace("file:///", ""));
            }catch(NotSupportedException){
                nvc.Add("CodeBase", "(not supported)");
            }
            // build date
            DateTime dt = AssemblyBuildDate(a, false);
            if (dt == DateTime.MaxValue){
                nvc.Add("BuildDate", "(unknown)");
            }else{
                nvc.Add("BuildDate", dt.ToString("yyyy-MM-dd hh:mm tt"));
            }
            // location
            try{
                nvc.Add("Location", a.Location);
            }catch(NotSupportedException){
                nvc.Add("Location", "(not supported)");
            }
            // version
            try{
                if (a.GetName().Version.Major == 0 && a.GetName().Version.Minor == 0){
                    nvc.Add("Version", "(unknown)");
                }else{
                    nvc.Add("Version", a.GetName().Version.ToString());
                }
            }catch(Exception){
                nvc.Add("Version", "(unknown)");
            }

            nvc.Add("FullName", a.FullName);

            return nvc;
        }



        // <summary>
        // retrieves a cached value from the entry assembly attribute lookup collection
        // </summary>
        private string EntryAssemblyAttrib(string strName)
        {
            if (_EntryAssemblyAttribCollection.ContainsKey(strName))
                return _EntryAssemblyAttribCollection[strName].ToString();

            return "[assembly: Assembly" + strName + "(\"\")]";
        }



        // <summary>
        // perform assemblyinfo to string replacements on labels
        // </summary>
        private string ReplaceTokens(string s)
        {
            s = s.Replace("%title%", EntryAssemblyAttrib("Title"));
            s = s.Replace("%copyright%", EntryAssemblyAttrib("copyright"));
            s = s.Replace("%description%", EntryAssemblyAttrib("description"));
            s = s.Replace("%company%", EntryAssemblyAttrib("company"));
            s = s.Replace("%product%", EntryAssemblyAttrib("product"));
            s = s.Replace("%trademark%", EntryAssemblyAttrib("trademark"));
            s = s.Replace("%year%", DateTime.Now.Year.ToString());
            s = s.Replace("%version%", EntryAssemblyAttrib("version"));
            s = s.Replace("%builddate%", EntryAssemblyAttrib("builddate"));
            return s;
        }

        private bool _IsPainted;

        private void BasicHelp_Paint(object sender, EventArgs e)
        {
            if (!_IsPainted)
            {
                _IsPainted = true;
                this.Icon = this.Owner.Icon;
                ImagePictureBox.Image = Icon.ToBitmap();
            }
        }

        // <summary>
        // things to do when form is loaded
        // </summary>
        private void BasicHelp_Load(object sender, EventArgs e)
        {
            var a = Assembly.GetEntryAssembly();
            _EntryAssemblyAttribCollection = AssemblyAttribs(a);

            this.Text = ReplaceTokens(this.Text);

            AppTitleLabel.Text = ReplaceTokens(AppTitleLabel.Text);
            if (AppDescriptionLabel.Visible)
            {
                AppDescriptionLabel.Text = ReplaceTokens(AppDescriptionLabel.Text);
            }

            string[] resourceNames = a.GetManifestResourceNames();
            if (resourceNames != null && resourceNames.Length > 0)
            {
                var selected = from String n in resourceNames
                    where n == "Ionic.XPathVisualizer.Help.rtf"
                    select n;

                if (selected.Count()!=0)
                {
                    var n = selected.First();
                    var s = a.GetManifestResourceStream(n);
                    var bytes = ReadAllBytes(s);
                    rtbHelp.Rtf = System.Text.Encoding.ASCII.GetString(bytes);
                    s.Close();
                }
            }

            this.ClientSize = new System.Drawing.Size(575, 360);
        }



/// <summary>
/// Reads the contents of the stream into a byte array.
/// data is returned as a byte array. An IOException is
/// thrown if any of the underlying IO calls fail.
/// </summary>
/// <param name="stream">The stream to read.</param>
/// <returns>A byte array containing the contents of the stream.</returns>
/// <exception cref="NotSupportedException">The stream does not support reading.</exception>
/// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
/// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
private byte[] ReadAllBytes(Stream source)
{
    long originalPosition = source.Position;
    source.Position = 0;

    try
    {
        byte[] readBuffer = new byte[4096];

        int totalBytesRead = 0;
        int bytesRead;

        while ((bytesRead = source.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
        {
            totalBytesRead += bytesRead;

            if (totalBytesRead == readBuffer.Length)
            {
                int nextByte = source.ReadByte();
                if (nextByte != -1)
                {
                    byte[] temp = new byte[readBuffer.Length * 2];
                    Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                    Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                    readBuffer = temp;
                    totalBytesRead++;
                }
            }
        }

        byte[] buffer = readBuffer;
        if (readBuffer.Length != totalBytesRead)
        {
            buffer = new byte[totalBytesRead];
            Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
        }
        return buffer;
    }
    finally
    {
        source.Position = originalPosition;
    }
}

        private void rtbHelp_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(e.LinkText);
            }
            catch (Exception)
            {
            }
        }

    }
}