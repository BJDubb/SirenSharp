using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SirenSharp.Models;

namespace SirenSharp.Services
{
    public interface IResourceGenerator
    {
        public void GenerateResource(string resourceName, string dlcName, string folderPath, List<SoundSet> soundSets);
    }
}
