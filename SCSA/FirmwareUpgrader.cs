using SCSA.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SCSA
{
    public class FirmwareUpgrader : IDisposable
    {
        private const int MaxRetry = 3;
        private const int ChunkSize = 1280;
        private readonly DeviceControlApiAsync _deviceApi;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public event Action<double> ProgressChanged;
        public event Action<UpgradeStatus> StatusChanged;
        public event Action<Exception> ErrorOccurred;

        public FirmwareUpgrader(DeviceControlApiAsync deviceApi)
        {
            _deviceApi = deviceApi ?? throw new ArgumentNullException(nameof(deviceApi));
        }

        public async Task<bool> StartUpgradeAsync(
            byte[] firmwareData,
            CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                // 阶段1：启动传输
                var startSuccess = await StartTransferPhase(firmwareData.Length, cancellationToken);
                if (!startSuccess) return false;

                // 阶段2：分片传输
                return await TransferDataPhase(firmwareData, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                StatusChanged?.Invoke(UpgradeStatus.Failed);
                ErrorOccurred?.Invoke(ex);
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<bool> StartTransferPhase(
            int totalSize,
            CancellationToken cancellationToken)
        {
            for (var retry = 0; retry < MaxRetry; retry++)
            {
                try
                {
                    StatusChanged?.Invoke(UpgradeStatus.Starting);
                    var success = await _deviceApi.FirmwareUpgradeStart(
                        totalSize,
                        cancellationToken);

                    return success;
                }
                catch (OperationCanceledException)
                {
                    StatusChanged?.Invoke(UpgradeStatus.Canceled);
                    return false;
                }
                catch (Exception ex)
                {
                    if (retry == MaxRetry - 1)
                    {
                        ErrorOccurred?.Invoke(new FirmwareTransferException(
                            $"启动阶段失败，重试次数已耗尽",
                            ex));
                        return false;
                    }
                    await Task.Delay(1000, cancellationToken);
                }
            }
            return false;
        }

        private async Task<bool> TransferDataPhase(
            byte[] firmwareData,
            CancellationToken cancellationToken)
        {
            var totalPackets = (int)Math.Ceiling(firmwareData.Length / (double)ChunkSize);
            var successPackets = 0;

            StatusChanged?.Invoke(UpgradeStatus.Transferring);

            for (var packetId = 1; packetId <= totalPackets; packetId++)
            {
                var chunk = GetDataChunk(firmwareData, packetId);

                var transferSuccess = await TransferSinglePacket(
                    packetId,
                    chunk,
                    totalPackets,
                    cancellationToken);

                if (!transferSuccess) return false;

                successPackets++;
                UpdateProgress(successPackets, totalPackets);
            }

            StatusChanged?.Invoke(UpgradeStatus.Completed);
            return true;
        }

        private byte[] GetDataChunk(byte[] fullData, int packetId)
        {
            var startIndex = (packetId - 1) * ChunkSize;
            var length = Math.Min(ChunkSize, fullData.Length - startIndex);
            var chunk = new byte[length];
            Array.Copy(fullData, startIndex, chunk, 0, length);
            return chunk;
        }

        private async Task<bool> TransferSinglePacket(
            int packetId,
            byte[] chunkData,
            int totalPackets,
            CancellationToken cancellationToken)
        {
            for (var retry = 0; retry < MaxRetry; retry++)
            {
                try
                {
                    var success = await _deviceApi.FirmwareUpgradeTransfer(
                        packetId,
                        chunkData,
                        cancellationToken);

                    if (success) return true;

                    if (retry == MaxRetry - 1)
                    {
                        ErrorOccurred?.Invoke(new FirmwareTransferException(
                            $"包{packetId}/{totalPackets}传输失败，已达最大重试次数"));
                        StatusChanged?.Invoke(UpgradeStatus.Failed); // 新增状态通知
                    }
                }
                catch (OperationCanceledException)
                {
                    StatusChanged?.Invoke(UpgradeStatus.Canceled);
                    return false;
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(new FirmwareTransferException(
                        $"包{packetId}/{totalPackets}传输异常",
                        ex));
                }

                await Task.Delay(500 * (retry + 1), cancellationToken);
            }
            return false;
        }

        private void UpdateProgress(int current, int total)
        {
            var progress = Math.Round((current / (double)total) * 100, 1);
            ProgressChanged?.Invoke(progress);
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }

    // 状态枚举
    public enum UpgradeStatus
    {
        Starting,
        Transferring,
        Completed,
        Canceled,
        Failed
    }

    // 自定义异常
    public class FirmwareTransferException : Exception
    {
        public FirmwareTransferException(string message, Exception inner = null)
            : base(message, inner) { }
    }

}
