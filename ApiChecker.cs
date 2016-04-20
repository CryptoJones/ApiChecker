using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Policy;
using System.Xml;

namespace ApiChecker
{
    public static class ProgramVariables
    {

        public static List<website> WebsiteList = new List<website>();
        public static string MailServerAddress;
        public static List<string> EmaiList = new List<string>();

    }

    public class website
    {
        public int ExpectedResponseLength { get; set; }
        public string url { get; set; }
    }
    class ApiChecker
    {
        static void Main(string[] args)
        {

            ParseXML();         

            foreach (website site in ProgramVariables.WebsiteList)
            {
                CheckSite(site.url, site.ExpectedResponseLength);
            }

        }

        public static void ParseXML()
        {
            if (!File.Exists("config.xml"))
            {
                Console.WriteLine("The config.xml file is missing. It should be in the following format:");
                Console.WriteLine(" ");
                Console.WriteLine("<?xml version=\"1.0\" encoding=\"utf - 8\"?>");
                Console.WriteLine("<feed xmlns=\"http://www.w3.org/2005/Atom\">");
                Console.WriteLine("    <sites>");
                Console.WriteLine("        <site>");
                Console.WriteLine("            <url>http://www.url.com</url>");
                Console.WriteLine("            <expectedreponselength>32</expectedreponselength>");
                Console.WriteLine("        </site>");
                Console.WriteLine("        <site>");
                Console.WriteLine("            <url>http://www.url2.com</url>");
                Console.WriteLine("            <expectedreponselength>32</expectedreponselength>");
                Console.WriteLine("        </site>");
                Console.WriteLine("    </sites>");
                Console.WriteLine("    <mailserver>127.0.0.1</mailserver> ");
                Console.WriteLine("    <recipients>");
                Console.WriteLine("        <recipient>user@domain.com</recipient>");
                Console.WriteLine("    </recipients>");
                Console.WriteLine("</feed>");
                Environment.Exit(78);


            }


            var xmlDocument = new XmlDocument();
            xmlDocument.Load("config.xml");
            var xmlnm = new XmlNamespaceManager(xmlDocument.NameTable);
            xmlnm.AddNamespace("ns", "http://www.w3.org/2005/Atom");

            // Sites List
            var sitesList = xmlDocument.SelectNodes("//ns:sites", xmlnm);

            foreach (XmlNode sites in sitesList)
            {
                foreach (XmlNode site in sites)
                {
                    var websiteObject = new website();

                    foreach (XmlNode siteElement in site)
                    {
                        if (siteElement.Name == "url")
                        {
                            websiteObject.url = siteElement.InnerXml;
                        }

                        if (siteElement.Name == "expectedreponselength")
                        {
                            websiteObject.ExpectedResponseLength = Convert.ToInt32(siteElement.InnerXml);
                        }
                    }

                    ProgramVariables.WebsiteList.Add(websiteObject);
                }
            }

            // Mail Server Address
            var server = xmlDocument.SelectSingleNode("//ns:mailserver", xmlnm);
            ProgramVariables.MailServerAddress = server.InnerXml;

            // Email Recipients
            var recipients = xmlDocument.SelectSingleNode("//ns:recipients", xmlnm);

            foreach (XmlNode recipient in recipients)
            {
                    ProgramVariables.EmaiList.Add(recipient.InnerText);
            }
        }

        public static void CheckSite(string url, int expectedLength)
        {
            try
            {
                WebRequest wr = WebRequest.Create(url);
                ((HttpWebRequest) wr).UserAgent = "API Checker/1.0 (.NET Runtime 4.x)";
                WebResponse response = wr.GetResponse();

                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                String responseFromServer = reader.ReadToEnd();
                response.Close();

                if (responseFromServer.Length != expectedLength)
                {
                    Alert("Unexpected reponse length from API at: ", url);
                }
            }
            catch (Exception ex)
            {
                Alert(ex.ToString(), url);
            }
        }

        public static void Alert(string message, string site)
        {

            MailMessage newmessage = new MailMessage();
            newmessage.From = new MailAddress("noreply@domain.com");
            newmessage.Subject = "API Alert";
            newmessage.Body = message + site;

            foreach (string email in ProgramVariables.EmaiList)
            {
               newmessage.To.Add(email);
            } 
            
            SmtpClient client = new SmtpClient(ProgramVariables.MailServerAddress);
            client.Send(newmessage);

        }
    }
}