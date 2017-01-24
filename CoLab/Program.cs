using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
            var ldb = new LiteDatabase("db.lite");
            var udb = ldb.GetCollection<User>("Users");
            var pdb = ldb.GetCollection<Project>("Projects");
            var rsa = CryptoRsa.GenerateKeyPair();
            var pub = rsa.Item1;
            var priv = rsa.Item2;
            var activeFiles = new ConcurrentDictionary<string, List<WebSocketDialog>>();
            var openEditableFiles = new ConcurrentDictionary<string, EditableFile>();
            var sb = new StringBuilder(System.IO.File.ReadAllText("./test.txt"));
            var activeProjects = new ConcurrentDictionary<string, ConcurrentDictionary<string, EditableFile>>();

            
            server.Get("/project/:project", (req, res) =>
            {
                string uid;
                if (!VerifyUser(req, res, out uid)) return;
                var pid = req.Params["project"];
                var p = pdb.FindById(pid);
                if (p == null)
                {
                    res.SendString("", status: 404);
                    return;
                }
                if (uid != p.OwnerId && !p.Collaborators.Contains(uid))
                {
                    res.SendString("", status: 401);
                    return;
                }
                res.SendJson(p.Files);
            });

            server.Post("/project/:project/download", (req, res) =>
            {
                string uid;
                if (!VerifyUser(req, res, out uid)) return;
                var pid = req.Params["project"];
                var p = pdb.FindById(pid);
                if (p == null)
                {
                    res.SendString("Project not found", status: 404);
                    return;
                }
                if (uid != p.OwnerId && !p.Collaborators.Contains(uid))
                {
                    res.SendString("You do not have access to the project", status: 401);
                    return;
                }
                ConcurrentDictionary<string, EditableFile> openFiles;
                if (!activeProjects.TryGetValue(pid, out openFiles))
                {
                    openFiles = new ConcurrentDictionary<string, EditableFile>();
                    activeProjects.TryAdd(pid, openFiles);
                }
                var file = req.ParseBody<string>();
                EditableFile ef;
                if (!openFiles.TryGetValue(file, out ef))
                {
                    if (!FileManager.TryGetFile(pid, file, out ef))
                    {
                        res.SendString("File could not be found", status:404);
                        return;
                    }
                    openFiles.TryAdd(file, ef);
                }
                res.SendString(ef.GetString());
            });
            server.Post("/project/:project/create", (req, res) =>
            {
                string uid;
                if (!VerifyUser(req, res, out uid)) return;
                var pid = req.Params["project"];
                var p = pdb.FindById(pid);
                if (p == null)
                {
                    res.SendString("Project not found", status: 404);
                    return;
                }
                if (uid != p.OwnerId && !p.Collaborators.Contains(uid))
                {
                    res.SendString("You do not have access to the project", status: 401);
                    return;
                }
                var file = req.ParseBody<string>();
                if (!FileManager.CreateNewFile(pid, file))
                    res.SendString("File with given name already exists in directory", status: 409);
                else
                    res.SendString("OK");
            });
            server.Post("/project/:project/upload", async (req, res) =>
            {
                string uid;
                if (!VerifyUser(req, res, out uid)) return;
                var pid = req.Params["project"];
                var p = pdb.FindById(pid);
                if (p == null)
                {
                    res.SendString("Project not found", status: 404);
                    return;
                }
                if (uid != p.OwnerId && !p.Collaborators.Contains(uid))
                {
                    res.SendString("You do not have access to the project", status: 401);
                    return;
                }
                var save = await req.SaveBodyToFile(".projects/"+pid);
                if (save)
                    res.SendString("OK");
                else
                    res.SendString("Error", status: 500);
            });

            server.Get("/login", (req, res) =>
            {
                
            });

            server.Post("/login", async (req, res) =>
            {
                var uq = req.GetBodyPostFormData();
                var um = CryptoRsa.Decrypt(uq["u"], priv);
                var pwd = CryptoRsa.Decrypt(uq["p"], priv);

                var user = udb.FindOne(u => u.Email == um);
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
                string uid;
                if (!VerifyUser(req, res, out uid)) return;

            });

            server.Get("/user/:user/projects", (req, res) =>
            {
                string uid;
                if (!VerifyUser(req, res, out uid)) return;

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

        private static bool VerifyUser(RRequest req, RResponse res, out string uid)
        {
            var id = req.Cookies["id"].Value;
            var token = req.Cookies["token"].Value;
            Session sess;
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(id) || !_sessions.TryGetValue(id, out sess) || sess.Token != token)
            {
                res.SendString("no", status: 401);
                uid = "";
                return false;
            }
            uid = sess.User;
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

    static class FileManager
    {
        private const string projDir = "./projects";

        static FileManager()
        {
            Directory.CreateDirectory(projDir);
        }

        public static bool TryGetFile(string project, string file, out EditableFile ef)
        {
            var filepath = Path.Combine(projDir, projDir, file);
            if (!File.Exists(filepath))
            {
                ef = null;
                return false;
            }
            ef = EditableFile.FromFile(filepath);
            return true;
        }

        public static bool Exists(string project, string file)
        {
            var filepath = Path.Combine(projDir, projDir, file);
            return File.Exists(filepath);
        }

        public static bool CreateNewFile(string project, string file)
        {
            var filepath = Path.Combine(projDir, projDir, file);
            if (File.Exists(filepath))
            {
                return false;
            }
            Directory.CreateDirectory(filepath);
            File.WriteAllText(filepath, "");
            return true;
        }
    }

    class Session
    {
        public string Token { get; set; }
        public string User { get; set; }
        public DateTime ExpireUTC { get; set; }
    }

    class ProjDir
    {
        public string Name { get; set; }
        public List<ProjDir> Dirs { get; set; } = new List<ProjDir>();
        public List<string> Files { get; set; } = new List<string>();
    }

    class Project
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; }
        public string Description { get; set; }
        public string OwnerId { get; set; }
        public ProjDir Files { get; set; } = new ProjDir {Name = ""};
        public List<string> Collaborators { get; } = new List<string>();
        
        [BsonIgnore]
        public List<EditableFile> OpenFiles { get; } = new List<EditableFile>();
    }
    
    class User
    {
        public string Email { get; set; }
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string DisplayName { get; set; }
        public string PassHash { get; set; }

        public List<string> Projects { get; set; } = new List<string>();
        public List<string> CollaboratorOn { get; set; } = new List<string>();
    }
}
