using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Elin.Plugin.Main.PluginHelpers
{
    internal class FileLogger : IDisposable
    {
        #region variable

        private StreamWriter? _writer = null;

        #endregion

        public FileLogger(string path)
        {
            Path = path;
        }

        #region property

        private string Path { get; }

        private StreamWriter Writer
        {
            get
            {
                ThrowIfDisposed();

                return this._writer ??= CreateWriter();
            }
        }

        #endregion

        #region function

        private StreamWriter CreateWriter()
        {
            var stream = new FileStream(Path, FileMode.Append, FileAccess.Write, FileShare.Read);
            var writer = new StreamWriter(stream);
            return writer;
        }

        public void LogCore(string message)
        {
            Writer.WriteLine(message);
        }

        public void Log(string message)
        {
            LogCore(message);
            Writer.Flush();
        }

        public void Log(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                LogCore(line);
            }
            Writer.Flush();
        }


        #endregion

        #region IDisposable

        private bool _disposedValue;

        protected void ThrowIfDisposed([CallerMemberName] string callerMemberName = "")
        {
            if (this._disposedValue)
            {
                throw new ObjectDisposedException(callerMemberName);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (this._writer is not null)
                {
                    this._writer.Dispose();
                    this._writer = null;
                }

                this._disposedValue = true;
            }
        }

        ~FileLogger()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
