using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SirenSharp.Models;

namespace SirenSharp.Services
{
    public interface IDataGenerator
    {
        public string GenerateDatXml(DLC dlc, List<SoundSet> soundSets);
        public string GenerateNametable(List<SoundSet> soundSets);
    }
}
