using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("LaTeX Build Component")]
[assembly: AssemblyDescription("A LaTeX Build Component for the SandCastle Help File Builder.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Marcus Cuda")]
[assembly: AssemblyProduct("LaTeX Build Component")]
[assembly: AssemblyCopyright(AssemblyInfo.Copyright)]
[assembly: AssemblyTrademark("http://marcuscuda.com")]
[assembly: AssemblyCulture("")]

// Resources contained within the assembly are English
[assembly: NeutralResourcesLanguageAttribute("en")]

[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]

// All version numbers for an assembly consists of the following four values:
//
//      Year of release     (Matches SHFB release)
//      Month of release    (Matches SHFB release)
//      Day of release      (Matches SHFB release)
//      Revision            (Revision of the plug-in)
//
[assembly: AssemblyVersion(AssemblyInfo.ProductVersion)]
[assembly: AssemblyFileVersion(AssemblyInfo.ProductVersion)]

internal static class AssemblyInfo
{
    internal const string Copyright = "Copyright \xA9 Marcus Cuda 2008-2015";

    internal const string ProductVersion = "2015.7.25.0";
}
