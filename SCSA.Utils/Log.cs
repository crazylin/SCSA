using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SCSA.Utils
{
    /// <summary>
    /// 简单的日志工具类，根据开关决定是否将日志写入文件。
    /// </summary>
    public static class Log
    {
        private static readonly object _initLock = new();
        private static bool _enabled = true;
        private static string _logFilePath;

        private static Channel<string> _channel;
        private static CancellationTokenSource? _cts;
        private static Task? _workerTask;

        /// <summary>
        /// 初始化日志系统。
        /// </summary>
        /// <param name="enable">是否启用日志记录</param>
        public static void Initialize(bool enable)
        {
            lock (_initLock)
            {
                if (_workerTask != null) return; // 已初始化

                _enabled = enable;
                if (!_enabled) return;

                var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory);
                _logFilePath = Path.Combine(logDirectory, $"{DateTime.Now:yyyyMMdd}.log");

                _cts = new CancellationTokenSource();
                _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                });

                _workerTask = Task.Run(() => ProcessChannelAsync(_cts.Token));
            }
        }

        public static void Info(string message) => Write("INFO", message);
        public static void Warning(string message) => Write("WARNING", message);
        public static void Debug(string message) => Write("DEBUG", message);
        public static void Error(string message, Exception? ex = null)
        {
            var msg = ex == null ? message : $"{message} | Exception: {ex}";
            Write("ERROR", msg);
        }

        private static void Write(string level, string message)
        {
            if (!_enabled || string.IsNullOrEmpty(_logFilePath))
                return;

            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
            _channel.Writer.TryWrite(line);
        }

        private static async Task ProcessChannelAsync(CancellationToken token)
        {
            try
            {
                await foreach (var line in _channel.Reader.ReadAllAsync(token))
                {
                    await File.AppendAllTextAsync(_logFilePath, line + Environment.NewLine, token);
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"日志后台写入失败: {ex.Message}");
            }
        }

        public static void Shutdown()
        {
            if (_cts == null) return;
            try
            {
                _channel.Writer.TryComplete();
                _cts.Cancel();
                _workerTask?.Wait(2000);
            }
            catch
            {
                // ignored
            }
        }
    }
} 