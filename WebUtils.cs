using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;

class WebUtils
{
    readonly static X509Certificate2 Cert = new X509Certificate2("nx_tls_client_cert.pfx", "switch");
    const string Edge = "X-Nintendo-DenebEdgeToken", ETN = "edge.token", All = "*/*";

    public static object GET(long deviceID, string url, bool returnAsString)
    {
        Console.WriteLine("GET {0}", url);

        ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        var Request = (HttpWebRequest)WebRequest.Create(url);

        Request.ClientCertificates.Add(Cert);
        Request.Accept = All;
        Request.UserAgent = Utils.UA(deviceID);
        Request.AddRange(0);

        if (!url.Contains("aqua"))
            Request.Headers.Add(Edge, File.ReadAllText(ETN));

        if (returnAsString)
            using (var Response = (HttpWebResponse)Request.GetResponse())
            using (var Read = new StreamReader(Response.GetResponseStream()))
                return Read.ReadToEnd();
        else return (HttpWebResponse)Request.GetResponse();
    }

    public static string HEAD(long deviceID, string url)
    {
        Console.WriteLine("HEAD {0}", url);

        ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        var Request = (HttpWebRequest)WebRequest.Create(url);

        Request.ClientCertificates.Add(Cert);
        Request.Method = "HEAD";
        Request.Accept = All;
        Request.UserAgent = Utils.UA(deviceID);
        Request.Headers.Add(Edge, File.ReadAllText(ETN));

        using (var Response = (HttpWebResponse)Request.GetResponse())
            return Response.GetResponseHeader("X-Nintendo-Content-ID");
    }
}