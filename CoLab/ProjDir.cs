using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CoLab
{
    public class ProjDir
    {
        public string Name { get; set; }
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public List<ProjDir> Dirs { get; set; } = new List<ProjDir>();
        public List<FileWrap> Files { get; set; } = new List<FileWrap>();

        public static ProjDir FromDirectory(DirectoryInfo d)
        {
            if (!d.Exists) return null;
            var ret = new ProjDir {Name = d.Name};
            foreach (var ed in d.EnumerateDirectories())
                ret.Dirs.Add(FromDirectory(ed));
            foreach (var ef in d.EnumerateFiles())
                ret.Files.Add(new FileWrap(ef.Name));
            return ret;
        }

        public FileWrap GetFile(string fid)
        {
            FileWrap f;
            f = Files.FirstOrDefault(x => x.Id == fid);
            if (f == null)
                foreach (var projDir in Dirs)
                {
                    f = projDir.GetFile(fid);
                    if (f != null) break;
                }
            return f;
        }

        public ProjDir GetDir(string did)
        {
            ProjDir d;
            d = Dirs.FirstOrDefault(x => x.Id == did);
            if (d == null)
                foreach (var projDir in Dirs)
                {
                    d = projDir.GetDir(did);
                    if (d != null) break;
                }
            return d;
        }

        public FileWrap GetFile(string fid, out ProjDir parent)
        {
            FileWrap f;
            var p = this;
            f = Files.FirstOrDefault(x => x.Id == fid);
            if (f == null)
                foreach (var projDir in Dirs)
                {
                    f = projDir.GetFile(fid, out p);
                    if (f != null) break;
                }
            parent = p;
            return f;
        }

        public ProjDir GetDir(string did, out ProjDir parent)
        {
            ProjDir d, p;
            p = this;
            d = Dirs.FirstOrDefault(x => x.Id == did);
            if (d == null)
                foreach (var projDir in Dirs)
                {
                    d = projDir.GetDir(did, out p);
                    if (d != null) break;
                }
            parent = p;
            return d;
        }
    }
}