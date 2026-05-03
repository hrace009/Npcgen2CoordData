using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Npcgen2CoordData
{
    public delegate void SetProgressMax(int value);
    public delegate void SetProgressNext();
    public delegate void SetProgressValue(int value);
    public delegate void SetProgressText(string value);

    public partial class Form1 : Form
    {
        NpcGen npcgen = new NpcGen();
        CoordData coord = new CoordData();

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            npcgen.ProgressMax += ProgressMax;
            npcgen.ProgressNext += ProgressNext;
            npcgen.ProgressText += ProgressText;
            npcgen.ProgressValue += ProgressValue;
            coord.ProgressMax += ProgressMax;
            coord.ProgressNext += ProgressNext;
            coord.ProgressText += ProgressText;
            coord.ProgressValue += ProgressValue;
        }

        private void ProgressValue(int value)
        {
            Progress.Value = value;
        }

        private void ProgressText(string value)
        {
            Status.Text = value;
        }

        private void ProgressNext()
        {
            ++Progress.Value;
        }

        private void ProgressMax(int value)
        {
            Progress.Maximum = value;
        }

        private void load_coord_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = "Coord_data|*.txt|All Files|*.*"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                new Thread(() => coord.Read(ofd.FileName)).Start();
            }
        }

        private void save_coord_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "Coord_data|*.txt|All Files|*.*"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                new Thread(() => coord.Save(sfd.FileName)).Start();
            }
        }

        private void import_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog()
            {
                Description = "Select the server maps root folder (containing map subfolders with npcgen.data)",
                ShowNewFolderButton = false
            };
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                string root = fbd.SelectedPath;
                new Thread(() =>
                {
                    string[] mapDirs = Directory.GetDirectories(root);
                    List<string> targets = new List<string>();
                    foreach (string dir in mapDirs)
                    {
                        string candidate = Path.Combine(dir, "npcgen.data");
                        if (File.Exists(candidate))
                        {
                            targets.Add(dir);
                        }
                    }

                    if (targets.Count == 0)
                    {
                        ProgressMax(1);
                        ProgressValue(0);
                        ProgressText("No npcgen.data found in subfolders");
                        return;
                    }

                    List<int> cleared = new List<int>();
                    List<string> skipped = new List<string>();
                    int imported = 0;
                    ProgressMax(targets.Count);
                    ProgressValue(0);

                    foreach (string dir in targets)
                    {
                        string map = Path.GetFileName(dir);
                        string file = Path.Combine(dir, "npcgen.data");
                        ProgressText("Importing " + map);

                        NpcGen ng = new NpcGen();
                        ng.ProgressMax += v => { };
                        ng.ProgressNext += () => { };
                        ng.ProgressText += ProgressText;
                        ng.ProgressValue += v => { };

                        try
                        {
                            FileInfo fi = new FileInfo(file);
                            if (fi.Length < 16)
                            {
                                skipped.Add(map + " (empty/too small)");
                                ProgressNext();
                                continue;
                            }

                            using (BinaryReader br = new BinaryReader(File.OpenRead(file)))
                            {
                                ng.ReadNpcgen(br);
                            }
                        }
                        catch (EndOfStreamException)
                        {
                            skipped.Add(map + " (truncated)");
                            ProgressNext();
                            continue;
                        }
                        catch (Exception ex)
                        {
                            skipped.Add(map + " (" + ex.GetType().Name + ")");
                            ProgressNext();
                            continue;
                        }

                        imported++;

                        ng.NpcMobList.ForEach(x =>
                        {
                            x.MobDops.ForEach(y =>
                            {
                                if (coord.Entrys.ContainsKey(y.Id.ToString()))
                                {
                                    if (!cleared.Contains(y.Id))
                                    {
                                        coord.Entrys[y.Id.ToString()].Clear();
                                        cleared.Add(y.Id);
                                    }
                                    coord.Entrys[y.Id.ToString()].Add(new CoordDataEntry()
                                    {
                                        MapNumber = map,
                                        X = x.X_position,
                                        Y = x.Y_position,
                                        Z = x.Z_position
                                    });
                                }
                                else
                                {
                                    cleared.Add(y.Id);
                                    coord.Entrys[y.Id.ToString()] = new List<CoordDataEntry>
                                    {
                                        new CoordDataEntry()
                                        {
                                            MapNumber = map,
                                            X = x.X_position,
                                            Y = x.Y_position,
                                            Z = x.Z_position
                                        }
                                    };
                                }
                            });
                        });
                        ng.ResourcesList.ForEach(x =>
                        {
                            x.ResExtra.ForEach(y =>
                            {
                                if (coord.Entrys.ContainsKey(y.Id.ToString()))
                                {
                                    if (!cleared.Contains(y.Id))
                                    {
                                        coord.Entrys[y.Id.ToString()].Clear();
                                        cleared.Add(y.Id);
                                    }
                                    coord.Entrys[y.Id.ToString()].Add(new CoordDataEntry()
                                    {
                                        MapNumber = map,
                                        X = x.X_position,
                                        Y = x.Y_position,
                                        Z = x.Z_position
                                    });
                                }
                                else
                                {
                                    cleared.Add(y.Id);
                                    coord.Entrys[y.Id.ToString()] = new List<CoordDataEntry>
                                    {
                                        new CoordDataEntry()
                                        {
                                            MapNumber = map,
                                            X = x.X_position,
                                            Y = x.Y_position,
                                            Z = x.Z_position
                                        }
                                    };
                                }
                            });
                        });

                        ProgressNext();
                    }

                    ProgressValue(0);
                    string summary = "Done. Imported " + imported + "/" + targets.Count + " map(s).";
                    if (skipped.Count > 0)
                    {
                        summary += " Skipped: " + string.Join(", ", skipped.ToArray());
                    }
                    ProgressText(summary);
                }).Start();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
