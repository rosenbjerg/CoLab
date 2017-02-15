using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using LiteDB;
using Org.BouncyCastle.Crypto;
using RHttpServer;
using ServiceStack;

namespace CoLab
{
    internal class Program
    {
        private static readonly Random Random = new Random();
        private static readonly ConcurrentDictionary<string, Project> ActiveProjects = new ConcurrentDictionary<string, Project>();
        private static readonly ConcurrentDictionary<string, Session> Sessions = new ConcurrentDictionary<string, Session>();
        private static readonly LiteCollection<Project> Pdb = new LiteDatabase("pdb.lite").GetCollection<Project>("Projects");
        private static readonly LiteCollection<User> Udb = new LiteDatabase("udb.lite").GetCollection<User>("Users");

        private static void Mailer()
        {
            string to = "malterandersen@live.dk"; //To address    
            string from = "colab@rosenbjerg.dk"; //From address    
            MailMessage message = new MailMessage(from, to);

            string mailbody = "In this article you will learn how to send a email using Asp.Net & C#";
            message.Subject = "Sending Email Using Asp.Net & C#";
            message.Body = mailbody;
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;
            SmtpClient client = new SmtpClient("smtp.zoho.com", 465); //Gmail smtp    
            System.Net.NetworkCredential cred = new System.Net.NetworkCredential("colab@rosenbjerg.dk", "colabflødebolle");
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = cred;
            try
            {
                client.Send(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex); 
            }


        }

        private static void Main(string[] args)
        {
            //const string pagePath = "pages/";
            const string pagePath = "../../../CoLabClient/";
            var server = new TaskBasedHttpServer(5005, throwExceptions:true);
            //var tp = new Project
            //{
            //    Title = "Test project",
            //    OwnerId = "Malt",
            //    Id = "testtest"
            //};
            //tp.AddFile("../../EditableFile.cs", "0", "EditableFile.cs");
            //Pdb.Insert(tp);


            server.Get("/project/:project", (req, res) =>
            {
                string uid;
                if (!VerifyUser(req, res, true, out uid)) return;

                var pid = req.Params["project"];
                Project p;
                if (!TryGetProject(pid, uid, out p))
                {
                    res.SendString("Unable to find project. It either does not exist, or is inaccessible to you", status: 404);
                    return;
                }
                var rp = new RenderParams
                {
                    {"pid", pid}
                };
                res.RenderPage(pagePath + "editor/editor.ecs", rp);
            });

            server.Get("/project/:project/meta", (req, res) =>
            {
                string uid;
                if (!VerifyUser(req, res, false, out uid)) return;

                var pid = req.Params["project"];
                Project p;
                if (!TryGetProject(pid, uid, out p))
                {
                    res.SendString("Unable to find project. It either does not exist, or is inaccessible to you", status: 404);
                    return;
                }
                res.SendJson(new
                {
                    p.Title,
                    Desc = p.Description,
                    p.Files
                });
            });

            server.Get("/project/:project/:fileid", (req, res) =>
            {
                string uid;
                if (!VerifyUser(req, res, false, out uid)) return;

                var pid = req.Params["project"];
                var fid = req.Params["fileid"];
                Project p;
                if (!TryGetProject(pid, uid, out p))
                {
                    res.SendString("Unable to find project. It either does not exist, or is inaccessible to you", status: 404);
                    return;
                }
              
                var f = p.Files.GetFile(fid);
                if (f == null)
                {
                    res.SendString("", status: 404);
                    return;
                }

                EditableFile ef;
                if (!p.OpenFiles.TryGetValue(fid, out ef))
                {
                    ef = FileManager.GetEditable(pid, fid);
                }
                res.SendString(ef.GetString());
            });

            server.Post("/project/:project/rename", (req, res) =>
            {
                string uid;
                if (!VerifyUser(req, res, false, out uid)) return;

                var pid = req.Params["project"];
                Project p;
                if (!TryGetProject(pid, uid, out p))
                {
                    res.SendString("Unable to find project. It either does not exist, or is inaccessible to you", status: 404);
                    return;
                }
                var file = req.ParseBody<string>();
                var split = file.Split('/');
                if (split.Length != 2)
                {
                    res.SendString("Malformed request", status:400);
                    return;
                }
                var id = split[0];
                var newname = split[1];
                p.Rename(id, newname);
                Pdb.Update(p);
                res.SendString("OK");
            });

            server.Post("/project/:project/delete", (req, res) =>
            {
                string uid;
                if (!VerifyUser(req, res, false, out uid)) return;

                var pid = req.Params["project"];
                Project p;
                if (!TryGetProject(pid, uid, out p))
                {
                    res.SendString("Unable to find project. It either does not exist, or is inaccessible to you", status: 404);
                    return;
                }
                var id = req.ParseBody<string>();
                p.Delete(id);
                Pdb.Update(p);
                res.SendString("OK");
            });

            server.Post("/project/:project/create/:type", (req, res) =>
            {
                string uid;
                if (!VerifyUser(req, res, false, out uid)) return;

                var pid = req.Params["project"];
                Project p;
                if (!TryGetProject(pid, uid, out p))
                {
                    res.SendString("Unable to find project. It either does not exist, or is inaccessible to you", status: 404);
                    return;
                }

                var file = req.ParseBody<string>();
                var split = file.Split('/');
                if (split.Length != 2)
                {
                    res.SendString("Malformed request", status: 400);
                    return;
                }
                var id = split[0];
                var newname = split[1];

                var type = req.Params["type"];
                switch (type)
                {
                    case "dir":
                        p.CreateDir(id, newname);
                        break;
                    case "file":
                        p.CreateEmptyFile(id, newname);
                        break;
                    default:
                        res.SendString("Malformed request", status:400);
                        return;
                }

                Pdb.Update(p);
                
                res.SendString("OK");
            });

            server.Post("/project/:project/upload/:pid", async (req, res) => 
            {
                res.AddHeader("Access-Control-Allow-Origin", "http://localhost:63342");
                //string uid;
                var uid = "Malt";
                //if (!VerifyUser(req, res, false, out uid)) return;

                var pid = req.Params["project"];
                var parent = req.Params["pid"];
                Project p;
                if (!TryGetProject(pid, uid, out p))
                {
                    res.SendString("Unable to find project. It either does not exist, or is inaccessible to you", status: 404);
                    return;
                }

                var oldname = "";
                var newname = RandomString(5);

                var save = await req.SaveBodyToFile("temp", s =>
                {
                    oldname = s;
                    newname += s;
                    return newname;
                }, 0x1400);
                if (!save)
                    res.SendString("Error", status: 500);
                else
                {
                    p.AddFile("temp/" + newname, parent, oldname);
                    Pdb.Update(p);
                    res.SendString("OK");
                }
            });

            server.Get("/createproject", (req, res) =>
            {
                string uid;
                if (!VerifyUser(req, res, true, out uid)) return;
                res.RenderPage(pagePath + "newproject/newproject.ecs", null);
            });
            server.Post("/createproject", (req, res) =>
            {
                string uid;
                if (!VerifyUser(req, res, false, out uid)) return;
                var proj = req.ParseBody<NewProject>();
                if (proj == null || !proj.Validate())
                {
                    res.SendString("Malformed request", status: 400);
                    return;
                }
                var user = Udb.FindById(uid);
                if (user == null)
                {
                    res.SendString("no");
                    return;
                }
                var colabs = (from c in proj.co select Udb.FindOne(u => u.Email == c) into d where d != null select d.Id).ToList();
                var np = new Project
                {
                    Title = proj.pn,
                    Description = proj.pd,
                    OwnerId = uid,
                    Collaborators = colabs
                };
                user.Projects.Add(new ProjInf(np.Id, np.Title));
                Udb.Update(user);
                Pdb.Insert(np);
                res.SendString(np.Id);
            });

            server.Get("/login", (req, res) =>
            {
                var rp = req.Queries["rp"];
                res.RenderPage(pagePath + "login/login.ecs", new RenderParams
                {
                    {"rp", rp}
                });
            });
            server.Post("/login", async (req, res) =>
            {
                res.AddHeader("Access-Control-Allow-Origin", "http://localhost:63342");
                var login = req.ParseBody<Login>();
                if (login == null ||!login.Validate())
                {
                    res.SendString("Malformed request", status:400);
                    return;
                }
                var user = Udb.FindOne(u => u.Email == login.u);
                if (user == null || !BCrypt.Net.BCrypt.Verify(login.p, user.PassHash))
                {
                    await Task.Delay(500);
                    res.SendString("no");
                    return;
                }
                
                var session = new Session
                {
                    Token = Guid.NewGuid().ToString("N"),
                    ExpireUTC = DateTime.UtcNow.AddHours(6)
                };
                Sessions[user.Id] = session;
                var cookie = $"[\"id={user.Id}; expires={session.ExpireUTC:R}; path=/\",\"token={session.Token}; expires={session.ExpireUTC:R}; path=/\"]";
                //var cookie = $"[\"id={user.Id}; expires={session.ExpireUTC:R}; path=/; secure\",\"token={session.Token}; expires={session.ExpireUTC:R}; path=/; secure\"]";
                res.SendString(cookie);
            });

            server.Get("/register", (req, res) =>
            {
                res.RenderPage(pagePath + "register/register.ecs", null);
            });
            server.Post("/register", async (req, res) =>
            {
                res.AddHeader("Access-Control-Allow-Origin", "http://localhost:63342");
                var reg = req.ParseBody<Registration>();
                if (reg == null || !reg.Validate())
                {
                    res.SendString("Malformed request", status: 400);
                    return;
                }
                var user = Udb.FindOne(u => u.Email == reg.em);
                if (user != null)
                {
                    await Task.Delay(500);
                    res.SendString("Email already in use");
                    return;
                }
                user = new User
                {
                    FirstName = reg.fn,
                    LastName = reg.ln,
                    Email = reg.em,
                    PassHash = BCrypt.Net.BCrypt.HashPassword(reg.pw),
                    DisplayName = reg.fn,
                    DeveloperType = reg.dt,
                    CountryCode = reg.co
                };
                Udb.Insert(user);
                res.SendString("OK");
            });

            server.Get("/projects", (req, res) =>
            {
                string uid;
                if (!VerifyUser(req, res, true, out uid)) return;
                var u = Udb.FindById(uid);
                var rp = new RenderParams
                {
                    {"projects", new
                        {
                            own = u.Projects,
                            collab = u.CollaboratorOn
                        }
                    }
                };
                res.RenderPage(pagePath + "projects/projects.ecs", rp);

            });

            server.Get("/user", (req, res) =>
            {
                string uid;
                if (!VerifyUser(req, res, true, out uid)) return;
                var u = Udb.FindById(uid);
                var rp = new RenderParams
                {
                    {"", ""}
                };
                res.RenderPage("pages/user.ecs", rp);

            });
            
            server.Options("/*", (req, res) =>
            {
                res.AddHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
                res.AddHeader("Access-Control-Allow-Headers", "Content-Type");
                res.AddHeader("Access-Control-Allow-Origin", "http://localhost:63342");
                res.SendString("");
            });
            
            server.WebSocket("/:project/:file", (req, wsd) =>
            {
                string uid;
                if (!VerifyUser(req, wsd, out uid)) return;

                var pid = req.Params["project"];
                var fid = req.Params["file"];
                Project p;

                if (!TryGetProject(pid, uid, out p))
                {
                    wsd.Close();
                    return;
                }
                
                EditableFile file;
                if (!p.OpenFiles.TryGetValue(fid, out file))
                {
                    file = FileManager.GetEditable(pid, fid);
                    if (file != null)
                        p.OpenFiles.TryAdd(fid, file);
                    else
                    {
                        wsd.Close();
                        return;
                    }
                }
                
                file.ActiveEditors.Add(wsd);

                wsd.OnTextReceived += (sender, eventArgs) =>
                {
                    var msgt = eventArgs.Text.Substring(0, 2);
                    var msgc = eventArgs.Text.Substring(2);

                    if (msgt == "tc")
                        file.ApplyTextChange(TextChange.FromJson(msgc));
                    foreach (var w in file.ActiveEditors)
                        w.SendText(eventArgs.Text);
                };

                wsd.OnClosed += (sender, eventArgs) =>
                {
                    file.ActiveEditors.Remove(wsd);
                    if (file.ActiveEditors.Count != 0) return;
                    file.Save();
                    p.OpenFiles.TryRemove(fid, out file);
                };
                wsd.Ready();
            });

            server.InitializeDefaultPlugins(false);

            SessionsManager();
            Persister();

            server.Start(true);
        }

        private static bool TryGetProject(string pid, string uid, out Project p)
        {
            if (ActiveProjects.TryGetValue(pid, out p)) return true;
            p = Pdb.FindById(pid);
            if (p != null && (p.OwnerId == uid || p.Collaborators.Contains(uid)))
                ActiveProjects.TryAdd(pid, p);
            else
                return false;
            return true;
        }

        private static bool VerifyUser(RRequest req, RResponse res, bool redirect, out string uid)
        {
            var id = req.Cookies["id"]?.Value ?? "";
            var token = req.Cookies["token"]?.Value ?? "";
            Session sess;
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(id) || !Sessions.TryGetValue(id, out sess) || sess.Token != token)
            {
                if (redirect)
                {
                    res.Redirect("/login?rp=" + req.UnderlyingRequest.Url.AbsolutePath);
                    uid = "";
                    return false;
                }
                res.SendString("no", status: 401);
                uid = "";
                return false;
            }
            uid = id;
            return true;
        }
        
        private static bool VerifyUser(RRequest req, WebSocketDialog wsd, out string uid)
        {
            var id = req.Cookies["id"].Value;
            var token = req.Cookies["token"].Value;
            Session sess;
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(id) || !Sessions.TryGetValue(id, out sess) || sess.Token != token)
            {
                wsd.Close();
                uid = "";
                return false;
            }
            uid = id;
            return true;
        }
        
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        private static async void Persister()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(4));
                foreach (var activeProject in ActiveProjects.Select(p => p.Value))
                {
                    foreach (var activeProjectOpenFile in activeProject.OpenFiles.Select(p => p.Value))
                    {
                        activeProjectOpenFile.Save();
                    }
                }
            }

        }

        private static async void SessionsManager()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(15));
                var now = DateTime.UtcNow;
                var re = Sessions.Where(x => x.Value.ExpireUTC <= now);
                Session o;
                foreach (var keyValuePair in re)
                    Sessions.TryRemove(keyValuePair.Key, out o);
            }
        }
    }
}
