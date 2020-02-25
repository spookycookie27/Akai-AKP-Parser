using System;
using System.Text;

namespace RiffParserDemo
{
    class RiffParserDemo
    {
        // Parse a RIFF file
        static void Main(string[] args)
        {
			// Create a parser instance
            RiffParser rp = new RiffParser();
			try 
			{
				string filename = @"C:\Program Files\Microsoft Visual Studio .NET 2003\Common7\Graphics\videos\BLUR24.avi";
				//string filename = @"C:\WINNT\Media\Chimes.wav"
				if (0 != args.Length)  
				{
					filename = args[0];
				}
					
				// Specify a file to open
				rp.OpenFile(filename);

				// If we got here - the file is valid. Output information about the file
				Console.WriteLine("File " + rp.ShortName + " is a \"" + RiffParser.FromFourCC(rp.FileRIFF)
					+ "\" with a specific type of \"" + RiffParser.FromFourCC(rp.FileType) + "\"");

				// Store the size to loop on the elements
				int size = rp.DataSize;

				// Define the processing delegates
				RiffParser.ProcessChunkElement pc = new RiffParser.ProcessChunkElement(ProcessChunk);
				RiffParser.ProcessListElement pl = new RiffParser.ProcessListElement(ProcessList);

				// Read all top level elements and chunks
				while (size > 0)
				{
					// Prefix the line with the current top level type
					Console.Write(RiffParser.FromFourCC(rp.FileType) + " (" + size.ToString() + "): ");
					// Get the next element (if there is one)
					if (false == rp.ReadElement(ref size, pc, pl)) break;
				}
				// Close the stream
				rp.CloseFile();
				Console.WriteLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine("-----------------");
				Console.WriteLine("Problem: " + ex.ToString());
			}
            Console.WriteLine("\n\rDone. Press 'Enter' to exit.");
            Console.ReadLine();
        }

        // Process a RIFF list element (list sub elements)
        public static void ProcessList(RiffParser rp, int FourCC, int length)
        {
            string type = RiffParser.FromFourCC(FourCC);
            Console.WriteLine("Found list element of type \"" + type + "\" and length " + length.ToString());

			// Define the processing delegates
			RiffParser.ProcessChunkElement pc = new RiffParser.ProcessChunkElement(ProcessChunk);
			RiffParser.ProcessListElement pl = new RiffParser.ProcessListElement(ProcessList);

			// Read all the elements in the current list
            try {
                while (length > 0) {
					// Prefix each line with the type of the current list
                    Console.Write(type + " (" + length.ToString() + "): ");
					// Get the next element (if there is one)
                    if (false == rp.ReadElement(ref length, pc, pl)) break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem: " + ex.ToString());
            }
        }

        // Process a RIFF chunk element (skip the data)
        public static void ProcessChunk(RiffParser rp, int FourCC, int length, int paddedLength)
        {
            string type = RiffParser.FromFourCC(FourCC);
            Console.WriteLine("Found chunk element of type \"" + type + "\" and length " + length.ToString());

            // Skip data and update bytesleft
            rp.SkipData(paddedLength);
        }
    }
}
