using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CoLab
{
    class ProjDir
    {
        public string Name { get; set; }
        public List<ProjDir> Dirs { get; set; } = new List<ProjDir>();
        public List<FileWrap> Files { get; set; } = new List<FileWrap>();

        public static ProjDir FromDirectory(DirectoryInfo d)
        {
            if (!d.Exists) return null;
            var ret = new ProjDir();
            foreach (var ed in d.EnumerateDirectories())
            {
                ret.Dirs.Add(FromDirectory(ed));
            }
            foreach (var ef in d.EnumerateFiles())
            {
                ret.Files.Add(new FileWrap(ef.Name));
            }
            return ret;
        }

        public FileWrap Get(string fid)
        {
            FileWrap f;
            f = Files.FirstOrDefault(x => x.Id == fid);
            if (f == null)
            {
                foreach (var projDir in Dirs)
                {
                    f = projDir.Get(fid);
                    if (f != null) break;
                }
            }
            return f;
        }
    }
}