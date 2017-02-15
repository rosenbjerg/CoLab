using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LiteDB;

namespace CoLab
{
    public class Project
    {
        public Project()
        {
            
        }
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string Title { get; set; }
        public string Description { get; set; }
        public string OwnerId { get; set; }
        public ProjDir Files { get; set; } = new ProjDir {Name = "", Id = "0"};
        public List<string> Collaborators { get; set; } = new List<string>();
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        [BsonIgnore]
        public ConcurrentDictionary<string, EditableFile> OpenFiles { get; } =
            new ConcurrentDictionary<string, EditableFile>();

        public string LineEnding { get; set; } = "\n";

        public void AddFile(string templocation, string dirid, string filename)
        {
            ProjDir d;
            d = dirid == "0" ? Files : Files.GetDir(dirid);
            var f = new FileWrap(filename);
            FileManager.Import(templocation, Id, f.Id);
            d.Files.Add(f);
        }

        public void CreateEmptyFile(string pid, string file)
        {
            ProjDir d;
            d = pid == "0" ? Files : Files.GetDir(pid);
            var f = new FileWrap(file);
            if (FileManager.CreateNewFile(Id, f.Id))
                d.Files.Add(f);
        }

        public void CreateDir(string pid, string file)
        {
            ProjDir d;
            d = pid == "0" ? Files : Files.GetDir(pid);
            d.Dirs.Add(new ProjDir {Name = file});
        }

        public void Rename(string id, string newName)
        {
            var d = Files.GetDir(id);
            if (d != null) d.Name = newName;
            else
            {
                var f = Files.GetFile(id);
                f.Name = newName;
            }
        }

        public void Delete(string id)
        {
            ProjDir parent;
            var d = Files.GetDir(id, out parent);
            if (d != null)
            {
                DeleteDirectory(d);
                parent.Dirs.Remove(d);
            }
            else
            {
                var f = Files.GetFile(id, out parent);
                parent?.Files.Remove(f);
            }
        }

        private void DeleteDirectory(ProjDir dir)
        {
            foreach (var d in dir.Dirs)
            {
                DeleteDirectory(d);
            }
            foreach (var f in dir.Files)
            {
                FileManager.Remove(Id, f.Id);
            }
        }
    }

    public class ProjInf
    {
        public ProjInf()
        {
            
        }
        public ProjInf(string npId, string npTitle)
        {
            Id = npId;
            Name = npTitle;
        }

        public string Id { get; set; }
        public string Name { get; set; }
    }
}