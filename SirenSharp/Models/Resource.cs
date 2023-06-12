using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SirenSharp.Models
{
    public class Resource
    {
        public List<DLC> DLCs {  get; set; }
        
        public string Name { get; }

        public Resource(string name)
        {
            DLCs = new List<DLC>();
            
            Name = name;
        }
    }
}
