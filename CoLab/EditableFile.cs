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

        private readonly List<StringBuilder> _lines = new List<StringBuilder>();
        private bool _changes;


        public SynchronizedCollection<WebSocketDialog> ActiveEditors { get; } =
            new SynchronizedCollection<WebSocketDialog>();

        public string FileId { get; private set; }

        public static EditableFile FromFile(string file)
        {
            if (!File.Exists(file)) return null;
            var lines = File.ReadLines(file);
            var retVal = new EditableFile {FileId = file.Substring(file.LastIndexOf("/") + 1)};
            foreach (var line in lines)
                retVal._lines.Add(new StringBuilder(line));
            return retVal;
        }

        public void ApplyTextChange(TextChange tc)
        {
            if (tc.action == "insert")
            {
                while (tc.start.row > _lines.Count - 1)
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
                    var ml = firstLine.Length - tc.start.column;
                    var toMove = firstLine.ToString(tc.start.column, ml);
                    firstLine.Remove(tc.start.column, ml);
                    firstLine.Append(tc.lines[0]);

                    var i = 1;
                    for (; i < tc.lines.Length; i++)
                        _lines.Insert(tc.start.row + i, new StringBuilder(tc.lines[i]));
                    _lines[tc.start.row + i - 1].Append(toMove);
                }
            }
            else
            {
                if (tc.start.row > _lines.Count)
                    return;
                if (tc.end.row > _lines.Count)
                    tc.end.row = _lines.Count;
                var firstLine = _lines[tc.start.row];
                if (tc.start.row == tc.end.row)
                {
                    firstLine.Remove(tc.start.column, tc.end.column - tc.start.column);
                }
                else
                {
                    var frStart = tc.start.column > firstLine.Length
                        ? firstLine.Length
                        : firstLine.Length - tc.start.column;
                    var frEnd = firstLine.Length - frStart;
                    firstLine.Remove(frStart, frEnd);
                    var lastLine = _lines[tc.end.row];
                    if (tc.end.column < lastLine.Length)
                    {
                        lastLine.Remove(0, tc.end.column);
                        firstLine.Append(lastLine);
                    }
                    var s = tc.start.row + 1;
                    var c = tc.end.row - s; // tc.start.row;
                    _lines.RemoveRange(s, c);
                }
            }
            _changes = true;
        }

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
        }

        private void TrimEmptyLines()
        {
            for (var i = _lines.Count - 1; i > 0; i--)
            {
                if (_lines[i].Length != 0) break;
                _lines.RemoveAt(i);
            }
        }
    }
}