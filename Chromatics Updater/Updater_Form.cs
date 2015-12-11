using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chromatics_Updater
{
    public partial class Updater_Form : Form
    {
        public Updater_Form()
        {
            InitializeComponent();
            ExecuteUpdate();
        }

        void ExecuteUpdate()
        {
            string[] args = Environment.GetCommandLineArgs();
            string updatedFile = args[1];

            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 8.0)");
                client.DownloadFileCompleted += new AsyncCompletedEventHandler((sender, e) => Completed(sender, e, updatedFile));
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler((sender, e) => ProgressChanged(sender, e));
                client.DownloadFileAsync(new Uri("http://thejourneynetwork.net/chromatics/update/Chromatics.zip"), updatedFile + @"\Chromatics.zip");
            }

            
        }


        private void prog_Download_Click(object sender, EventArgs e)
        {

        }

        private void Completed(object sender, AsyncCompletedEventArgs e, string updatedFile)
        {
            FileInfo destFile = new FileInfo(Path.Combine(updatedFile, @"\Chromatics.zip"));

            if (destFile.Extension.ToLower() == ".zip")
            {
                lbl_data.Text = "Extracting Update..";

                ZipArchive zipArchive = ZipFile.OpenRead(updatedFile + @"\Chromatics.zip");

                foreach (ZipArchiveEntry entry in zipArchive.Entries)
                {
                    string fullPath = Path.Combine(updatedFile + @"\", entry.FullName);
                    if (String.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(fullPath);
                    }
                    else
                    {
                        if (!entry.Name.Equals("updater.exe"))
                        {
                            entry.ExtractToFile(fullPath, true);
                        }
                    }
                }
                
                zipArchive.Dispose();
                //File.Delete(updatedFile + @"\Chromatics.zip");
                lbl_data.Text = "Closing Updater";
                if (File.Exists(@"C:\Program Files (x86)\Advanced Combat Tracker\Advanced Combat Tracker.exe"))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = @"C:\Program Files (x86)\Advanced Combat Tracker\Advanced Combat Tracker.exe";
                    Process.Start(startInfo);
                }
                System.Windows.Forms.Application.Exit();
            }
            else
            {
                lbl_data.Text = "The downloaded file did not contain a valid Chromatics file (Unknown file type)";
            }


        }

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            prog_Download.Value = e.ProgressPercentage;
            lbl_data.Text = "Downloading Update: " + e.ProgressPercentage + "% Complete";
        }
    }

}
