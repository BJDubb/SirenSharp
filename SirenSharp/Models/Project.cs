using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

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
    }
}
