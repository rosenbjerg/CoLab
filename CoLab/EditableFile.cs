using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CoLab
{
    class EditableFile
    {
        private EditableFile()
        {

        }
        
        public string FilePath { get; private set; }

        public static EditableFile FromFile(string file)
        {
            if (!File.Exists(file)) return null;
            var lines = File.ReadLines(file);
            var retVal = new EditableFile {FilePath = file};
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

                    for (var i = tc.start.row + 1; i < tc.end.row; i++)
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

        public void Save(string lineEnding = "\n")
        {
            if (!_changes) return;
            File.WriteAllText(FilePath, GetString(lineEnding));
            _changes = false;
            Console.WriteLine(FilePath + " saved");
        }
    }
}