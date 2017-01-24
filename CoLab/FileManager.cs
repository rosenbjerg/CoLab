using System.IO;

namespace CoLab
{
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
            var filepath = Path.Combine(projDir, project, file);
            if (File.Exists(filepath))
            {
                return false;
            }
            Directory.CreateDirectory(filepath);
            File.WriteAllText(filepath, "");
            return true;
        }

        public static EditableFile GetEditable(string pid, string fid)
        {
            var filepath = Path.Combine(projDir, pid, fid);
            if (!File.Exists(filepath))
            {
                return null;
            }
            var ret = EditableFile.FromFile(filepath);
            return ;
        }
    }
}