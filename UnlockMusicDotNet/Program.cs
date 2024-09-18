using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UnlockMusicDotNet
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string currentPath = Directory.GetCurrentDirectory();
            var files = Directory.GetFiles(currentPath, ".", SearchOption.AllDirectories).Where(s => s.ToLower().EndsWith(".qmc3") || s.ToLower().EndsWith(".qmcflac") || s.ToLower().EndsWith(".qmcogg") || s.ToLower().EndsWith(".qmc0") || s.ToLower().EndsWith(".mflac"));

            var tasks = new List<Task>();
            if (files.Count() > 0)
            {
                foreach (var file in files)
                {
                    if(tasks.Count(s => s.Status == TaskStatus.Running) > 5)
                    {
                        await Task.WhenAny(tasks);
                    }

                    tasks.Add(Task.Run(() =>
                    {
                        if (Path.GetExtension(file).Equals(".mflac", StringComparison.OrdinalIgnoreCase))
                            ProcessMflac(file);
                        else
                            ProcessAncient(file);
                    }));

                }

                await Task.WhenAll(tasks);
                Console.WriteLine("Finish");
            }
            else
            {
                Console.WriteLine("Can Not find QQMusic file in directory");
            }

            Console.WriteLine("UnlockMusicDotNet override by KimiDing");
            Console.ReadKey();
        }

        private static void ProcessAncient(object filePath)
        {
            using (FileStream fsreadFile = new FileStream((string)filePath, FileMode.Open))
            {
                string outputPath = Path.GetFileNameWithoutExtension((string)filePath) + ProcessExtension((string)filePath);
                if (File.Exists(outputPath))
                {
                    Console.WriteLine(outputPath + " is exist skip!!!");
                }
                else
                {
                    using (FileStream fswriteFile = new FileStream(outputPath, FileMode.CreateNew))
                    {
                        byte[] buffer = new byte[8192];
                        int readSize = 0;
                        int offset = 0;
                        Seed seed = new Seed();
                        do
                        {
                            readSize = fsreadFile.Read(buffer, 0, 8192);
                            for (int i = 0; i < 8192; i++)
                            {
                                buffer[i] = Convert.ToByte(seed.next_mask() ^ buffer[i]);
                            }
                            offset += readSize;
                            fswriteFile.Write(buffer, 0, buffer.Length);
                        } while (readSize > 0);

                        Console.WriteLine(filePath + " Done!!!");
                    }
                }

            }

        }

        private static void ProcessMflac(object filePath)
        {
            using (FileStream fsreadFile = new FileStream((string)filePath, FileMode.Open))
            {
                string outputPath = Path.GetFileNameWithoutExtension((string)filePath) + ProcessExtension((string)filePath);
                if (File.Exists(outputPath))
                {
                    Console.WriteLine(outputPath + " is exist skip!!!");
                }
                else
                {
                    using (FileStream fswriteFile = new FileStream(outputPath, FileMode.CreateNew))
                    {

                        byte[] tempBuffer = new byte[fsreadFile.Length - 0x170];
                        byte[] buffer = new byte[8192];
                        int readSize = 0;
                        int offset = 0;

                        Mask mask = new Mask();
                        fsreadFile.Read(tempBuffer, 0, (int)(fsreadFile.Length - 0x170));
                        if (mask.detectMask(tempBuffer))
                        {
                            fsreadFile.Position = 0;
                            do
                            {
                                readSize = fsreadFile.Read(buffer, 0, 8192);
                                for (int i = 0; i < 8192; i++)
                                {
                                    buffer[i] = Convert.ToByte(mask.nextMask() ^ buffer[i]);
                                }
                                offset += readSize;
                                fswriteFile.Write(buffer, 0, buffer.Length);
                            } while (readSize > 0);

                            Console.WriteLine(filePath + " Done!!!");
                        }
                        else
                        {
                            Console.WriteLine(filePath + " Not Support!!!");
                        }
                    }
                }
            }
        }

        private static string ProcessExtension(string filePath)
        {
            string ext = Path.GetExtension(filePath);
            switch (ext)
            {
                case ".qmcflac":
                    return ".flac";
                case ".qmc3":
                    return ".mp3";
                case ".qmc0":
                    return ".mp3";
                case ".qmcogg":
                    return ".ogg";
                case ".mflac":
                    return ".flac";
                default:
                    return "";
            }
        }
    }

}
