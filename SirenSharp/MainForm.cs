using System.Windows.Forms.VisualStyles;
using CodeWalker;
using CodeWalker.GameFiles;
using Microsoft.VisualBasic.Devices;
using Microsoft.WindowsAPICodePack.Dialogs;
using static SirenSharp.CodeWalker;

namespace SirenSharp
{
    public partial class MainForm : Form
    {

        private Dictionary<string, FileTypeInfo> FileTypes;

        private string awcPath;
        private AwcFile? awcFile;

        private List<Siren> sirens = new List<Siren>();
        private Siren currentSiren;

        public MainForm()
        {
            InitializeComponent();
            InitFileTypes();


            settingsPage.Enabled = false;
            sirenPage.Enabled = false;
            fileListView.Enabled = false;

            editToolStripMenuItem.Enabled = false;
            toolsToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Enabled = false;

        }
        private void InitFileTypes()
        {
            FileTypes = new Dictionary<string, FileTypeInfo>();
            InitFileType(".rpf", "Rage Package File", 3);
            InitFileType("", "File", 4);
            InitFileType(".dat", "Data File", 4);
            InitFileType(".cab", "CAB File", 4);
            InitFileType(".txt", "Text File", 5, FileTypeAction.ViewText);
            InitFileType(".gxt2", "Global Text Table", 5, FileTypeAction.ViewGxt);
            InitFileType(".log", "LOG File", 5, FileTypeAction.ViewText);
            InitFileType(".ini", "Config Text", 5, FileTypeAction.ViewText);
            InitFileType(".vdf", "Steam Script File", 5, FileTypeAction.ViewText);
            InitFileType(".sps", "Shader Preset", 5, FileTypeAction.ViewText);
            InitFileType(".ugc", "User-Generated Content", 5, FileTypeAction.ViewText);
            InitFileType(".xml", "XML File", 6, FileTypeAction.ViewXml);
            InitFileType(".meta", "Metadata (XML)", 6, FileTypeAction.ViewXml);
            InitFileType(".ymt", "Metadata (Binary)", 6, FileTypeAction.ViewYmt, true);
            InitFileType(".pso", "Metadata (PSO)", 6, FileTypeAction.ViewJPso, true);
            InitFileType(".gfx", "Scaleform Flash", 7);
            InitFileType(".ynd", "Path Nodes", 8, FileTypeAction.ViewYnd, true);
            InitFileType(".ynv", "Nav Mesh", 9, FileTypeAction.ViewModel, true);
            InitFileType(".yvr", "Vehicle Record", 9, FileTypeAction.ViewYvr, true);
            InitFileType(".ywr", "Waypoint Record", 9, FileTypeAction.ViewYwr, true);
            InitFileType(".fxc", "Compiled Shaders", 9, FileTypeAction.ViewFxc, true);
            InitFileType(".yed", "Expression Dictionary", 9, FileTypeAction.ViewYed, true);
            InitFileType(".yld", "Cloth Dictionary", 9, FileTypeAction.ViewYld, true);
            InitFileType(".yfd", "Frame Filter Dictionary", 9, FileTypeAction.ViewYfd);
            InitFileType(".asi", "ASI Plugin", 9);
            InitFileType(".dll", "Dynamic Link Library", 9);
            InitFileType(".exe", "Executable", 10);
            InitFileType(".yft", "Fragment", 11, FileTypeAction.ViewModel, true);
            InitFileType(".ydr", "Drawable", 11, FileTypeAction.ViewModel, true);
            InitFileType(".ydd", "Drawable Dictionary", 12, FileTypeAction.ViewModel, true);
            InitFileType(".cut", "Cutscene", 12, FileTypeAction.ViewCut, true);
            InitFileType(".ysc", "Script", 13);
            InitFileType(".ymf", "Manifest", 14, FileTypeAction.ViewYmf, true);
            InitFileType(".bik", "Bink Video", 15);
            InitFileType(".jpg", "JPEG Image", 16);
            InitFileType(".jpeg", "JPEG Image", 16);
            InitFileType(".gif", "GIF Image", 16);
            InitFileType(".png", "Portable Network Graphics", 16);
            InitFileType(".dds", "DirectDraw Surface", 16);
            InitFileType(".ytd", "Texture Dictionary", 16, FileTypeAction.ViewYtd, true);
            InitFileType(".mrf", "Move Network File", 18, FileTypeAction.ViewMrf, true);
            InitFileType(".ycd", "Clip Dictionary", 18, FileTypeAction.ViewYcd, true);
            InitFileType(".ypt", "Particle Effect", 18, FileTypeAction.ViewModel, true);
            InitFileType(".ybn", "Static Collisions", 19, FileTypeAction.ViewModel, true);
            InitFileType(".ide", "Item Definitions", 20, FileTypeAction.ViewText);
            InitFileType(".ytyp", "Archetype Definitions", 20, FileTypeAction.ViewYtyp, true);
            InitFileType(".ymap", "Map Data", 21, FileTypeAction.ViewYmap, true);
            InitFileType(".ipl", "Item Placements", 21, FileTypeAction.ViewText);
            InitFileType(".awc", "Audio Wave Container", 22, FileTypeAction.ViewAwc, true);
            InitFileType(".rel", "Audio Data (REL)", 23, FileTypeAction.ViewRel, true);
            InitFileType(".nametable", "Name Table", 5, FileTypeAction.ViewNametable);
            InitFileType(".ypdb", "Pose Matcher Database", 9, FileTypeAction.ViewYpdb, true);
        }

        private void InitFileType(string ext, string name, int imgidx, FileTypeAction defaultAction = FileTypeAction.ViewHex, bool xmlConvertible = false)
        {
            var ft = new FileTypeInfo(ext, name, imgidx, defaultAction, xmlConvertible);
            FileTypes[ext] = ft;
        }


        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "AWC File (*.awc)|*.awc|All Files (*.*)|*.*";
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(ofd.FileName))
                {
                    awcPath = ofd.FileName;
                    LoadAWC();
                    fileListView.Enabled = true;
                }
                else
                {
                    MessageBox.Show("Invalid file selected!", "Error Reading File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoadAWC()
        {
            byte[] data = File.ReadAllBytes(awcPath);
            string name = new FileInfo(awcPath).Name;
            string path = awcPath;

            if (data == null) return;

            var fti = GetFileType(path);

            if (fti.DefaultAction != FileTypeAction.ViewAwc)
            {
                MessageBox.Show("This file is not an AWC file.", "Error Reading File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var fe = CreateFileEntry(name, path, ref data);

            awcFile = RpfFile.GetFile<AwcFile>(fe, data);

            if (awcFile.Streams != null)
            {
                var strlist = awcFile.Streams.ToList();
                strlist.Sort((a, b) => a.Name.CompareTo(b.Name));
                fileListView.Items.Clear();
                foreach (var audio in strlist)
                {
                    var stereo = (audio.ChannelStreams?.Length == 2);
                    if ((audio.StreamBlocks != null) && (!stereo)) continue;//don't display multichannel source audios
                    var audioName = audio.Name;
                    if (stereo) audioName = "(Stereo Playback)";
                    var item = fileListView.Items.Add(audioName);
                    item.SubItems.Add(audio.Type);
                    item.SubItems.Add(audio.LengthStr);
                    item.SubItems.Add(TextUtil.GetBytesReadable(audio.ByteLength));
                    item.Tag = audio;
                }
            }
        }

        public FileTypeInfo GetFileType(string fn)
        {
            var fi = new FileInfo(fn);
            var ext = fi.Extension.ToLowerInvariant();
            if (!string.IsNullOrEmpty(ext))
            {
                FileTypeInfo ft;
                if (FileTypes.TryGetValue(ext, out ft))
                {
                    if (ft.SubTypes != null)
                    {
                        var fnl = fn.ToLowerInvariant();
                        foreach (var sft in ft.SubTypes)
                        {
                            if (fnl.EndsWith(sft.Extension))
                            {
                                return sft;
                            }
                        }
                    }
                    return ft;
                }
                else
                {
                    ft = new FileTypeInfo(ext, ext.Substring(1).ToUpperInvariant() + " File", 4, FileTypeAction.ViewHex, false);
                    FileTypes[ft.Extension] = ft; //save it for later!
                    return ft;
                }
            }
            else
            {
                return FileTypes[""];
            }
        }

        private RpfFileEntry CreateFileEntry(string name, string path, ref byte[] data)
        {
            //this should only really be used when loading a file from the filesystem.
            RpfFileEntry e = null;
            uint rsc7 = (data?.Length > 4) ? BitConverter.ToUInt32(data, 0) : 0;
            if (rsc7 == 0x37435352) //RSC7 header present! create RpfResourceFileEntry and decompress data...
            {
                e = RpfFile.CreateResourceFileEntry(ref data, 0);//"version" should be loadable from the header in the data..
                data = ResourceBuilder.Decompress(data);
            }
            else
            {
                var be = new RpfBinaryFileEntry();
                be.FileSize = (uint)data?.Length;
                be.FileUncompressedSize = be.FileSize;
                e = be;
            }
            e.Name = name;
            e.NameLower = name?.ToLowerInvariant();
            e.NameHash = JenkHash.GenHash(e.NameLower);
            e.ShortNameHash = JenkHash.GenHash(Path.GetFileNameWithoutExtension(e.NameLower));
            e.Path = path;
            return e;
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            awcFile = null;
            awcPath = "";
            fileListView.Items.Clear();

            settingsPage.Enabled = true;
            sirenPage.Enabled = false;
            fileListView.Enabled = true;
            editToolStripMenuItem.Enabled = true;
            toolsToolStripMenuItem.Enabled = true;
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Wave File (*.wav)|*.wav";
            ofd.Multiselect = true;
            ofd.RestoreDirectory = true;
            ofd.Title = "Select Audio Files...";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                foreach (var file in ofd.FileNames)
                {
                    if (File.Exists(file))
                    {
                        var siren = new Siren(Path.GetFileNameWithoutExtension(file), file);
                        sirens.Add(siren);
                    }
                    else
                    {
                        MessageBox.Show("Invalid file selected!", "Error Reading File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            RefreshListView();
        }

        void RefreshListView()
        {
            fileListView.Items.Clear();

            foreach (var siren in sirens)
            {
                var item = fileListView.Items.Add(siren.SirenName);
                item.SubItems.Add($"{siren.SampleRate} Hz");
                item.SubItems.Add(siren.Length.ToString("mm\\:ss"));
                item.SubItems.Add(HumanReadableBytes(new FileInfo(siren.AudioPath).Length));
                item.Tag = siren;
            }
        }

        private string HumanReadableBytes(long len)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }

        private void fileListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (fileListView.SelectedItems.Count == 0)
            {
                currentSiren = null;
                sirenPage.Enabled = false;
                sirenNameTextBox.Text = "";
                sirenAudioPathTextBox.Text = "";
                return;
            }

            var siren = fileListView.SelectedItems[0].Tag as Siren;
            if (siren != null)
            {
                currentSiren = siren;
                sirenNameTextBox.Text = siren.SirenName;
                sirenAudioPathTextBox.Text = siren.AudioPath;

                sirenPage.Enabled = true;
            }
        }

        private void sirenSaveButton_Clicked(object sender, EventArgs e)
        {
            if (sirenNameTextBox.Text.Contains(" "))
            {
                MessageBox.Show("Siren Name can't contain spaces (use _ instead)", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            currentSiren.SirenName = sirenNameTextBox.Text;

            if (currentSiren.AudioPath != sirenAudioPathTextBox.Text)
            {
                currentSiren.AudioPath = sirenAudioPathTextBox.Text;
            }
            RefreshListView();


        }

        private void sirenAudioBrowseButton_Clicked(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Wave File (*.wav)|*.wav";
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(ofd.FileName))
                {
                    sirenAudioPathTextBox.Text = ofd.FileName;
                }
                else
                {
                    MessageBox.Show("Invalid file selected!", "Error Reading File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "AWC File (*.awc)|*.awc";
            sfd.RestoreDirectory = true;
            sfd.Title = "Save AWC file";
            var awcName = "custom_sounds";
            sfd.FileName = awcName + ".awc";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                if (Directory.Exists(Path.GetDirectoryName(sfd.FileName)))
                {
                    SirenBuilder.GenerateSirenAWC(awcName, "E:\\OpalRP\\txData\\CFXDefault_44E74A.base\\resources\\[debug]\\sirens\\dlc_serversideaudio\\oac\\police", sirens.ToArray(), sfd.FileName);
                }
                else
                {
                    MessageBox.Show("Invalid file selected!", "Error Reading File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void generateFiveMResourceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var folderPath = "";

            var cfd = new CommonOpenFileDialog();
            cfd.IsFolderPicker = true;

            if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                folderPath = cfd.FileName;
            }

            if (!Directory.Exists(folderPath))
            {
                MessageBox.Show("Invalid Directory selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (fivemResourceNameTextBox.Text.Contains(" "))
            {
                MessageBox.Show("FiveM Resource Name can't contain spaces (use - instead).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (dlcNameTextBox.Text.Contains(" "))
            {
                MessageBox.Show("DLC Name can't contain spaces (use _ instead).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (awcNameTextBox.Text.Contains(" "))
            {
                MessageBox.Show("AWC Name can't contain spaces (use _ instead).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (!FilesAreInSameDirectory(sirens.Select(x => x.AudioPath)))
            {
                MessageBox.Show("Audio files aren't in the same directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            SirenBuilder.GenerateFiveMResource(folderPath, fivemResourceNameTextBox.Text, awcNameTextBox.Text, dlcNameTextBox.Text, sirens.ToArray());
        }

        private bool FilesAreInSameDirectory(IEnumerable<string> filePaths)
        {
            var directories = filePaths.Select(x => Path.GetDirectoryName(x));
            return directories.Distinct().Count() == 1;
        }
    }
}
