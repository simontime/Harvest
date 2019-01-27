using LibHac;
using LibHac.IO;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using static Utils;
using static WebUtils;

namespace Harvest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var keyFile = Environment.ExpandEnvironmentVariables("%USERPROFILE%/.switch/prod.keys");
            var keysInUserDir = true;

            var getDeltasOnly = false;

            #region Pre-execution checks
            if (!File.Exists(keyFile))
            {
                if (!File.Exists("prod.keys"))
                {
                    Console.Error.WriteLine("Error: prod.keys is missing!");
                    return;
                }
                else keysInUserDir = false;
            }

            if (!File.Exists("edge.token"))
            {
                Console.Error.WriteLine("Error: Please add an edge token to a file named \"edge.token\".");
                return;
            }

            if (!File.Exists("device.id"))
            {
                Console.Error.WriteLine("Error: Please add your device ID in hexadecimal text form to a file named \"device.id\".");
                return;
            }

            if (!File.Exists("nx_tls_client_cert.pfx"))
            {
                Console.Error.WriteLine("Error: Please add your Switch certificate to a file named \"nx_tls_client_cert.pfx\".");
                return;
            }
            #endregion

            var deviceID = Convert.ToInt64(File.ReadAllText("device.id"), 16);
            var keys = ExternalKeys.ReadKeyFile(keysInUserDir ? keyFile : "prod.keys");

            if (args[0] == "-s" || args[0] == "0100000000000816")
                throw new NotImplementedException("System title downloading has not been implemented yet.");

            string id = null,
                   idFirstReq = null,
                   idSecondReq = null;

            List<string> tidTargets = new List<string>(),
                         verTargets = new List<string>(),
                         entryTargets = new List<string>();

            var numOfListings = 0;

            if (args.Length == 2)
            {
                if (args[1] == "-d")
                    getDeltasOnly = true;
                else if (args[0] == "-t")
                {
                    var rightsID = args[1];
                    using (var tikReq = (HttpWebResponse)GET(deviceID, CETKURL(rightsID), false))
                    using (var strm = tikReq.GetResponseStream())
                    using (var rd = new BinaryReader(strm))
                    {
                        File.WriteAllBytes($"{rightsID}.tik", rd.ReadBytes(0x2C0));
                        File.WriteAllBytes($"{rightsID}.cert", rd.ReadBytes(0x700));
                    }

                    return;
                }
            }

            GET(deviceID, Aqua(deviceID), false);

            if (args.Length == 3)
            {
                numOfListings = 2;
                if (args[0].Substring(13, 3) != "000" && args[0].Substring(13, 3) != "800")
                {
                    tidTargets.Add(null);
                    tidTargets.Add(args[0]);
                }
                else
                {
                    tidTargets.Add(null);
                    tidTargets.Add($"{args[0].Substring(0, 13)}800");
                }
                verTargets.Add(null);
                verTargets.Add(args[2]);
            }
            else
            {
                var Streq = (string)GET(deviceID, Superfly(GetBaseTID(args[0])), true);
                if (args[0].Substring(13, 3) == "000")
                {
                    foreach (JToken Listing in JArray.Parse(Streq))
                    if ((string)Listing["title_type"] != "AddOnContent")
                    {
                        numOfListings++;
                        tidTargets.Add(Listing["title_id"].ToString());
                        verTargets.Add(Listing["version"].ToString());
                    }
                }
                else
                {
                    foreach (JToken Listing in JArray.Parse(Streq))
                    if ((string)Listing["title_id"] == args[0])
                    {
                        numOfListings++;
                        tidTargets.Add(Listing["title_id"].ToString());
                        verTargets.Add(Listing["version"].ToString());
                    }
                }
            }

            if (args.Length == 1)
                id = HEAD(deviceID, MetaURL('a', tidTargets[0], verTargets[0], deviceID));

            if (args.Length == 1 && id != null)
                GET(deviceID, ContentURL('a', id), true);

            if (numOfListings >= 2)
                idFirstReq = HEAD(deviceID, MetaURL('a', tidTargets[1], verTargets[1], deviceID));

            if (args.Length == 1 && numOfListings >= 2)
                GET(deviceID, ContentURL('a', idFirstReq), true);

            if (numOfListings >= 2)
                idSecondReq = HEAD(deviceID, MetaURL('a', tidTargets[1], verTargets[1], deviceID));

            if (numOfListings >= 2)
                GET(deviceID, ContentURL('a', idSecondReq), true);

            if (args.Length == 1)
            {
                using (var meta = (HttpWebResponse)GET(deviceID, ContentURL('a', id), false))
                using (var rd = new BinaryReader(meta.GetResponseStream()))
                using (var strm = new MemoryStorage(rd.ReadBytes((int)meta.ContentLength)))
                {
                    Directory.CreateDirectory(tidTargets[0]);
                    strm.WriteAllBytes($"{tidTargets[0]}/{id}.cnmt.nca");
                    using (var nca = new Nca(keys, strm, false).OpenSection(0, false, IntegrityCheckLevel.None, false))
                    {
                        var pfs = new Pfs(nca);
                        var cnmt = new Cnmt(pfs.OpenFile(pfs.Files[0]).AsStream());
                        foreach (var entry in cnmt.ContentEntries)
                        {
                            string NCAID = entry.NcaId.ToHexString().ToLower();
                            using (var Req = (HttpWebResponse)GET(deviceID, ContentURL('c', NCAID), false))
                            using (var Output = File.OpenWrite($"{tidTargets[0]}/{NCAID}.nca"))
                                Req.GetResponseStream().CopyTo(Output);
                        }
                    }
                }
            }

            if (numOfListings >= 2)
            {
                using (var meta = (HttpWebResponse)GET(deviceID, ContentURL('a', idSecondReq), false))
                using (var read = new BinaryReader(meta.GetResponseStream()))
                using (var metastrm = new MemoryStorage(read.ReadBytes((int)meta.ContentLength)))
                {
                    Directory.CreateDirectory(tidTargets[1]);
                    metastrm.WriteAllBytes($"{tidTargets[1]}/{idSecondReq}.cnmt.nca");
                    using (var nca = new Nca(keys, metastrm, false).OpenSection(0, false, IntegrityCheckLevel.None, false))
                    {
                        var pfs = new Pfs(nca);
                        var cnmt = new Cnmt(pfs.OpenFile(pfs.Files[0]).AsStream());
                        foreach (var entry in getDeltasOnly ? 
                            cnmt.ContentEntries.Where(e => e.Type == CnmtContentType.DeltaFragment) :
                            cnmt.ContentEntries)
                        {
                            var ncaID = entry.NcaId.ToHexString().ToLower();
                            using (var request = (HttpWebResponse)GET(deviceID, ContentURL('c', ncaID), false))
                            using (var output = File.OpenWrite($"{tidTargets[1]}/{ncaID}.nca"))
                                request.GetResponseStream().CopyTo(output);

                            if (entry.Type == CnmtContentType.Program)
                            {
                                var rightsID = new Nca(keys, new StreamStorage
                                    (File.OpenRead($"{tidTargets[1]}/{ncaID}.nca"), false), false)
                                    .Header.RightsId.ToHexString().ToLower();

                                using (var tikReq = (HttpWebResponse)GET(deviceID, CETKURL(rightsID), false))
                                using (var strm = tikReq.GetResponseStream())
                                using (var rd = new BinaryReader(strm))
                                {
                                    File.WriteAllBytes($"{tidTargets[1]}/{rightsID}.tik", rd.ReadBytes(0x2C0));
                                    File.WriteAllBytes($"{tidTargets[1]}/{rightsID}.cert", rd.ReadBytes(0x700));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}