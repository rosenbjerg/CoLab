using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using RHttpServer;

namespace CoLab
{
    class Program
    {
        static readonly ConcurrentDictionary<string, Session> _sessions = new ConcurrentDictionary<string, Session>();
        static void Main(string[] args)
        {
            const string dir = "files";

            var server = new TaskBasedHttpServer(5005);
            var db = new LiteDatabase("db.lite").GetCollection<User>("User");
            var rsa = CryptoRsa.GenerateKeyPair();
            var pub = rsa.Item1;
            var priv = rsa.Item2;
            var activeFiles = new ConcurrentDictionary<string, List<WebSocketDialog>>();
            var openEditableFiles = new ConcurrentDictionary<string, EditableFile>();
            var sb = new StringBuilder(System.IO.File.ReadAllText("./test.txt"));

            
            server.Get("/project/:team/:project", (req, res) =>
            {
                if (!VerifyUser(req, res)) return;
            });

            server.Get("/login", (req, res) =>
            {
                
            });

            server.Post("/login", async (req, res) =>
            {
                var uq = req.GetBodyPostFormData();
                var um = CryptoRsa.Decrypt(uq["u"], priv);
                var pwd = CryptoRsa.Decrypt(uq["p"], priv);

                var user = db.FindOne(u => u.Email == um);
                if (user == null || user.PassHash != pwd)
                {
                    await Task.Delay(1000);
                    res.SendString("no", status: 401);
                    return;
                }

                

                var session = new Session
                {
                    Token = Guid.NewGuid().ToString("N"),
                    ExpireUTC = DateTime.UtcNow.AddHours(6)
                };
                _sessions.TryAdd(user.Id, session);
                var cookie = $"id={user.Id}token={session.Token}; expires={session.ExpireUTC:R};";
                res.SendString(cookie);
            });

            server.Get("/user/:user", (req, res) =>
            {
                if (!VerifyUser(req, res)) return;

            });

            server.Get("/team/:team", (req, res) =>
            {
                if (!VerifyUser(req, res)) return;

            });

            server.Get("/team/:team/:project", (req, res) =>
            {
                if (!VerifyUser(req, res)) return;

            });
            
            
            server.Get("/file/:file", (req, res) =>
            {
                var fname = req.Params["file"];
                res.AddHeader("Access-Control-Allow-Origin", "*");
                EditableFile file;
                if (!openEditableFiles.TryGetValue(fname, out file))
                {
                    file = EditableFile.FromFile(fname);
                    openEditableFiles.TryAdd(fname, file);
                }
                res.SendString(file.GetString());
            });

            server.WebSocket("/:file", (req, wsd) =>
            {
                var fname = req.Params["file"];
                List<WebSocketDialog> list;
                EditableFile file;
                if (!activeFiles.TryGetValue(fname, out list))
                {
                    list = new List<WebSocketDialog> {wsd};
                    activeFiles.TryAdd(fname, list);
                }
                else
                    list.Add(wsd);
                if (!openEditableFiles.TryGetValue(fname, out file))
                {
                    file = EditableFile.FromFile(fname);
                    openEditableFiles.TryAdd(fname, file);
                }
                Console.WriteLine(list.Count);


                wsd.OnTextReceived += (sender, eventArgs) =>
                {
                    file.ApplyTextChange(TextChange.FromJson(eventArgs.Text));
                    lock (list)
                    {
                        foreach (var webSocketDialog in list)
                            webSocketDialog.SendText(eventArgs.Text);
                    }
                };

                wsd.OnClosed += (sender, eventArgs) =>
                {
                    lock (list)
                    {
                        list.Remove(wsd);
                        if (list.Count == 0)
                        {
                            List<WebSocketDialog> tl;
                            activeFiles.TryRemove("test", out tl);
                            file.Save();
                            openEditableFiles.TryRemove(fname, out file);
                        }
                    }
                };
                wsd.Ready();
            });

            server.Start(true);
        }

        private static bool VerifyUser(RRequest req, RResponse res)
        {
            var id = req.Cookies["id"].Value;
            var token = req.Cookies["token"].Value;
            Session sess;
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(id) || !_sessions.TryGetValue(id, out sess) || sess.Token != token)
            {
                res.SendString("no", status: 401);
                return false;
            }
            return true;
        }

        private async void Persister()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        }
    }

    class Session
    {
        public string Token { get; set; }
        public string User { get; set; }
        public DateTime ExpireUTC { get; set; }
    }

    class Project
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string OwnerId { get; set; }
        public List<string> Files { get; set; }
    }



    class User
    {
        public string Email { get; set; }
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string PassHash { get; set; }
    }
}
