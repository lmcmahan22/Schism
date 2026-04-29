using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Service.FileLogging
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _basePath;
        private readonly string _fileName;

        public FileLoggerProvider(string basePath)
        {
            _basePath = basePath;

            _fileName =
                $"schiism@{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.log";
        }

        public ILogger CreateLogger(string categoryName)
        {
            var fullPath = Path.Combine(_basePath, _fileName);
            Directory.CreateDirectory(_basePath); // Idempotent, meaning that if this already exists, nothing bad happens :D
            return new FileLogger(fullPath);
        }

        public void Dispose() { }
    }
}
