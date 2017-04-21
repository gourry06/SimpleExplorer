using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileExplorer
{
    public partial class Form1 : Form
    {
        string m_curPath = "";
        Thread m_thread;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            m_curPath = "Root";
            label1.Text = m_curPath;

            TreeNode root = treeView1.Nodes.Add(m_curPath);

            string[] drives = Directory.GetLogicalDrives();

            foreach (string drive in drives)
            {
                DriveInfo di = new DriveInfo(drive);

                if (di.IsReady)
                {
                    TreeNode node = root.Nodes.Add(drive);
                    node.Nodes.Add("\\");
                }
            }
        }

        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            try
            {
                if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text.Equals("\\"))
                {
                    e.Node.Nodes.Clear();

                    string path = e.Node.FullPath.Substring(e.Node.FullPath.IndexOf("\\") + 1);

                    string[] directories = Directory.GetDirectories(path);
                    foreach (string directory in directories)
                    {
                        TreeNode newNode = e.Node.Nodes.Add(directory.Substring(directory.LastIndexOf("\\") + 1));
                        newNode.Nodes.Add("\\" );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("treeView1_BeforeExpand : " + ex.Message);
            }
        }

        private void ViewDirectoryList(string path)
        {
            if (m_thread != null && m_thread.IsAlive)
                m_thread.Abort();

            string curPath = path;

            Console.WriteLine(path.IndexOf("Root\\"));
            if (path.IndexOf("Root\\") == 0)
            {
                curPath = path.Substring(path.IndexOf("\\") + 1);
                label1.Text = (curPath.Length > 4) ? curPath.Remove(curPath.IndexOf("\\") + 1, 1) : curPath;
                m_curPath = label1.Text;
            }
            else
            {
                label1.Text = path;
                m_curPath = path;
            }

            try
            {
                listView1.Items.Clear();

                string[] directories = Directory.GetDirectories(curPath);

                foreach (string directory in directories)
                {
                    DirectoryInfo info = new DirectoryInfo(directory);
                    ListViewItem item = new ListViewItem(new string[]
                    {
                        info.Name, info.LastWriteTime.ToString(), "파일 폴더", ""
                    });
                    listView1.Items.Add(item);
                }

                string[] files = Directory.GetFiles(curPath);

                foreach (string file in files)
                {
                    FileInfo info = new FileInfo(file);
                    ListViewItem item = new ListViewItem(new string[]
                    {
                        info.Name, info.LastWriteTime.ToString(), info.Extension, ((info.Length/1000)+1).ToString()+"KB"
                    });
                    listView1.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ViewDirectoryList : " + ex.Message);
            }
        }

        private void SelectTreeView(TreeNode node)
        {
            if (node.FullPath == null)
            {
                Console.WriteLine("empth node.FullPath");
                return;
            }

            string path = node.FullPath;

            ViewDirectoryList(path);
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SelectTreeView(e.Node);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                m_curPath = fbd.SelectedPath;
                Console.WriteLine(m_curPath);
                label1.Text = m_curPath;

                ViewDirectoryList(m_curPath);
            }
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                string processPath;
                if (listView1.SelectedItems[0].Text.IndexOf("\\") > 0)
                    processPath = listView1.SelectedItems[0].Text;
                else
                    processPath = m_curPath + "\\" + listView1.SelectedItems[0].Text;

                Process.Start("explorer.exe", processPath);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(m_thread != null && m_thread.IsAlive)
                m_thread.Abort();

            m_curPath = label1.Text;
            DirectoryInfo rootDirInfo = new DirectoryInfo(m_curPath);
            string searchFiles = textBox1.Text;

            m_thread = new Thread(delegate ()
            {
                WalkDirectoryTree(rootDirInfo, searchFiles);
            });
            m_thread.Start();
        }

        public void WalkDirectoryTree(DirectoryInfo dirInfo, string searchFiles)
        {
            listView1.Items.Find(searchFiles, true);

            FileInfo[] files = null;
            DirectoryInfo[] subDirs = null;
           
            try
            {
                files = dirInfo.GetFiles(searchFiles + "*.*");
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            if (files != null)
            {
                DirectoryInfo tempDirInfo = new DirectoryInfo(m_curPath);

                if (dirInfo.ToString() == tempDirInfo.ToString())
                    listView1.Items.Clear();

                foreach (FileInfo fi in files)
                {
                    ListViewItem item = new ListViewItem(new string[]
                    {
                        fi.FullName, fi.LastWriteTime.ToString(), fi.Extension, ((fi.Length/1000)+1).ToString()+"KB"
                    });
                    listView1.Items.Add(item);
                }

                subDirs = dirInfo.GetDirectories();
                foreach (DirectoryInfo di in subDirs)
                {
                    WalkDirectoryTree(di, searchFiles);
                }
            }
        }
    }
}
