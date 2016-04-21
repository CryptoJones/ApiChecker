using System;
using System.CodeDom;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Policy;
using System.Xml;

namespace ApiChecker
{
    internal class ApiChecker
    {
        private static void Main(string[] args)
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

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load("config.xml");
            XmlNamespaceManager xmlnm = new XmlNamespaceManager(xmlDocument.NameTable);
            xmlnm.AddNamespace("ns", "http://www.w3.org/2005/Atom");

            ValidateXml(xmlDocument, xmlnm);

            foreach (Website site in ProgramVariables.WebsiteList)
            {
                CheckSite(site.url, site.ExpectedResponseLength);
            }
        }

        public static void ValidateXml(XmlDocument document, XmlNamespaceManager nsm)
        {
            // Sites List
            XmlNodeList sitesList = document.SelectNodes("//ns:sites", nsm);

            if (sitesList.Count < 1)
            {
                Console.WriteLine("No <sites> entry in config.xml file. ");
                Console.WriteLine("The sites entry should look like the following:");
                Console.WriteLine(" ");
                Console.WriteLine("    <sites>");
                Console.WriteLine("        <site>");
                Console.WriteLine("            <url>http://www.url.com</url>");
                Console.WriteLine("            <expectedreponselength>32</expectedreponselength>");
                Console.WriteLine("        </site>");
                Console.WriteLine("    </sites>");

                Environment.Exit(78);
            }


            foreach (XmlNode sites in sitesList)
            {
                if (sites.ChildNodes.Count < 1)
                {
                    Console.WriteLine("No <site> entry in config.xml file. (This should be inside the <sites> entry!!)");
                    Console.WriteLine(" ");
                    Console.WriteLine("The site entry should look like the following:");
                    Console.WriteLine(" ");
                    Console.WriteLine("        <site>");
                    Console.WriteLine("            <url>http://www.url.com</url>");
                    Console.WriteLine("            <expectedreponselength>32</expectedreponselength>");
                    Console.WriteLine("        </site>");

                    Environment.Exit(78);
                }

                foreach (XmlNode site in sites)
                {
                    Website websiteObject = new Website();

                    if ((site.FirstChild.Name != "url" && site.FirstChild.Name != "expectedresponselength") |
                        (site.LastChild.Name == "url" && site.LastChild.Name == "expectedresponselength") |
                        (site.ChildNodes.Count != 2))
                    {
                        Console.WriteLine("Invalid <site> entry in config.xml file.");
                        Console.WriteLine(" ");
                        Console.WriteLine("The site entry should look like the following:");
                        Console.WriteLine(" ");
                        Console.WriteLine("        <site>");
                        Console.WriteLine("            <url>http://www.url.com</url>");
                        Console.WriteLine("            <expectedreponselength>32</expectedreponselength>");
                        Console.WriteLine("        </site>");

                        Environment.Exit(78);
                    }

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
            try
            {
                XmlNode server = document.SelectSingleNode("//ns:mailserver", nsm);
                ProgramVariables.MailServerAddress = server.InnerXml;
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid or missing <mailserver> entry in config.xml file.");
                Console.WriteLine(" ");
                Console.WriteLine("The <mailserver> entry should look like the following:");
                Console.WriteLine(" ");
                Console.WriteLine("    <mailserver>127.0.0.1</mailserver>");

                Environment.Exit(78);
            }


            // Email Recipients
            try
            {
                XmlNode recipients = document.SelectSingleNode("//ns:recipients", nsm);

                if (recipients.ChildNodes.Count == 0)
                {
                    Console.WriteLine("Invalid or missing <recipient> entry in config.xml file.");
                    Console.WriteLine(" ");
                    Console.WriteLine("The <recipient> entry should look like the following:");
                    Console.WriteLine(" ");
                    Console.WriteLine("<recipient>user@domain.com</recipient>");
                    Environment.Exit(78);
                }

                foreach (XmlNode recipient in recipients)
                {
                    ProgramVariables.EmaiList.Add(recipient.InnerText);
                }
            }
            catch
            {
                Console.WriteLine("Invalid or missing <recipients> entry in config.xml file.");
                Console.WriteLine(" ");
                Console.WriteLine("The <recipients> entry should look like the following:");
                Console.WriteLine(" ");
                Console.WriteLine("    <recipients>");
                Console.WriteLine("        <recipient>user@domain.com</recipient>");
                Console.WriteLine("    </recipients>");
                Environment.Exit(78);
            }
        }

        public static void CheckSite(string url, int expectedLength)
        {
            try
            {
                WebRequest wr = WebRequest.Create(url);
                ((HttpWebRequest) wr).UserAgent = "API Checker 1.0/(.NET Runtime 4.x)";
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
                Alert(ex + " ", url);
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