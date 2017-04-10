using System.IO;

namespace CoLab
{
    internal static class FileManager
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
                return false;
            Directory.CreateDirectory(Path.Combine(projDir, project));
            File.WriteAllText(filepath + ".txt", "");
            File.Move(filepath + ".txt", filepath);
            return true;
        }

        public static EditableFile GetEditable(string pid, string fid)
        {
            var filepath = Path.Combine(projDir, pid, fid);
            if (!File.Exists(filepath))
                return null;
            var ret = EditableFile.FromFile(filepath);
            return ret;
        }

        public static bool Import(string from, string pid, string fid)
        {
            if (!File.Exists(from)) return false;
            Directory.CreateDirectory(Path.Combine(projDir, pid));
            var filepath = Path.Combine(projDir, pid, fid);
            File.Copy(from, filepath);
            return true;
        }

        public static void Remove(string pid, string fid)
        {
            var filepath = Path.Combine(projDir, pid, fid);
            if (!File.Exists(filepath))
                return;
            File.Delete(filepath);
        }
    }
}