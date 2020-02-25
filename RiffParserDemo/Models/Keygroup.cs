using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiffParserDemo.Models
{
    public class Keygroup
    {
        public Keygroup()
        {
            Klocation = new List<string>();
            AmpEnv = new List<string>();
            FilterEnv = new List<string>();
            AuxEnv = new List<string>();
            Filter = new List<string>();
            Zone1 = new List<string>();
            Zone2 = new List<string>();
            Zone3 = new List<string>();
            Zone4 = new List<string>();
        }

        public List<string> Klocation { get; set; }
        public List<string> AmpEnv { get; set; }
        public List<string> FilterEnv { get; set; }
        public List<string> AuxEnv { get; set; }
        public List<string> Filter { get; set; }
        public List<string> Zone1 { get; set; }
        public List<string> Zone2 { get; set; }
        public List<string> Zone3 { get; set; }
        public List<string> Zone4 { get; set; }
        
    }
}
