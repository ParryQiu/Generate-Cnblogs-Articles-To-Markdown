using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Common.Helper;
using Html2Markdown;

namespace Generate_Cnblogs_Articles_To_Markdown_Files
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //Init
            if (!Directory.Exists(Application.StartupPath + "\\output\\"))
            {
                Directory.CreateDirectory(Application.StartupPath + "\\output\\");
            }

            if (!Directory.Exists(Application.StartupPath + "\\images\\"))
            {
                Directory.CreateDirectory(Application.StartupPath + "\\images\\");
            }

            CnblogsHelper.ExportToMarkdown(1, 4, true, "http://7xqdjc.com1.z0.glb.clouddn.com/blog_");

            Console.ReadKey();
        }
    }
}