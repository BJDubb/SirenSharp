using System.IO;
using System.Linq;
using SirenSharp.Models;
using Xunit;

namespace SirenSharp.Tests
{
    public class ProjectTests
    {
        [Fact]
        public void SaveLoad_RoundTripsSoundSetsAndSounds()
        {
            using var dir = new TempDir();
            var path = dir.File("demo.ssproj");

            var project = new Project("demo", path) { DLCName = "policesirens" };
            var set = new SoundSet("lspd");
            set.AddSound(new Sound { Name = "wail", AudioPath = @"C:\sirens\wail.wav" });
            project.SoundSets.Add(set);
            project.Save();

            var loaded = Project.Load(path);

            Assert.Equal("demo", loaded.ProjectName);
            Assert.Equal("policesirens", loaded.DLCName);
            Assert.Equal(path, loaded.ProjectPath);
            var loadedSet = Assert.Single(loaded.SoundSets);
            Assert.Equal("lspd", loadedSet.Name);
            var loadedSound = Assert.Single(loadedSet.Sounds);
            Assert.Equal("wail", loadedSound.Name);
            Assert.Equal(@"C:\sirens\wail.wav", loadedSound.AudioPath);
        }

        [Fact]
        public void HasUnsavedChanges_TrueAfterEditFalseAfterSave()
        {
            using var dir = new TempDir();
            var path = dir.File("demo.ssproj");

            var project = new Project("demo", path);
            Assert.False(project.HasUnsavedChanges());

            project.SoundSets.Add(new SoundSet("lspd"));
            Assert.True(project.HasUnsavedChanges());

            project.Save();
            Assert.False(project.HasUnsavedChanges());
        }

        [Fact]
        public void GetErrors_FlagsDuplicateSoundsetNames()
        {
            using var dir = new TempDir();
            var project = new Project("demo", dir.File("demo.ssproj"));
            project.SoundSets.Add(new SoundSet("lspd"));
            project.SoundSets.Add(new SoundSet("lspd"));

            Assert.False(project.IsValid());
            Assert.Contains(project.GetErrors(), e => e.Contains("same name"));
        }
    }
}
