using Gitle.Model.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Gitle.Service
{
    public class PdfExportService : IPdfExportService
    {
        protected readonly ITemplateParserService templateParserService;
        private readonly string wkhtmltopdfPath;

        public PdfExportService(ITemplateParserService templateParserService, string wkhtmltopdfPath)
        {
            this.templateParserService = templateParserService;
            this.wkhtmltopdfPath = wkhtmltopdfPath;
        }

        public byte[] ConvertHtmlToPdf(string template, Dictionary<string, object> propertyBag)
        {
            var html = templateParserService.Parse(template, propertyBag);
            // negeer zowel de standaard input als de standaard output.
            // dit gaan we redirecten naar streams die we kunnen aanspreken.
            //var arguments = "--header-font-name Georgia --header-font-size 10 --footer-font-name Georgia --footer-font-size 9 -B 15mm -T 15mm -L 15mm -R 15mm --header-spacing 5 --footer-spacing 10 ";
            //if (propertyBag.ContainsKey("headerLeft"))
            //{
            //    arguments += "--header-left " + propertyBag["headerLeft"] + "\" ";
            //}
            //if (propertyBag.ContainsKey("headerRight"))
            //{
            //    arguments += "--header-right \"" + propertyBag["headerRight"] + "\" ";
            //}
            //if (propertyBag.ContainsKey("footerLeft"))
            //{
            //    arguments += "--footer-left \"" + propertyBag["footerLeft"] + "\" ";
            //}
            //if (propertyBag.ContainsKey("footerCenter"))
            //{
            //    arguments += "--footer-center \"" + propertyBag["footerCenter"] + "\" ";
            //}
            //if (propertyBag.ContainsKey("footerRight"))
            //{
            //    arguments += "--footer-right \"" + propertyBag["footerRight"] + "\" ";
            //}

            var arguments = "--zoom 0.8 - -";

            return ConvertHtmlToPdf(html, arguments);
        }

        private byte[] ConvertHtmlToPdf(string html, string arguments)
        {
            var startInfo = new ProcessStartInfo(wkhtmltopdfPath)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                Arguments = arguments
            };

            int returnCode;
            byte[] buffer;

            //Process.Start(startInfo);
            using (var process = Process.Start(startInfo))
            using (var myOutput = process.StandardOutput)
            {
                //StreamReader myErrors = process.StandardError;
                var myWriter = process.StandardInput;
                try
                {
                    //schijf de input string naar de inputstream van het process
                    myWriter.AutoFlush = true;
                    myWriter.Write(html);
                    myWriter.Close();

                    var output = myOutput.ReadToEnd();
                    //errorLines = myErrors.ReadToEnd();

                    buffer = process.StandardOutput.CurrentEncoding.GetBytes(output);

                    returnCode = process.ExitCode;
                }
                finally
                {
                    myWriter.Close();
                }

            }
            if (returnCode != 0)
            {
                throw new Exception("Er is iets mis gegaan bij het creeren van de PDF");
            }

            return buffer;
        }
    }
}
