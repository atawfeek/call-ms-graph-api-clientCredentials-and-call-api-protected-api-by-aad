/*
 The MIT License (MIT)

Copyright (c) 2015 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
#if VariationWithCertificateCredentials
using System.Security.Cryptography.X509Certificates;
#endif 
using System.Threading.Tasks;
using Microsoft.Identity.Client.AppConfig;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Xml;
using System.Reflection;

namespace daemon_console
{
    /// <summary>
    /// This sample shows how to query the Microsoft Graph from a daemon application
    /// which uses application permissions.
    /// For more information see https://aka.ms/msal-net-client-credentials
    /// 
    /// https://docs.microsoft.com/en-us/graph/api/user-list-memberof?view=graph-rest-1.0
    /// </summary>
    class Program
    {
        public object HttpContext { get; private set; }

        static void Main(string[] args)
        {
            try
            {
                string userProperties = LoadUserProperties();
                Console.WriteLine("Please enter user principal name to get required properties: ");
                var userPrincipalName = "abdelrahamn.tawfik@vodafone.com"; //Console.ReadLine();
                Console.WriteLine("abdelrahamn.tawfik@vodafone.com");


                //Call Microsoft Graph API
                RunAsync(userPrincipalName, userProperties).GetAwaiter().GetResult();

                //Call my custom API protected by AAD
                //ExecuteLandingPageMiddlewareAPIAsync(userPrincipalName).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static async Task RunAsync(string userPrincipalName, string userProperties)
        {
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json", "MicrosoftGraphAPI");

            // Even if this is a console application here, a daemon application is a confidential client application
            IConfidentialClientApplication app;

#if !VariationWithCertificateCredentials
            app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                .WithClientSecret(config.ClientSecret)
                .WithAuthority(new Uri(config.Authority))
                .Build();
#else
            X509Certificate2 certificate = ReadCertificate(config.CertificateName);
            app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                .WithCertificate(certificate)
                .WithAuthority(new Uri(config.Authority))
                .Build();
#endif

            // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
            // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
            // a tenant administrator
            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

            AuthenticationResult result = null;
            try
            {
                result = await app.AcquireTokenForClient(scopes)
                    .ExecuteAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Token acquired");
                Console.ResetColor();
            }
            catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
            {
                // Invalid scope. The scope has to be of the form "https://resourceurl/.default"
                // Mitigation: change the scope to be as expected
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Scope provided is not supported");
                Console.ResetColor();
            }

            if (result != null)
            {
                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                Console.WriteLine(await GetAPIUrl(userPrincipalName));
                await apiCaller.CallWebApiAndProcessResultASync(await GetAPIUrl(userPrincipalName), result.AccessToken, Display, HttpMethod.Get);

                //get user groups
                Console.WriteLine(await GetGroupsAPIUrl(userPrincipalName));
                await apiCaller.CallWebApiAndProcessResultASync(await GetGroupsAPIUrl(userPrincipalName), result.AccessToken, Display, HttpMethod.Post);

                //get user manager
                Console.WriteLine(await GetManagerAPIUrl(userPrincipalName));
                await apiCaller.CallWebApiAndProcessResultASync(await GetManagerAPIUrl(userPrincipalName), result.AccessToken, Display, HttpMethod.Get);

                //get all users
                Console.WriteLine(await GetAllUsersAPIUrl(""));
                await apiCaller.CallWebApiAndProcessResultASync(await GetAllUsersAPIUrl(""), result.AccessToken, ExportUsers, HttpMethod.Get);
            }
        }

        private static async Task ExecuteLandingPageMiddlewareAPIAsync(string userPrincipalName)
        {
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json", "TodoListService");

            // Even if this is a console application here, a daemon application is a confidential client application
            IConfidentialClientApplication app;

#if !VariationWithCertificateCredentials
            app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                .WithClientSecret(config.ClientSecret)
                .WithAuthority(new Uri(config.Authority))
                .Build();
#else
            X509Certificate2 certificate = ReadCertificate(config.CertificateName);
            app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                .WithCertificate(certificate)
                .WithAuthority(new Uri(config.Authority))
                .Build();
#endif

            // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
            // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
            // a tenant administrator
            string[] scopes = new string[] { "https://vodafoneitc.onmicrosoft.com/dc0b1790-bf49-4501-bf20-cedb62af0b6c/.default" };

            AuthenticationResult result = null;
            try
            {
                result = await app.AcquireTokenForClient(scopes)
                    .ExecuteAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Token acquired");
                Console.ResetColor();
            }
            catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
            {
                // Invalid scope. The scope has to be of the form "https://resourceurl/.default"
                // Mitigation: change the scope to be as expected
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Scope provided is not supported");
                Console.ResetColor();
            }

            if (result != null)
            {
                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                Console.WriteLine(await GetLandingPageMiddlewareAPIUrl(userPrincipalName));
                await apiCaller.CallWebApiAndProcessResultASync(await GetLandingPageMiddlewareAPIUrl(userPrincipalName), result.AccessToken, Display, HttpMethod.Get);
            }
        }


        static private async Task<string> GetAllUsersAPIUrl(string userPrincipalName)
        {
            StringBuilder graphEndPoint = new StringBuilder();
            graphEndPoint.Append("https://graph.microsoft.com/v1.0/users");
            if (!string.IsNullOrWhiteSpace(userPrincipalName))
                graphEndPoint.Append("/" + userPrincipalName);
            graphEndPoint.Append("?$select=" + "userPrincipalName");

            return graphEndPoint.ToString();
        }

        static private async Task<string> GetAPIUrl(string userPrincipalName)
        {
            StringBuilder graphEndPoint = new StringBuilder();
            graphEndPoint.Append("https://graph.microsoft.com/v1.0/users");
            if (!string.IsNullOrWhiteSpace(userPrincipalName))
                graphEndPoint.Append("/" + userPrincipalName);
            graphEndPoint.Append("?$select=" + LoadUserProperties());

            return graphEndPoint.ToString();
        }

        static private async Task<string> GetGroupsAPIUrl(string userPrincipalName)
        {
            if (!string.IsNullOrWhiteSpace(userPrincipalName))
                return "https://graph.microsoft.com/v1.0/users/" + userPrincipalName + "/getMemberGroups";
            else
                return string.Empty;
        }

        static private async Task<string> GetManagerAPIUrl(string userPrincipalName)
        {
            if (!string.IsNullOrWhiteSpace(userPrincipalName))
                return "https://graph.microsoft.com/v1.0/users/" + userPrincipalName + "/manager";
            else
                return string.Empty;
        }

        static private async Task<string> GetLandingPageMiddlewareAPIUrl(string userPrincipalName)
        {
            StringBuilder graphEndPoint = new StringBuilder();
            graphEndPoint.Append("https://localhost:44351/api/todolist");
            if (!string.IsNullOrWhiteSpace(userPrincipalName))
                graphEndPoint.Append("?user=" + userPrincipalName);

            return graphEndPoint.ToString();
        }

        static private string LoadUserProperties()
        {
            StringBuilder userProperties = new StringBuilder();

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"UserProperties.xml");
            string xmlString = File.ReadAllText(path);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlString);

            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                foreach(XmlNode property in node.ChildNodes)
                {
                    userProperties.Append(property.InnerText);
                    userProperties.Append(",");
                }
            }

            return userProperties.ToString().Remove(userProperties.ToString().Count()-1);
        }

        /// <summary>
        /// Display the result of the Web API call
        /// </summary>
        /// <param name="result">Object to display</param>
        private static void Display(JObject result)
        {
            foreach (JProperty child in result.Properties().Where(p => !p.Name.StartsWith("@")))
            {
                Console.WriteLine($"{child.Name} = {child.Value}");
            }
        }

        private static void ExportUsers(JObject result)
        {
            //before your loop
            var csv = new StringBuilder();

            foreach (JProperty child in result.Properties().Where(p => !p.Name.StartsWith("@")))
            {
                //in your loop

                var first = child.Value.ToString();
                //Suggestion made by KyleMit
                var newLine = $"{first}";
                csv.AppendLine(newLine);
            }

            //after your loop
            File.WriteAllText(@"C:\external\active-directory-dotnetcore-daemon-v2-master\daemon-console\allUsers.csv", csv.ToString());
        }

#if VariationWithCertificateCredentials
        private static X509Certificate2 ReadCertificate(string certificateName)
        {
            if (string.IsNullOrWhiteSpace(certificateName))
            {
                throw new ArgumentException("certificateName should not be empty. Please set the CertificateName setting in the appsettings.json", "certificateName");
            }
            X509Certificate2 cert = null;

            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certCollection = store.Certificates;

                // Find unexpired certificates.
                X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

                // From the collection of unexpired certificates, find the ones with the correct name.
                X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certificateName, false);

                // Return the first certificate in the collection, has the right name and is current.
                cert = signingCert.OfType<X509Certificate2>().OrderByDescending(c => c.NotBefore).FirstOrDefault();
            }
            return cert;
        }
#endif
    }
}
