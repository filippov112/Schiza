using Schiza.Domain.FileSystem;
using Schiza.Windows.Project.Explorer.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Schiza.Services
{
    public class FileSystemService: IFileSystemService
    {

        public void Move(string old_path, string new_path)
        {
            old_path = old_path.Trim().ToLower();
            new_path = new_path.Trim().ToLower();

            if (IsParentAndChild(old_path, new_path))
                return;
            if (old_path == new_path) 
                return;

            // Проверка на существование файла/папки с таким именем в новом месте
            int index = 1;
            string extension = Path.GetExtension(new_path);
            string directory = Path.GetDirectoryName(new_path) ?? "";
            string filename = Path.GetFileName(new_path).Split(extension)[0];
            while (File.Exists(new_path) || Directory.Exists(new_path))
            {
                new_path = $"{directory}/{filename}({index}){extension}";
                index++;
            }

            try
            {
                if (Directory.Exists(old_path))
                {
                    Directory.Move(old_path, new_path);
                }
                else
                {
                    File.Move(old_path, new_path);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при перемещении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // Вспомогательный метод для проверки, является ли 'parent' родительским для 'child'
        private bool IsParentAndChild(string parent, string child)
        {
            var parentInfo = new DirectoryInfo(parent);
            var childInfo = new DirectoryInfo(child);
            while (childInfo.Parent != null)
            {
                if (childInfo.Parent.FullName.Equals(parentInfo.FullName, StringComparison.OrdinalIgnoreCase))
                    return true;

                childInfo = childInfo.Parent;
            }
            return false;
        }
    }
}
