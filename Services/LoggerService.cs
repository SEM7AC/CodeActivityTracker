using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CodeActivityTracker.Services;

public class LoggerService
    {
    private readonly string _logPath;

    public LoggerService()
        {
        
        var folder = @"C:\Projects\Logs";
        Directory.CreateDirectory(folder);

        _logPath = Path.Combine(folder, "cats.log");

        }

    public void Log(string message)
        {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";
        File.AppendAllText(_logPath, line + Environment.NewLine);
        }
    }