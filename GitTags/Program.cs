using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Windows.Forms;

namespace GitTags
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            FolderBrowserDialog repoDialog = new FolderBrowserDialog();
            repoDialog.Description = "Выберите папку с репозиторием";
            DialogResult result = repoDialog.ShowDialog();
            if (result != DialogResult.OK)
            { MessageBox.Show("Папка не выбрана."); Environment.Exit(0); }

            string repoPath = repoDialog.SelectedPath.Replace(@"\", @"/");

            OpenFileDialog gitDialog = new OpenFileDialog();
            gitDialog.Title = "Укажите исполняемый файл git cmd";
            gitDialog.AddExtension = true;
            DialogResult gitresult = gitDialog.ShowDialog();
            if (gitresult != DialogResult.OK)
            { MessageBox.Show("Исполняемый файл не выбран."); Environment.Exit(0); }

            string gitFile = gitDialog.FileName;


            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.FileName = gitFile ;
            startInfo.Arguments = @" --login -i";
            startInfo.WorkingDirectory = repoPath;

            string cdCommand = @"git cd " + repoPath;
            string tagCommand = "git tag";
            string showCommand = "git show -s --format=\"%cD\" ";
            Process process = new Process();
            process.StartInfo = startInfo;

            //запускаем
            try
            {process.Start();}
            catch (Exception ex)
            { MessageBox.Show("Git cmd не может быть запущен.\n" + ex.Message); Environment.Exit(0); }

            using (StreamWriter inputWriter = process.StandardInput)
            {
                inputWriter.WriteLine(cdCommand);
            }
            string[] welcomeOutput = process.StandardOutput.ReadToEnd()
                .Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            process.WaitForExit();

            //получаем тэги
            process.Start();
            using (StreamWriter inputWriter = process.StandardInput)
            {
                inputWriter.WriteLine(tagCommand);
            }

            string output = process.StandardOutput.ReadToEnd();
            string[] tagsOutput = output
                .Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                MessageBox.Show("Ошибка при получении тэгов.\n");
                Environment.Exit(0);
            }


            bool noTagFound = true;
            XmlDocument tagsXML = new XmlDocument();
            XmlNode root = tagsXML.CreateNode(XmlNodeType.Element, "root", "");
            tagsXML.AppendChild(root);

            foreach (string line in tagsOutput)
            {
                //получаем коммиты по меткам
                process.Start();
                using (StreamWriter inputWriter = process.StandardInput)
                {inputWriter.WriteLine(showCommand + line);}
                string[] commitOutput = process.StandardOutput.ReadToEnd()
                    .Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                process.WaitForExit();

                if (process.ExitCode == 0) // ошибка будет если вместо тэга попала другая строка
                {
                    noTagFound = false;
                    string date = commitOutput[commitOutput.Length - 1];
                    //добавляем в XML
                    XmlNode xmltag = tagsXML.CreateNode(XmlNodeType.Element, "tag", "");

                    XmlAttribute nameAttribute = tagsXML.CreateAttribute("name");
                    nameAttribute.Value = line;
                    xmltag.Attributes.Append(nameAttribute);

                    XmlAttribute dateAttribute = tagsXML.CreateAttribute("date");
                    dateAttribute.Value = date;
                    xmltag.Attributes.Append(dateAttribute);

                    root.AppendChild(xmltag);
                }
            }

            if (noTagFound)
            {
                MessageBox.Show("В репозитории нет тегов.\n");
                Environment.Exit(0);
            }

            process.Close();
            try
            {
                tagsXML.Save(repoPath + @"\..\tags.xml");
            }
            catch (Exception ex)
            { MessageBox.Show("Не удалось сохранить файл xml.\n" + ex.Message); }
            
        }
    }
}
