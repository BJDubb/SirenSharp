using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using SirenSharp.Validators;

namespace SirenSharp.Models
{
    [Serializable]
    [XmlRoot("SirenSharpProject")]
    public class Project
    {
        [XmlElement("Name")]
        public string ProjectName { get; set; } = string.Empty;
        [XmlIgnore]
        public string ProjectPath { get; set; } = string.Empty;

        public string DLCName { get; set; } = string.Empty;
        public ObservableCollection<SoundSet> SoundSets { get; set; } = new();

        public Project()
        {
            
        }

        public Project(string projectName, string projectPath)
        {
            ProjectName = projectName;
            ProjectPath = projectPath;

            SoundSets = new ObservableCollection<SoundSet>();

            GenerateProjectFile();
        }

        private void GenerateProjectFile()
        {
            XmlWriterSettings settings = new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true };


            XmlSerializer serializer = new XmlSerializer(typeof(Project));
            using (var xml = XmlWriter.Create(ProjectPath, settings))
            {
                serializer.Serialize(xml, this, new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));

            }
        }

        public static Project Load(string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Project));
            using var fs = new FileStream(filePath, FileMode.Open);

            if (serializer.Deserialize(fs) is not Project project)
                throw new InvalidDataException("The selected file is not a valid SirenSharp project.");

            project.ProjectPath = filePath;
            project.SoundSets ??= new ObservableCollection<SoundSet>();

            return project;
        }

        public void Save()
        {
            Save(ProjectPath);
        }

        public void Save(string filepath)
        {
            XmlWriterSettings settings = new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true };


            XmlSerializer serializer = new XmlSerializer(typeof(Project));
            using (var xml = XmlWriter.Create(filepath, settings))
            {
                serializer.Serialize(xml, this, new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));
            }
        }

        public void SaveAs(string filepath)
        {
            Save(filepath);
        }

        public bool HasUnsavedChanges()
        {
            MemoryStream stream = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true };

            XmlSerializer serializer = new XmlSerializer(typeof(Project));
            using (var xml = XmlWriter.Create(stream, settings))
            {
                serializer.Serialize(xml, this, new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));
            }
            var serializedProject = Encoding.UTF8.GetString(stream.ToArray()).Replace("\uFEFF", "");
            

            var diskProject = File.ReadAllText(ProjectPath);

            return serializedProject != diskProject;
        }

        public bool IsValid()
        {
            return GetErrors().Count == 0;
        }

        public List<string> GetErrors()
        {
            var errors = new List<string>();

            if (SoundSets.GroupBy(x => x.Name).Count() != SoundSets.Count)
            {
                errors.Add("AWC's cannot have the same name");
            }

            foreach (var soundSet in SoundSets)
            {
                var validationResult = new AwcNameValidator().ValidateValue(soundSet.Name);
                if (!validationResult.IsValid) errors.Add($"AWC ({soundSet.Name}): {validationResult.ErrorContent}");

                if (soundSet.Sounds.GroupBy(x => x.Name).Count() != soundSet.Sounds.Count)
                {
                    errors.Add($"AWC ({soundSet.Name}) cannot have sirens with the same name");
                }

                foreach (var sound in soundSet.Sounds)
                {
                    validationResult = new SirenNameValidator().ValidateValue(sound.Name);
                    if (!validationResult.IsValid) errors.Add($"Siren ({soundSet.Name}/{sound.Name}): {validationResult.ErrorContent}");

                    if (string.IsNullOrWhiteSpace(sound.AudioPath) || !File.Exists(sound.AudioPath))
                    {
                        errors.Add($"Siren ({soundSet.Name}/{sound.Name}): No WAV file selected or file does not exist");
                    }
                }
            }

            return errors;
        }
    }
}
