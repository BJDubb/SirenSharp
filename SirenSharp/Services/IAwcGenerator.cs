using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SirenSharp.Models;

namespace SirenSharp.Services
{
    public interface IAwcGenerator
    {
        public string GenerateAwcXml(SoundSet soundSet);
    }
}
