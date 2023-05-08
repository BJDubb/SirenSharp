using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SirenSharp
{
    public class CodeWalker
    {
        public class FileTypeInfo
        {
            public string Name { get; set; }
            public string Extension { get; set; }
            public int ImageIndex { get; set; }
            public FileTypeAction DefaultAction { get; set; }
            public List<FileTypeInfo> SubTypes { get; set; }
            public bool XmlConvertible { get; set; }

            public FileTypeInfo(string extension, string name, int imageindex, FileTypeAction defaultAction, bool xmlConvertible)
            {
                Name = name;
                Extension = extension;
                ImageIndex = imageindex;
                DefaultAction = defaultAction;
                XmlConvertible = xmlConvertible;
            }

            public void AddSubType(FileTypeInfo t)
            {
                if (SubTypes == null) SubTypes = new List<FileTypeInfo>();
                SubTypes.Add(t);
            }
        }

        public enum FileTypeAction
        {
            ViewHex = 0,
            ViewText = 1,
            ViewXml = 2,
            ViewYtd = 3,
            ViewYmt = 4,
            ViewYmf = 5,
            ViewYmap = 6,
            ViewYtyp = 7,
            ViewJPso = 8,
            ViewModel = 9,
            ViewCut = 10,
            ViewAwc = 11,
            ViewGxt = 12,
            ViewRel = 13,
            ViewFxc = 14,
            ViewYwr = 15,
            ViewYvr = 16,
            ViewYcd = 17,
            ViewYnd = 18,
            ViewCacheDat = 19,
            ViewYed = 20,
            ViewYld = 21,
            ViewYfd = 22,
            ViewHeightmap = 23,
            ViewMrf = 24,
            ViewNametable = 25,
            ViewDistantLights = 26,
            ViewYpdb = 27,
        }
    }
}
