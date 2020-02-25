using System;
using System.Collections.Generic;
using System.IO;
using RiffParserDemo.Models;

// ReSharper disable IdentifierTypo

namespace RiffParserDemo
{
    #region RiffParserException

    public class RiffParserException : ApplicationException
    {
        public RiffParserException()
            : base()
        {
        }

        public RiffParserException(string message)
            : base(message)
        {
        }

        public RiffParserException(string message, Exception inner)
            : base(message, inner)
        {
        }

    }

    #endregion

    public class RiffParser
    {
        public const int Bytesize = 1;
        public const int Wordsize = 2;
        public const int Dwordsize = 4;
        public const int Twodwordssize = 8;
        public readonly string Keygroup = "kgrp";
        public readonly string PROGRAM = "prg ";
        public readonly string Riff4Cc = "RIFF";
        public readonly string Rifx4Cc = "RIFX";
        public readonly string List4Cc = "LIST";
        private readonly byte[] _eightBytes = new byte[Twodwordssize];
        private readonly byte[] _fourBytes = new byte[Dwordsize];
        private readonly byte[] _twoBytes = new byte[Wordsize];
        private readonly byte[] _oneByte = new byte[Bytesize];
        private int lfoCount = 0;
        private int _mDatasize;
        private FileStream _mStream;

        private Program program;

        public long FileSize { get; set; }
        public string FileName { get; private set; }
        public string ShortName { get; private set; }
        public int FileRiff { get; private set; }
        public int FileType { get; private set; }

        public RiffParser(Program program)
        {
            this.program = program;
            lfoCount = 0;
        }

        public void OpenFile(string filename)
        {
            // Sanity check
            if (null != _mStream) {
                throw new RiffParserException("RIFF file already open " + FileName);
            }

            // Opening a new file
            try {
                FileInfo fi = new FileInfo(filename);
                FileName = fi.FullName;
                ShortName = fi.Name;
                FileSize = fi.Length;

                //Console.WriteLine(ShortName + " is a valid file.");

                // Read the RIFF header
                _mStream = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                int fourCc;
                int datasize;
                int fileType;

                ReadTwoInts(out fourCc, out datasize);
                ReadOneInt(out fileType); 

                FileRiff = fourCc;
                FileType = fileType;

                // Check for a valid RIFF header
                string riff = FromFourCc(fourCc);
                if (0 == String.CompareOrdinal(riff, Riff4Cc)
                    || 0 == String.CompareOrdinal(riff, Rifx4Cc))
                {
                    _mDatasize = datasize;
                    if (FileSize >= _mDatasize + Twodwordssize)
                    {
                        Console.WriteLine(ShortName + " has a valid size");
                    }
                    else
                    {
                        _mStream.Close(); _mStream = null;
                        throw new RiffParserException("Error. Truncated file " + FileName);
                    }
                }
                else
                {
                    _mStream.Close(); _mStream = null;
                    throw new RiffParserException("Error. Not a valid RIFF file " + FileName);
                }
            }
            catch (Exception exception)
            {
				if (null != _mStream) 
				{
					_mStream.Close(); _mStream = null;
				}
                throw new RiffParserException("Error. Problem reading file " + FileName, exception);
            }
        }

        public bool ReadElement(ref long bytesleft)
        {
            // Are we done?
            if (Twodwordssize > bytesleft)
            {
                return false;
            }

            // We have enough bytes, read
            int fourCc;
            int size;

            ReadTwoInts(out fourCc, out size);
            string type = FromFourCc(fourCc);
            //Console.WriteLine("Found element of type \"" + type + "\" and length " + size.ToString());

            // Reduce bytes left
            bytesleft -= Twodwordssize;

            // Do we have enough bytes?
            if (bytesleft < size)
            {
                // Skip the bad data and throw an exception
                SkipData(bytesleft);
                bytesleft = 0;
                throw new RiffParserException("Element size mismatch for element " + FromFourCc(fourCc)
                + " need " + size.ToString() + " but have only " + bytesleft.ToString());
            }

            if (0 == string.CompareOrdinal(type, List4Cc))
            {
                // We have a list
                ReadOneInt(out fourCc);
                ProcessList(fourCc, size - 4);

                // Adjust size
                bytesleft -= size;
            }
            else
            {
                // Calculated padded size - padded to WORD boundary
                int paddedSize = size;
                if (0 != (size & 1)) 
                    ++paddedSize;
                
                if (type == Keygroup)
                {
                    //Console.WriteLine("Processing KeyGroups");
                    ProcessKgrp(size);
                }
                else
                {
                    ProcessChunk(fourCc, size, paddedSize);
                }

                // Adjust size
                bytesleft -= paddedSize;
            }

            return true;
        }

        public unsafe void ReadTwoInts(out int fourCc, out int size)
        {
            try {
                int readsize = _mStream.Read(_eightBytes, 0, Twodwordssize);

                if (Twodwordssize != readsize) {
                    throw new RiffParserException("Unable to read. Corrupt RIFF file " + FileName);
                }

                fixed (byte* bp = &_eightBytes[0]) {
                    fourCc = *((int*)bp);
                    size = *((int*)(bp + Dwordsize));
                }
            }
            catch (Exception ex)
            {
                throw new RiffParserException("Problem accessing RIFF file " + FileName, ex);
            }
        }

        public unsafe void ReadOneInt(out int fourCc)
        {
            try {
                int readsize = _mStream.Read(_fourBytes, 0, Dwordsize);

                if (Dwordsize != readsize) {
                    throw new RiffParserException("Unable to read. Corrupt RIFF file " + FileName);
                }

                fixed (byte* bp = &_fourBytes[0]) {
                    fourCc = *((int*)bp);
                }
            }
            catch (Exception ex)
            {
                throw new RiffParserException("Problem accessing RIFF file " + FileName, ex);
            }
        }

        public unsafe void ReadHalfInt(out int fourCc)
        {
            try
            {
                int readsize = _mStream.Read(_twoBytes, 0, Wordsize);

                if (Wordsize != readsize)
                {
                    throw new RiffParserException("Unable to read. Corrupt RIFF file " + FileName);
                }

                fixed (byte* bp = &_twoBytes[0])
                {
                    fourCc = *((int*)bp);
                }
            }
            catch (Exception ex)
            {
                throw new RiffParserException("Problem accessing RIFF file " + FileName, ex);
            }
        }

        public unsafe int ReadByte()
        {
            int oneCc;
            try
            {
                int readsize = _mStream.Read(_oneByte, 0, Bytesize);

                if (Bytesize != readsize)
                {
                    throw new RiffParserException("Unable to read. Corrupt RIFF file " + FileName);
                }

                fixed (byte* bp = &_oneByte[0])
                {
                    oneCc = *((int*)bp);
                }


                return oneCc;
            }
            catch (Exception ex)
            {
                throw new RiffParserException("Problem accessing RIFF file " + FileName, ex);
            }
        }

        public void CloseFile()
        {
            if (null != _mStream)
            {
                _mStream.Close();
                _mStream = null;
            }
        }

        public void ProcessList(int fourCc, long length)
        {
            string type = FromFourCc(fourCc);
            Console.WriteLine("Found list element of type \"" + type + "\" and length " + length.ToString());


            // Read all the elements in the current list
            try
            {
                while (length > 0)
                {
                    // Prefix each line with the type of the current list
                    Console.Write(type + " (" + length.ToString() + "): ");
                    // Get the next element (if there is one)
                    if (false == ReadElement(ref length)) break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem: " + ex.ToString());
            }
        }

        public void ProcessChunk(int fourCc, int length, int paddedLength)
        {
            string type = FromFourCc(fourCc);
            //Console.WriteLine("Found chunk element of type \"" + type + "\" and length " + length.ToString());
            switch (type)
            {
                case "prg ":
                    ReadBytes(length, program.PrgValues);
                    break;
                case "out ":
                    ReadBytes(length, program.OutValues);
                    break;
                case "tune":
                    ReadBytes(length, program.TuneValues);
                    break;
                case "lfo ":
                    if (lfoCount == 0)
                    {
                        ReadBytes(length, program.LfoValues);
                        lfoCount++;
                    }
                    else
                    {
                        ReadBytes(length, program.Lfo2Values);
                        lfoCount++;
                    }
                    break;
                case "mods":
                    ReadBytes(length, program.ModsValues);
                    break;
                default:
                    SkipData(paddedLength);
                    break;
            }
        }

        public void ProcessKgrp(int length)
        {
            var keygroup = new Keygroup();
            int fourCc;
            int size;
            int countEnv = 0;
            int countZone = 0;
            int zoneNumber = 1;
            while (length > 0)
            {
                ReadTwoInts(out fourCc, out size);
                length -= Twodwordssize;
                var type = FromFourCc(fourCc);
                //Console.WriteLine("Found element called \"" + type + "\" and length " + size.ToString());
                List<string> list = new List<string>();
                list = SetList(type, list, keygroup, ref countEnv, ref countZone);
                if (type == "zone")
                {
                    string sampleName = "";
                    var valueByte = ReadByte();
                    var charFromByte = FromOneCc(valueByte);
                    var numberOfChars = ReadByte();
                    sampleName = SampleName(numberOfChars);
                    if (zoneNumber == 1)
                    {
                        // on 1st zone
                    }
                    //Console.WriteLine("Sample Name: " + sampleName);
                    list.Add(sampleName);
                    ReadBytes(14, list);
                    length -= 48;
                    zoneNumber++;
                }
                else
                {
                    ReadBytes(size, list);
                    length -= size;
                }
            }

            program.Keygroups.Add(keygroup);
        }

        private string SampleName(int numberOfChars)
        {
            string word = "";

            for (int i = 1; i <= 32; i++)
            {
                if (i <= numberOfChars)
                {
                    word += FromOneCc(ReadByte());
                }
                else
                {
                    //word += FromOneCc(ReadByte());
                    var byteVal = ReadByte();
                }
            }

            return word;
        }

        private static List<string> SetList(string type, List<string> list, Keygroup keygroup, ref int countEnv, ref int countZone)
        {
            switch (type)
            {
                case "kloc":
                    list = keygroup.Klocation;
                    break;
                case "env ":
                    switch (countEnv)
                    {
                        case 0:
                            list = keygroup.AmpEnv;
                            countEnv++;
                            break;
                        case 1:
                            list = keygroup.FilterEnv;
                            countEnv++;
                            break;
                        case 2:
                            list = keygroup.AuxEnv;
                            countEnv++;
                            break;
                    }

                    break;
                case "filt":
                    list = keygroup.Filter;
                    break;
                case "zone":
                    switch (countZone)
                    {
                        case 0:
                            list = keygroup.Zone1;
                            countZone++;
                            break;
                        case 1:
                            list = keygroup.Zone2;
                            countZone++;
                            break;
                        case 2:
                            list = keygroup.Zone3;
                            countZone++;
                            break;
                        case 3:
                            list = keygroup.Zone4;
                            countZone++;
                            break;
                    }
                    break;
            }

            return list;
        }

        private void ReadBytes(int numberBytes, List<int> list)
        {
            for (int j = 0; j < numberBytes; j = j + 1)
            {
                var valueByte = ReadByte();
                //Console.WriteLine("Found byte of type '" + valueByte + "'");
                list.Add(valueByte);
            }
        }

        private void ReadBytes(int numberBytes, List<string> list)
        {
            for (int j = 0; j < numberBytes; j = j + 1)
            {
                var valueByte = ReadByte();
                //Console.WriteLine("Found byte of type '" + valueByte + "'");
                list.Add(valueByte.ToString());
            }
        }

        public string FromOneCc(int oneCc)
        {
            if (oneCc == 0) return string.Empty;
            char ch = (char) (oneCc & 0xFF);
            return ch.ToString();
        }

        public string FromFourCc(int fourCc)
        {
            char[] chars = new char[4];
            chars[0] = (char)(fourCc & 0xFF);
            chars[1] = (char)((fourCc >> 8) & 0xFF);
            chars[2] = (char)((fourCc >> 16) & 0xFF);
            chars[3] = (char)((fourCc >> 24) & 0xFF);

            return new string(chars);
        }

        public void SkipData(long skipBytes)
        {
            try
            {
                _mStream.Seek(skipBytes, SeekOrigin.Current);
            }
            catch (Exception ex)
            {
                throw new RiffParserException("Problem seeking in file " + FileName, ex);
            }
        }

    }
}