using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    // public static byte[] FromAudioClip(AudioClip clip, int targetSampleRate = 8000)
    // {
    //     using (var stream = new MemoryStream())
    //     {
    //         float[] samples = new float[clip.samples * clip.channels];
    //         clip.GetData(samples, 0);

    //         // 写入 WAV 头部
    //         WriteHeader(stream, clip);
    //         // 写入 PCM 数据
    //         ConvertAndWrite(stream, samples);

    //         return stream.ToArray();
    //     }
    // }

    // private static void WriteHeader(Stream stream, AudioClip clip)
    // {
    //     int hz = clip.frequency;
    //     int channels = clip.channels;
    //     int samples = clip.samples;

    //     stream.Seek(0, SeekOrigin.Begin);

    //     stream.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, 4);
    //     stream.Write(BitConverter.GetBytes(stream.Length - 8), 0, 4);
    //     stream.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, 4);
    //     stream.Write(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, 4);
    //     stream.Write(BitConverter.GetBytes(16), 0, 4);
    //     stream.Write(BitConverter.GetBytes((ushort)1), 0, 2);
    //     stream.Write(BitConverter.GetBytes((ushort)channels), 0, 2);
    //     stream.Write(BitConverter.GetBytes(hz), 0, 4);
    //     stream.Write(BitConverter.GetBytes(hz * channels * 2), 0, 4);
    //     stream.Write(BitConverter.GetBytes((ushort)(channels * 2)), 0, 2);
    //     stream.Write(BitConverter.GetBytes((ushort)16), 0, 2);
    //     stream.Write(System.Text.Encoding.UTF8.GetBytes("data"), 0, 4);
    //     stream.Write(BitConverter.GetBytes(samples * channels * 2), 0, 4);
    // }

    // private static void ConvertAndWrite(Stream stream, float[] samples)
    // {
    //     Int16[] intData = new Int16[samples.Length];
    //     Byte[] bytesData = new Byte[samples.Length * 2];

    //     for (int i = 0; i < samples.Length; i++)
    //     {
    //         intData[i] = (short)(samples[i] * 32767);
    //         byte[] byteArr = BitConverter.GetBytes(intData[i]);
    //         byteArr.CopyTo(bytesData, i * 2);
    //     }
    //     stream.Write(bytesData, 0, bytesData.Length);
    // }

    public static byte[] FromAudioClip(AudioClip clip, int targetSampleRate = 8000)
    {
        // 原始数据
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        // 降采样前的参数
        int originalRate = clip.frequency;
        int channels = clip.channels;
        int originalLength = samples.Length / channels; // 原始采样点数（每个声道）

        // 计算降采样后的采样点数
        int targetLength = (int)((float)originalLength * targetSampleRate / originalRate);

        // 降采样后的浮点数组（每个声道交替存放）
        float[] resampled = new float[targetLength * channels];

        // 简单的线性插值降采样
        for (int ch = 0; ch < channels; ch++)
        {
            for (int i = 0; i < targetLength; i++)
            {
                // 原始位置（浮点）
                float srcPos = (float)i * originalRate / targetSampleRate;
                int srcIndex1 = (int)srcPos;
                int srcIndex2 = Mathf.Min(srcIndex1 + 1, originalLength - 1);
                float frac = srcPos - srcIndex1;

                // 从原始数组中获取对应声道的样本
                float s1 = samples[srcIndex1 * channels + ch];
                float s2 = samples[srcIndex2 * channels + ch];

                // 线性插值
                resampled[i * channels + ch] = Mathf.Lerp(s1, s2, frac);
            }
        }

        // 将 float 转换为 16-bit PCM 字节
        // int byteCount = resampled.Length * 2; // 每个样本 2 字节
        // byte[] wavData = new byte[byteCount];
        // for (int i = 0; i < resampled.Length; i++)
        // {
        //     short shortValue = (short)(resampled[i] * short.MaxValue);
        //     byte[] shortBytes = BitConverter.GetBytes(shortValue);
        //     wavData[i * 2] = shortBytes[0];
        //     wavData[i * 2 + 1] = shortBytes[1];
        // }
        // byte[] header = CreateWavHeader(wavData.Length, targetSampleRate, channels, 16);

        byte[] wavData = new byte[resampled.Length]; // 每个样本 1 字节
        for (int i = 0; i < resampled.Length; i++)
        {
            // 将 [-1,1] 映射到 [0,255]
            byte unsignedByte = (byte)((resampled[i] * 0.5f + 0.5f) * 255);
            wavData[i] = unsignedByte;
        }

        // 构建 WAV 头
        byte[] header = CreateWavHeader(wavData.Length, targetSampleRate, channels, 8);
        byte[] result = new byte[header.Length + wavData.Length];
        Buffer.BlockCopy(header, 0, result, 0, header.Length);
        Buffer.BlockCopy(wavData, 0, result, header.Length, wavData.Length);
        return result;
    }

    private static byte[] CreateWavHeader(int dataLength, int sampleRate, int channels, int bitDepth)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            // int byteRate = sampleRate * channels * (bitDepth / 8);
            // int blockAlign = channels * (bitDepth / 8);
            int blockAlign = channels;
            int byteRate = sampleRate * channels;
            int totalDataLen = dataLength + 36; // 44 - 8

            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(totalDataLen);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16); // PCM header size
            writer.Write((short)1); // PCM format
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)blockAlign);
            writer.Write((short)bitDepth);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            writer.Write(dataLength);
            return stream.ToArray();
        }
    }
}