using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace GTS_Scene_Creator
{
    public partial class SceneEditorForm : Form
    {
        private const string FileDialogueFilter = "Space files (*.space)|*.space";

        private string currentPath = string.Empty;

        public SceneInfo sceneInfo = new SceneInfo();

        public SceneEditorForm()
        {
            InitializeComponent();

            sceneProperties.SelectedObject = sceneInfo;
        }

        public void Deserialize(string path)
        {
            currentPath = path;

            try
            {
                clearAllToolStripMenuItem_Click(null, new EventArgs());

                var r = new StreamReader(currentPath);

                var s = new XmlSerializer(typeof(SceneInfo));

                sceneInfo = (SceneInfo) s.Deserialize(r);

                r.Close();

                statusLabel.Text = "Loaded: " + path;

                sceneProperties.SelectedObject = sceneInfo;

                sceneProperties.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void clearAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sceneInfo = new SceneInfo();

            sceneProperties.SelectedObject = sceneInfo;

            sceneProperties.Refresh();

            statusLabel.Text = "Reinitialized Data Source...";
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var s = new SaveFileDialog
            {
                Filter = FileDialogueFilter,
                FileName = currentPath,
                DefaultExt = "space"
            };

            if (s.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    var stream = new FileStream(s.FileName, FileMode.Create);
                    var serializer = new XmlSerializer(typeof(SceneInfo));
                    serializer.Serialize(stream, sceneInfo);
                    stream.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
                }

                statusLabel.Text = "Saved: " + s.FileName;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var o = new OpenFileDialog
            {
                Filter = FileDialogueFilter,
                FileName = currentPath,
                DefaultExt = "space"
            };

            if (o.ShowDialog(this) == DialogResult.OK)
            {
                sceneInfo = new SceneInfo();
                Deserialize(o.FileName);
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clearAllToolStripMenuItem_Click(null, new EventArgs());
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}