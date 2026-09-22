using System;
using System.IO;
using System.Text;

namespace XboxKit.GUI
{
    public class ConsoleRedirectWriter : TextWriter
    {
        private readonly Action<string> _onLine;
        private readonly StringBuilder _currentLine = new();
        private readonly object _lock = new();

        public ConsoleRedirectWriter(Action<string> onLine)
        {
            _onLine = onLine;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            lock (_lock)
            {
                if (value == '\n')
                {
                    string line = _currentLine.ToString();
                    _currentLine.Clear();
                    _onLine(line);
                }
                else if (value != '\r')
                {
                    _currentLine.Append(value);
                }
            }
        }

        public override void Write(string? value)
        {
            if (value == null) return;
            lock (_lock)
            {
                foreach (char c in value)
                {
                    if (c == '\n')
                    {
                        string line = _currentLine.ToString();
                        _currentLine.Clear();
                        _onLine(line);
                    }
                    else if (c != '\r')
                    {
                        _currentLine.Append(c);
                    }
                }
            }
        }

        public override void Flush()
        {
            lock (_lock)
            {
                if (_currentLine.Length > 0)
                {
                    string line = _currentLine.ToString();
                    _currentLine.Clear();
                    _onLine(line);
                }
            }
        }
    }
}
