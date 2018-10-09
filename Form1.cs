using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace autoupdateERP
{
    public partial class Form1 : Form
    {

        string rootPath;

        public Form1()
        {
            InitializeComponent();
        }


       



        public void DownloadFile(string URL, string filename,
            System.Windows.Forms.ProgressBar prog = null,
            System.Windows.Forms.Label label1 = null)
        {
            float percent = 0;
            try
            {
                System.Net.HttpWebRequest Myrq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(URL);
                System.Net.HttpWebResponse myrp = (System.Net.HttpWebResponse)Myrq.GetResponse();
                long totalBytes = myrp.ContentLength;
                if (prog != null)
                {
                    prog.Maximum = (int)totalBytes;
                }
                System.IO.Stream st = myrp.GetResponseStream();
                System.IO.Stream so = new System.IO.FileStream(filename, System.IO.FileMode.Create);
                long totalDownloadedByte = 0;
                byte[] by = new byte[1024];
                int osize = st.Read(by, 0, (int)by.Length);
                while (osize > 0)
                {
                    totalDownloadedByte = osize + totalDownloadedByte;
                    System.Windows.Forms.Application.DoEvents();
                    so.Write(by, 0, osize);
                    if (prog != null)
                    {
                        prog.Value = (int)totalDownloadedByte;
                    }
                    osize = st.Read(by, 0, (int)by.Length);

                    percent = (float)totalDownloadedByte / (float)totalBytes * 100;
                    if (label1 != null)
                    {
                        label1.Text = String.Format("当前文件({0})\n下载进度",
                            System.IO.Path.GetFileName(filename)) + percent.ToString("F2") + "%";
                    }
                    System.Windows.Forms.Application.DoEvents(); //必须加注这句代码，否则label1将因为循环执行太快而来不及显示信息
                }
                so.Close();
                st.Close();
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        string getRemoteVersion()
        {
            String filepath = @"r_version.txt";
            DownloadFile(rootPath + @"version.txt", filepath);
            return getLocalVersion(filepath);
        }

        string getLocalVersion(String filepath = @"version.txt")
        {
            using (System.IO.StreamReader sr = new System.IO.StreamReader(filepath))
            {
                return sr.ReadLine();
            }
        }


        string[] readLines()
        {
            using (System.IO.StreamReader sr = new System.IO.StreamReader(@"filelist.txt"))
            {
                List<string> lst = new List<string>();
                string s;
                while ((s = sr.ReadLine()) != null)
                {
                    lst.Add(s);
                }
                return lst.ToArray();
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        void start()
        {
            // 获取文件列表
            DownloadFile(rootPath + @"filelist.txt", @"filelist.txt");
            string[] files = readLines();
            int num_files = files.Length;
            progressBar总.Maximum = num_files;
            string url;
            string filepath;
            for (int i = 0; i < num_files; ++i)
            {
                progressBar总.Value = i + 1;
                label总.Text = string.Format("总进度:{0}/{1}", i + 1, num_files);
                url = files[i].Split('\t')[0].Replace("#", "%23");
                url = System.Web.HttpUtility.UrlPathEncode(
                    rootPath + url);
                filepath = files[i].Split('\t')[1];
                DownloadFile(url, filepath, progressBar文件, label文件);

                System.Console.WriteLine(url);
            }
            // 更新当前版本号
            System.IO.File.Delete(@"version.txt");
            System.IO.File.Move(@"r_version.txt", @"version.txt");
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                start();
                MessageBox.Show("更新完毕");
                Application.Exit();
            }
            catch (Exception ee)
            {
                MessageBox.Show("更新失败，请发送AutoUpdate.log文件的内容给管理员");
                System.IO.File.WriteAllText("AutoUpdate.log", ee.Message + "\n" + ee.StackTrace);
                Application.Exit();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 获取版本信息
            readRootFromConfig();
            string localVersion = getLocalVersion();
            string remoteVersion = getRemoteVersion();
            if (string.Compare(remoteVersion, localVersion) > 0)
            {
                // 确认是否更新
                if (DialogResult.OK ==
                    MessageBox.Show(String.Format("检测到新版本: {0}(当前版本{1})，是否更新？",
                    remoteVersion, localVersion), "提示", MessageBoxButtons.OKCancel))
                {
                }
                else
                {
                    Application.Exit();
                }
            }
            else
            {
                MessageBox.Show("已经是最新版本");
                Application.Exit();
            }
            
        }

        private void readRootFromConfig()
        {
            string file = System.Windows.Forms.Application.ExecutablePath;
            Configuration config = ConfigurationManager.OpenExeConfiguration(file);

            ConfigurationManager.RefreshSection("appSettings");
            
            try
            {
                rootPath = config.AppSettings.Settings["root"].Value;
            }
            catch (Exception ee)
            {
                rootPath = @"http://123.206.90.229/erp/";
                config.AppSettings.Settings.Add("root", rootPath);
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
        }

        

        

    }
}
