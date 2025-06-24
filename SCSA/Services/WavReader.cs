using System;
using System.IO;
using System.Text;

namespace SCSA.Services
{
    public record WavData(int SampleRate, int Channels, short BitsPerSample, double[][] Samples);

    public static class WavReader
    {
        public static WavData Read(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs, Encoding.UTF8);

            // RIFF header
            var chunkId = br.ReadBytes(4); // "RIFF"
            var chunkSize = br.ReadInt32();
            var format = br.ReadBytes(4); // "WAVE"

            // fmt sub-chunk
            var subChunk1Id = br.ReadBytes(4); // "fmt "
            var subChunk1Size = br.ReadInt32();
            var audioFormat = br.ReadInt16(); // 1 for PCM
            var numChannels = br.ReadInt16();
            var sampleRate = br.ReadInt32();
            var byteRate = br.ReadInt32();
            var blockAlign = br.ReadInt16();
            var bitsPerSample = br.ReadInt16();

            if (audioFormat != 1)
            {
                throw new NotSupportedException("Only PCM WAV files are supported.");
            }

            // data sub-chunk
            var subChunk2Id = br.ReadBytes(4); // "data"
            while (Encoding.UTF8.GetString(subChunk2Id) != "data")
            {
                var extraSize = br.ReadInt32();
                br.ReadBytes(extraSize);
                subChunk2Id = br.ReadBytes(4);
            }
            var subChunk2Size = br.ReadInt32();

            int bytesPerSample = bitsPerSample / 8;
            int numSamples = subChunk2Size / blockAlign;
            
            var samples = new double[numChannels][];
            for(int i=0; i<numChannels; i++)
            {
                samples[i] = new double[numSamples];
            }

            for (int i = 0; i < numSamples; i++)
            {
                for (int j = 0; j < numChannels; j++)
                {
                    if (bitsPerSample == 16)
                    {
                        samples[j][i] = br.ReadInt16() / 32768.0; // Normalize to [-1, 1]
                    }
                    else if (bitsPerSample == 32)
                    {
                        if (audioFormat == 1) // Integer PCM
                        {
                            samples[j][i] = br.ReadInt32() / (double)Int32.MaxValue;
                        }
                        else if (audioFormat == 3) // Floating point PCM
                        {
                           samples[j][i] = br.ReadSingle();
                        }
                    } else if (bitsPerSample == 8)
                    {
                        samples[j][i] = (br.ReadByte() - 128) / 128.0; // Normalize to [-1, 1]
                    }
                    else
                    {
                         // Skip unsupported bit depths
                        br.ReadBytes(bytesPerSample);
                    }
                }
            }

            return new WavData(sampleRate, numChannels, bitsPerSample, samples);
        }
    }
} 