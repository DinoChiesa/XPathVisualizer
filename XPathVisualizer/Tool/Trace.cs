//#define Trace

// Trace.cs

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;         // for Conditional


namespace XPathVisualizer
{
    public partial class XPathVisualizerTool
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int pid);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern bool FreeConsole();

        /// <summary>
        /// This pops a console window to emit debugging messages into,
        /// at runtime.  It is compiled with Conditiona("Trace") so these messages
        /// never appear when Trace is not #define'd. 
        /// </summary>
        [Conditional("Trace")]
        private void SetupDebugConsole()
        {
            if ( !AttachConsole(-1) )  // Attach to a parent process console
                AllocConsole();        // Allocate a new console

            //_process= System.Diagnostics.Process.GetCurrentProcess();
            System.Console.WriteLine();
        }

    
        [Conditional("Trace")]
        private void Trace(string format, params object[] args)
        {
            // these messages appear in the allocated console.
            //System.Console.Write("{0:D5} ", _process.Id);
            System.Console.WriteLine(format, args);
        }

    }
}
