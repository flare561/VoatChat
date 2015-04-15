using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using System.Net;
using HtmlAgilityPack;
using System.IO;
using ChatSharp;
using System.Text.RegularExpressions;

namespace VoatChat
{
    class Program
    {
        //TODO Error handling in IRC connection
        //TODO nickserv registration 
        static Config conf = new Config();
        static IHubProxy hubProx;
        static IrcClient client;
        static int unsent = 0;
        static HubConnection hubConn;

        static void Main(string[] args)
        {
            if (!conf.LoadConfig())
                conf.SetInitialProperties();
            hubConn = new HubConnection("https://voat.co/");
            hubProx = hubConn.CreateHubProxy("messagingHub");

            var cookies = AuthenticateUser(conf.Username, conf.Password);
            hubConn.CookieContainer = cookies;
            

            hubProx.On<string, string>("appendChatMessage", (x,y) => 
                {
                    if (x != conf.Username)
                    {
                        var msg = string.Format("<{0}> {1}", HtmlEntity.DeEntitize(x), HtmlEntity.DeEntitize(y));
                        client.SendMessage(msg, conf.Channel);
                    }
                    else unsent = Math.Min(unsent-1, 0);
                });

            Console.Write("Username: {0}\nPassword: {1}\nSubverse: {2}\nNetwork: {3}\nNick: {4}\nChannel: {5}\n", conf.Username, conf.Password, conf.Subverse, conf.Network, conf.Nick, conf.Channel);

            hubConn.Start().Wait();
            hubProx.Invoke("JoinSubverseChatRoom", conf.Subverse);

            var regInfo = new IrcUser(conf.Nick, "voatbot", "", "voatbot");
            client = new IrcClient(conf.Network, regInfo);
            client.ConnectionComplete += (sender, e) => { client.JoinChannel(conf.Channel); };
            client.ChannelMessageRecieved += client_ChannelMessageRecieved;
            client.ConnectAsync();
            while (true)
            {
                System.Threading.Thread.Sleep(100);
            }
        }

        static void client_ChannelMessageRecieved(object sender, ChatSharp.Events.PrivateMessageEventArgs e)
        {
            if (e.PrivateMessage.User.Nick != conf.Nick)
            {
                var pattern = "[\\x02\\x1F\\x0F\\x16]|\\x03(\\d\\d?(,\\d\\d?)?)?";
                Regex reg = new Regex(pattern);
                string msg = "";
                if (!e.PrivateMessage.Message.Contains("\x0001ACTION"))
                    msg = string.Format("<{0}> {1}", e.PrivateMessage.User.Nick, reg.Replace(e.PrivateMessage.Message, ""));
                else
                    msg = string.Format("*{0} {1}", e.PrivateMessage.User.Nick, reg.Replace(e.PrivateMessage.Message, "").Replace("\x0001ACTION", "").Replace("\x0001", ""));

                try
                {
                    hubProx.Invoke("SendChatMessage", conf.Username, msg, conf.Subverse);
                    unsent++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Couldn't send chat message: " + ex.Message);
                    Console.WriteLine(msg);
                }
                if (unsent > 5)
                {
                    unsent=0;
                    hubConn.Stop();
                    hubConn.Start().Wait();
                    hubProx.Invoke("JoinSubverseChatRoom", conf.Subverse);
                }
            }
        }

        private static CookieContainer AuthenticateUser(string user, string password)
        {
            var cookies = new CookieContainer();
            var mainReq = WebRequest.Create("https://voat.co/") as HttpWebRequest;
            mainReq.Method = "GET";
            mainReq.CookieContainer = cookies;

            var mainResp = mainReq.GetResponse() as HttpWebResponse;
            var stream = mainResp.GetResponseStream();
            string html = "";
            using (var reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
                reader.Dispose();
            }
            stream.Dispose();
            mainResp.Dispose();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var reqVerTok = doc.DocumentNode.SelectNodes("//input[@name='__RequestVerificationToken']").First().Attributes["value"].Value;
            var request = WebRequest.Create("https://voat.co/account/login") as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = cookies;

            var qs = System.Web.HttpUtility.ParseQueryString(string.Empty);
            qs.Add("__RequestVerificationToken", reqVerTok);
            qs.Add("UserName", user);
            qs.Add("Password", password);
            qs.Add("RememberMe", "true");

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(qs.ToString());
            request.ContentLength = bytes.Length;
            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(bytes, 0, bytes.Length);
            }

            using (var response = request.GetResponse() as HttpWebResponse)
            {
                return cookies;
            }

        }
    }
}
