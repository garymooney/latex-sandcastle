/*
 * Copyright 2008-2015 Marcus Cuda - http://marcuscuda.com
 *
 *  This file is part of LaTeX Build Component.
 *
 *  LaTeX Build Component is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  LaTeX Build Component is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with LaTeX Build Component.  If not, see <http://www.gnu.org/licenses/>.
 *   
 * 08/30/2015 - EFW - Updated for use with the latest release of the Sandcastle Help File Builder
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

using Sandcastle.Core.BuildAssembler;
using Sandcastle.Core.BuildAssembler.BuildComponent;
using WpfMath;

namespace LatexBuildComponent
{
    /// <summary>
    /// Converts the code inside a latex tag into a GIF
    /// </summary>
    /// <example>
    /// <latex>f(x)=x^2</latex> 
    /// </example>
    public class LatexBuildComponent : BuildComponentCore
    {
        #region Build component factory for MEF
        //=====================================================================

        /// <summary>
        /// This is used to create a new instance of the build component used to generate LaTeX images
        /// </summary>
        [BuildComponentExport("LaTeX Build Component", IsVisible = true, IsConfigurable = true,
          Version = AssemblyInfo.ProductVersion, Copyright = AssemblyInfo.Copyright,
          Description = "This build component converts LaTeX code into images")]
        public sealed class Factory : BuildComponentFactory
        {
            /// <summary>
            /// Constructor
            /// </summary>
            public Factory()
            {
                base.ReferenceBuildPlacement = new ComponentPlacement(PlacementAction.Before,
                    "XSL Transform Component");
                base.ConceptualBuildPlacement = new ComponentPlacement(PlacementAction.Before,
                    "XSL Transform Component");
            }

            /// <inheritdoc />
            public override BuildComponentCore Create()
            {
                return new LatexBuildComponent(base.BuildAssembler);
            }

            /// <inheritdoc />
            public override string DefaultConfiguration
            {
                get
                {
                    return @"<helpType value=""{@HelpFileFormat}"" />
<basePath value=""{@WorkingFolder}"" />
<fontSize value=""3"" />";
                }
            }

            /// <inheritdoc />
            public override string ConfigureComponent(string currentConfiguration, CompositionContainer container)
            {
                // Open the dialog to edit the configuration
                using(var dlg = new LatexConfigDlg(currentConfiguration))
                {
                    // Get the modified configuration if OK was clicked
                    if(dlg.ShowDialog() == DialogResult.OK)
                    {
                        _fontSize = dlg.FontSize;

                        var config = XElement.Parse(currentConfiguration);

                        config.Element("fontSize").Attribute("value").SetValue(_fontSize);
                        currentConfiguration = config.ToString();
                    }
                }

                // Return the configuration data
                return currentConfiguration;
            }
        }
        #endregion

        #region Private data members
        //=====================================================================

        private readonly UnicodeEncoding _encoding = new UnicodeEncoding();
        private readonly SHA256 _hasher = new SHA256CryptoServiceProvider();
        private readonly IDictionary<byte[], string> _imgNameCache = new Dictionary<byte[], string>(new KeyComparer());

        private string[] _paths;
        private uint _count = 1;
        private static string _fontSize = "3";

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="assembler">A reference to the build assembler.</param>
        /// <param name="configuration">The configuration information</param>
        /// <exception cref="System.Configuration.ConfigurationErrorsException">This is thrown if an error is detected in the
        /// configuration.</exception>
        public LatexBuildComponent(BuildAssemblerCore assembler) : base(assembler)
        {
        }
        #endregion

        #region Method overrides
        //=====================================================================

        /// <inheritdoc />
        public override void Initialize(XPathNavigator configuration)
        {
            var nav = configuration.SelectSingleNode("fontSize");

            if(nav != null)
                _fontSize = nav.GetAttribute("value", String.Empty);

            _paths = GetWorkingDirectories(configuration);
        }

        /// <summary>
        /// This is implemented to perform the component tasks.
        /// </summary>
        /// <param name="document">The XML document with which to work.</param>
        /// <param name="key">The key (member name) of the item being documented.</param>
        public override void Apply(XmlDocument document, string key)
        {
            var latexList = document.SelectNodes("//latex");

            if(latexList == null)
                return;

            foreach(XmlNode code in latexList)
            {
                var attr = code.Attributes.GetNamedItem("expr");
                var expression = attr == null ? code.InnerText : attr.InnerText;

                if(String.IsNullOrWhiteSpace(expression))
                    continue;

                bool svgOut = true;

                var result = GetImageName(expression, svgOut);
                var filename = result.Item2;
                var filenamePng = Path.ChangeExtension(filename, ".png");

                var parser = new TexFormulaParser();

                if (!result.Item1)
                {
                    foreach(var path in _paths)
                    {
                        //TODO: copy instead of running for each path
                        //SafeNativeMethods.CreateGifFromEq(expression, path + filename, _fontSize);
                        //
                        //const string latex = @"\frac{2+2}{2}";
                        //const string fileName = @"T:\Temp\formula.png";
                        TexFormula formula;
                        try
                        {
                            formula = parser.Parse(expression);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("An error occurred while parsing the given input: " + ex.Message);
                            continue;
                        }

                        if (svgOut)
                        {
                            // Output png for fallback.
                            var renderer = formula.GetRenderer(TexStyle.Display, 20, "Arial");
                            var bitmap = renderer.RenderToBitmap(0, 0);
                            double width = bitmap.Width;
                            double height = bitmap.Height;

                            renderer = formula.GetRenderer(TexStyle.Display, 20, "Arial");
                            var geometry = renderer.RenderToGeometry(0, 0);
                            var converter = new SVGConverter();
                            var svgPathText = converter.ConvertGeometry(geometry);
                            var svgText = AddSVGHeader(svgPathText, width, height);
                            using (var stream = new FileStream(path + filename, FileMode.Create))
                            {
                                using (var writer = new StreamWriter(stream))
                                    writer.WriteLine(svgText);
                            }

                            // Output png for fallback.
                            var pngBytes = formula.RenderToPng(20.0, 0.0, 0.0, "Arial");
                            File.WriteAllBytes(path + filenamePng, pngBytes);
                        }
                        else
                        {
                            var pngBytes = formula.RenderToPng(20.0, 0.0, 0.0, "Arial");
                            File.WriteAllBytes(path + filename, pngBytes);
                        }
                    }
                }



                var src = document.CreateAttribute("src");
                src.Value = "../html/" + filename;
                var hspace = document.CreateAttribute("hspace");
                hspace.Value = "0";
                var vspace = document.CreateAttribute("vspace");
                vspace.Value = "0";
                var align = document.CreateAttribute("align");
                align.Value = "bottom";
                XmlNode img = document.CreateElement("img");
                img.Attributes.Append(src);
                img.Attributes.Append(hspace);
                img.Attributes.Append(vspace);
                img.Attributes.Append(align);
                if (svgOut)
                {
                    // Add a png fallback
                    var onError = document.CreateAttribute("onerror");
                    onError.Value = "this.src='../html/" + filenamePng + "'";
                    img.Attributes.Append(onError);
                }
                code.ParentNode.ReplaceChild(img, code);
            }
        }
        #endregion

        private string AddSVGHeader(string svgText, double width, double height)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>")
                //.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" style=\"display: block;\" viewBox=\"0 0 " + (int)width + " " + (int)height + "\">")
                .AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" width=\"" + (width*0.9) + "\" height=\"" + (height*0.9) + "\" viewBox=\"0 0 " + width + " " + height + "\">")
                .AppendLine(svgText)
                .AppendLine("</svg>");

            return builder.ToString();
        }

        private string AddSVGHeader(string svgText)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>")
                .AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\">")
                .AppendLine(svgText)
                .AppendLine("</svg>");

            return builder.ToString();
        }

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// Returns the image name from the cache if exists, otherwise creates one, places it in the cache
        /// and returns in.
        /// </summary>
        private Tuple<bool, string> GetImageName(string xml, bool whetherSvg)
        {
            var hash = _hasher.ComputeHash(_encoding.GetBytes(xml));

            if(_imgNameCache.ContainsKey(hash))
            {
                return new Tuple<bool, string>(true, _imgNameCache[hash]);
            }

            string filename;
            if (whetherSvg)
            {
                filename = "img_" + _count++ + ".svg";
            }
            else
            {
                filename = "img_" + _count++ + ".png";
            }
            _imgNameCache.Add(hash, filename);
            return new Tuple<bool, string>(false, filename);
        }

        private static string GetBasePath(XPathNavigator configuration)
        {
            var basePath = ".\\";
            var nav = configuration.SelectSingleNode("basePath");
            if(nav != null)
            {
                basePath = nav.GetAttribute("value", String.Empty);
            }

            return basePath;
        }

        private static string[] GetWorkingDirectories(XPathNavigator configuration)
        {
            var basePath = GetBasePath(configuration);
            var nav = configuration.SelectSingleNode("helpType");

            if(nav == null)
                throw new ArgumentException("helpType not specified in the configuration file.");

            var selected = nav.GetAttribute("value", String.Empty);
            var types = selected.Split(',');

            var paths = new string[types.Length];

            for(var i = 0; i < paths.Length; i++)
            {
                var type = types[i].Trim();

                string path;
                if(type.Equals("HtmlHelp1", StringComparison.InvariantCultureIgnoreCase))
                {
                    path = @"Output\HtmlHelp1\Html\";
                }
                else if(type.Equals("Website", StringComparison.InvariantCultureIgnoreCase))
                {
                    path = @"Output\Website\Html\";
                }
                else if(type.Equals("MSHelpViewer", StringComparison.InvariantCultureIgnoreCase))
                {
                    path = @"Output\MSHelpViewer\Html\";
                }
                else
                {
                    throw new ArgumentException(String.Format("{0} is not a supported help file format.", type));
                }
                paths[i] = basePath + path;
            }

            return paths;
        }
        #endregion

        #region Nested type: KeyComparer

        private class KeyComparer : IEqualityComparer<byte[]>
        {
            #region IEqualityComparer<byte[]> Members

            public bool Equals(byte[] left, byte[] right)
            {
                return left.SequenceEqual(right);
            }

            public int GetHashCode(byte[] key)
            {
                return key.Sum(b => b);
            }

            #endregion
        }

        #endregion
    }
}