using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using RiffParserDemo.Models;

namespace RiffParserDemo
{
    class RiffParserDemo
    {
        static void Main(string[] args)
        {
			try
            {
                var allPrograms = new List<Program>();
                string path = @"D:\Matt - AMG\Kick Ass Brass for Steve\All Programs\";
                string filename;
                var files = Directory.EnumerateFiles(path);

                foreach (string currentFile in files)
                {
                    Program program = new Program();
                    filename = currentFile.Substring(path.Length);
                    program.Name = filename.Replace(".akp", "");
                    string filePath = $"{path}\\{filename}";
                    RiffParser rp = new RiffParser(program);

                    rp.OpenFile(filePath);

                    // If we got here - the file is valid. Output information about the file
                    Console.WriteLine("File " + rp.ShortName + " is a \"" + rp.FromFourCc(rp.FileRiff)
                                      + "\" with a specific type of \"" + rp.FromFourCc(rp.FileType) + "\"");

                    // Store the size to loop on the elements
                    long size = rp.FileSize - 8;

                    // Read all top level elements and chunks
                    while (size > 0)
                    {
                        // Prefix the line with the current top level type
                        //Console.Write(rp.FromFourCc(rp.FileType) + " (" + size.ToString() + "): ");

                        if (false == rp.ReadElement(ref size)) break;
                    }
                    rp.CloseFile();
                    allPrograms.Add(program);
                }

                var data = JsonConvert.SerializeObject(allPrograms);
				Console.WriteLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Problem: " + ex.ToString());
			}
            Console.WriteLine("\n\rDone. Press 'Enter' to exit.");
            Console.ReadLine();
        }
    }
}
