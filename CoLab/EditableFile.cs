using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RHttpServer;

namespace CoLab
{
    public class EditableFile
    {
        private EditableFile()
        {

        }
        

        public SynchronizedCollection<WebSocketDialog> ActiveEditors { get; } = new SynchronizedCollection<WebSocketDialog>();

        public string FileId { get; private set; }

        public static EditableFile FromFile(string file)
        {
            if (!File.Exists(file)) return null;
            var lines = File.ReadLines(file);
            var retVal = new EditableFile {FileId = file.Substring(file.LastIndexOf("/") +1)};
            foreach (var line in lines)
            {
                retVal._lines.Add(new StringBuilder(line));
            }
            return retVal;
        }

        public void ApplyTextChange(TextChange tc)
        {
            if (tc.action == "insert")
            {
                while (_lines.Count -1 < tc.end.row)
                {
                    _lines.Add(new StringBuilder());
                }
                var firstLine = _lines[tc.start.row];
                if (tc.start.row == tc.end.row)
                {
                    firstLine.Insert(tc.start.column, tc.lines[0]);
                }
                else
                {
                    var toMove = firstLine.ToString(tc.start.column, firstLine.Length - tc.start.column);
                    firstLine.Remove(tc.start.column, firstLine.Length - tc.start.column);
                    firstLine.Append(tc.lines[0]);

                    int i = 1;
                    for (; i < tc.lines.Length; i++)
                    {
                        _lines.Insert(tc.start.row + i, new StringBuilder(tc.lines[i]));
                    }
                    _lines[tc.start.row + i - 1].Append(toMove);
                }
            }
            else
            {
                var firstLine = _lines[tc.start.row];
                if (tc.start.row == tc.end.row)
                {
                    firstLine.Remove(tc.start.column, tc.end.column-tc.start.column);
                }
                else
                {
                    firstLine.Remove(tc.start.column, firstLine.Length - tc.start.column);
                    var lastLine = _lines[tc.end.row];
                    lastLine.Remove(0, tc.end.column);
                    firstLine.Append(lastLine);
                    _lines.RemoveAt(tc.end.row);

                    for (var i = tc.start.row + 1; i < tc.end.row-1; i++)
                        _lines.RemoveAt(tc.start.row + 1);
                }
                
            }
            _changes = true;
        }

        private readonly List<StringBuilder> _lines = new List<StringBuilder>();
        private bool _changes;

        public string GetString(string lineEnding = "\n")
        {
            return string.Join(lineEnding, _lines);
        }

        public void Save()
        {
            if (!_changes) return;
            TrimEmptyLines();
            File.WriteAllText(FileId, GetString());
            _changes = false;
            Console.WriteLine(FileId + " saved");
        }

        private void TrimEmptyLines()
        {
            for (int i = _lines.Count-1; i > 0; i--)
            {
                if (_lines[i].Length != 0) break;
                _lines.RemoveAt(i);
            }
        }
    }
}