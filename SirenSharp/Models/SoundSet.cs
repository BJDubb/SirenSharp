using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using SirenSharp.Converters;

namespace SirenSharp.Models
{
    public class SoundSet : ObservableObject
    {

        public ObservableCollection<Sound> Sounds { get; set; }
        private string name;
        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged();                
            }
        }

        public SoundSet()
        {
        }

        public SoundSet(string name)
        {
            Name = name;
            Sounds = new ObservableCollection<Sound>();
        }

        public void AddSound(Sound sound)
        {
            Sounds.Add(sound);
        }
    }
}
