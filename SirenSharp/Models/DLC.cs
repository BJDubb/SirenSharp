using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SirenSharp.Models
{
    public class DLC
    {
        public string Name { get; set; }
        public List<SoundSet> SoundSets { get; set; }
        public DLC(string name)
        {
            Name = name;
            SoundSets = new List<SoundSet>();
        }
    }
}
