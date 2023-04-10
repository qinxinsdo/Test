using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ET
{
	public static class FileHelper
	{
		public static void GetAllFiles(List<string> files, string dir)
		{
			string[] fls = Directory.GetFiles(dir);
			foreach (string fl in fls)
			{
				files.Add(fl);
			}

			string[] subDirs = Directory.GetDirectories(dir);
			foreach (string subDir in subDirs)
			{
				GetAllFiles(files, subDir);
			}
		}
		
		public static void CleanDirectory(string dir)
		{
			foreach (string subdir in Directory.GetDirectories(dir))
			{
				Directory.Delete(subdir, true);		
			}

			foreach (string subFile in Directory.GetFiles(dir))
			{
				File.Delete(subFile);
			}
		}

		public static void CopyDirectory(string srcDir, string tgtDir)
		{
			DirectoryInfo source = new DirectoryInfo(srcDir);
			DirectoryInfo target = new DirectoryInfo(tgtDir);
	
			if (target.FullName.StartsWith(source.FullName, StringComparison.CurrentCultureIgnoreCase))
			{
				throw new Exception("父目录不能拷贝到子目录！");
			}
	
			if (!source.Exists)
			{
				return;
			}
	
			if (!target.Exists)
			{
				target.Create();
			}
	
			FileInfo[] files = source.GetFiles();
	
			for (int i = 0; i < files.Length; i++)
			{
				File.Copy(files[i].FullName, Path.Combine(target.FullName, files[i].Name), true);
			}
	
			DirectoryInfo[] dirs = source.GetDirectories();
	
			for (int j = 0; j < dirs.Length; j++)
			{
				CopyDirectory(dirs[j].FullName, Path.Combine(target.FullName, dirs[j].Name));
			}
		}
		
		public static void ReplaceExtensionName(string srcDir, string extensionName, string newExtensionName)
		{
			if (Directory.Exists(srcDir))
			{
				string[] fls = Directory.GetFiles(srcDir);

				foreach (string fl in fls)
				{
					if (fl.EndsWith(extensionName))
					{
						File.Move(fl, fl.Substring(0, fl.IndexOf(extensionName)) + newExtensionName);
						File.Delete(fl);
					}
				}

				string[] subDirs = Directory.GetDirectories(srcDir);

				foreach (string subDir in subDirs)
				{
					ReplaceExtensionName(subDir, extensionName, newExtensionName);
				}
			}
		}
		
		// public static bool CopyFile(string sourcePath, string targetPath, bool overwrite)
		// {
		// 	string sourceText = null;
		// 	string targetText = null;
		//
		// 	if (File.Exists(sourcePath))
		// 	{
		// 		sourceText = File.ReadAllText(sourcePath);
		// 	}
		//
		// 	if (File.Exists(targetPath))
		// 	{
		// 		targetText = File.ReadAllText(targetPath);
		// 	}
		//
		// 	if (sourceText != targetText && File.Exists(sourcePath))
		// 	{
		// 		File.Copy(sourcePath, targetPath, overwrite);
		// 		return true;
		// 	}
		//
		// 	return false;
		// }
		
		/// <summary>
        /// 改变目录的可读属性
        /// </summary>
        /// <param name="path"></param>
        /// <param name="readable"></param>
        public static void MakeDirReadable(string path, bool readable)
        {
            DirectoryInfo info = new DirectoryInfo(path);
            if(readable)
            {
                info.Attributes |= FileAttributes.ReadOnly;
            }
            else
            {
                info.Attributes &= ~FileAttributes.ReadOnly;
            }
        }

        /// <summary>
        /// 改变文件的可读属性
        /// </summary>
        /// <param name="path"></param>
        /// <param name="readable"></param>
        public static void MakeFileReadable(string path, bool readable)
        {
            FileInfo info = new FileInfo(path);
            if(readable)
            {
                info.Attributes |= FileAttributes.ReadOnly;
            }
            else
            {
                info.Attributes &= ~FileAttributes.ReadOnly;
            }
            info.Attributes &= ~FileAttributes.Hidden;
            info.Attributes &= ~FileAttributes.NotContentIndexed;
        }

        /// <summary>
        /// 获取父路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetPatentPath(string path)
        {
            if(string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            string newPath = path;

            newPath = newPath.Replace("\\", "/");
            int index = newPath.LastIndexOf("/");
            if(index > -1)
            {
                newPath = newPath.Substring(0, index);
            }

            return newPath;
        }

        /// <summary>
        /// 文件夹是否存在
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        /// 创建目录
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool CreateDirectory(string path)
        {
            if (Directory.Exists(path))
                return true;

            try
            {
                Directory.CreateDirectory(path);
            }
            catch(Exception e)
            {
                Debug.LogError("CreateDirectory: " + path);
                Debug.LogError("FileHelper.CreateDirectory Error: " + e.ToString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// 根据文件创建目录
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool CreateFileDirectory(string path)
        {
            int index = path.LastIndexOf("/");
            string folder = path.Substring(0, index);

            return CreateDirectory(folder);
        }

        /// <summary>
        /// 删除目录
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool DeleteDirectory(string path, bool isForce = false)
        {
            if (!Directory.Exists(path))
                return true;

            try
            {
                Directory.Delete(path, isForce);
            }
            catch(Exception e)
            {
                Debug.LogError("FileHelper.DeleteDirectory Error: " + e.ToString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// 文件是否存在
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool FileExists(string path)
        {
            return File.Exists(path);
        }

        /// <summary>
        /// 读取字节数组
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static byte[] ReadFile(string path)
        {
            if(FileExists(path))
            {
                byte[] bytes = null;
                try
                {
                    bytes = File.ReadAllBytes(path);
                }
                catch(Exception e)
                {
                    Debug.LogError("FileHelper.ReadFile Error: " + e.ToString());
                }

                return bytes;
            }

            return null;
        }

        /// <summary>
        /// 读取文本
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ReadText(string path)
        {
            if(FileExists(path))
            {
                string text = null;
                try
                {
                    text = File.ReadAllText(path);
                }
                catch(Exception e)
                {
                    Debug.LogError("FileHelper.ReadText Error: " + e.ToString());
                }

                return text;
            }

            return null;
        }

        /// <summary>
        /// 读取文本行
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string[] ReadLines(string path)
        {
            if(FileExists(path))
            {
                string[] lines = null;
                try
                {
                    lines = File.ReadAllLines(path);
                }
                catch(Exception e)
                {
                    Debug.LogError("FileHelper.ReadLines Error: " + e.ToString());
                }

                return lines;
            }

            return null;
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="folder"></param>
        /// <param name="name"></param>
        /// <param name="bytes"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        public static bool WriteFile(string path, string folder, string name, byte[] bytes, bool overwrite)
        {
            if (bytes == null)
                return false;

            if (overwrite == false && File.Exists(path))
                return false;

            if (!CreateDirectory(folder))
                return false;

            try
            {
                using(FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush();
                    fs.Close();
                }
            }
            catch(Exception e)
            {
                Debug.LogError("FileHelper.WriteFile Error: " + e.ToString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bytes"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        public static bool WriteFile(string path, byte[] bytes, bool overwrite)
        {
            if (bytes == null)
                return false;

            int index = path.LastIndexOf("/");
            string name = path.Substring(index + 1, path.Length - index - 1);
            string folder = path.Substring(0, index);

            return WriteFile(path, folder, name, bytes, overwrite);
        }

        /// <summary>
        /// 写入文本
        /// </summary>
        /// <param name="path"></param>
        /// <param name="folder"></param>
        /// <param name="name"></param>
        /// <param name="text"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        public static bool WriteText(string path, string folder, string name, string text, bool overwrite)
        {
            if (text == null)
                return false;

            byte[] bytes = Encoding.UTF8.GetBytes(text);
            return WriteFile(path, folder, name, bytes, overwrite);
        }

        /// <summary>
        /// 写入文本
        /// </summary>
        /// <param name="path"></param>
        /// <param name="text"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        public static bool WriteText(string path, string text, bool overwrite)
        {
            if (text == null)
                return false;

            byte[] bytes = Encoding.UTF8.GetBytes(text);
            return WriteFile(path, bytes, overwrite);
        }

        /// <summary>
        /// 写入文件(是否追加)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="text"></param>
        /// <param name="append"></param>
        /// <returns></returns>
        public static void WriteTextAppend(string path, string text, bool append)
        {
            if (text == null)
                return;

            StreamWriter streamWriter = new StreamWriter(path, append);
            streamWriter.WriteLine(text);
            streamWriter.Close();
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool DeleteFile(string path)
        {
            if (!File.Exists(path))
                return false;

            try
            {
                File.Delete(path);
            }
            catch(Exception e)
            {
                Debug.LogError("FileHelper.DeleteFile Error: " + e.ToString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern"></param>
        public static void DeleteFiles(string path, string pattern)
        {
            if (!Directory.Exists(path))
                return;

            string[] files = Directory.GetFiles(path, pattern, SearchOption.AllDirectories);
            foreach(var file in files)
            {
                File.Delete(file);
            }
        }

        /// <summary>
        /// 移动文件
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destPath"></param>
        /// <param name="destFolder"></param>
        /// <returns></returns>
        public static bool MoveFile(string sourcePath, string destPath, string destFolder)
        {
            if(!File.Exists(sourcePath))
                return false;

            if (File.Exists(destPath))
                return false;

            if (!CreateDirectory(destFolder))
                return false;

            try
            {
                File.Move(sourcePath, destPath);
            }
            catch(Exception e)
            {
                Debug.LogError("FileHelper.MoveFile Error: " + e.ToString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// 移动文件
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destPath"></param>
        /// <returns></returns>
        public static bool MoveFile(string sourcePath, string destPath)
        {
            int index = destPath.LastIndexOf("/");
            string folder = destPath.Substring(0, index);
            return MoveFile(sourcePath, destPath, folder);
        }

        /// <summary>
        /// 拷贝文件
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destPath"></param>
        /// <param name="destFolder"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        public static bool CopyFile(string sourcePath, string destPath, string destFolder, bool overwrite)
        {
            if (!File.Exists(sourcePath))
                return false;

            if (File.Exists(destPath))
                return false;

            if (!CreateDirectory(destFolder))
                return false;

            try
            {
                File.Copy(sourcePath, destPath, overwrite);
            }
            catch(Exception e)
            {
                Debug.LogError("FileHelper.CopyFile Error: " + e.ToString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// 拷贝文件
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destPath"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        public static bool CopyFile(string sourcePath, string destPath, bool overwrite)
        {
            int index = destPath.LastIndexOf("/");
            string folder = destPath.Substring(0, index);
            return CopyFile(sourcePath, destPath, folder, overwrite);
        }

        /// <summary>
        /// 拷贝目录（文件后缀过滤模式）
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destPath"></param>
        /// <param name="filterPatterns"></param>
        public static void CopyDirectory(string sourcePath, string destPath, string filterPatterns = null)
        {
            if (!DirectoryExists(sourcePath))
            {
                Debug.LogWarning("Source path not exist: " + sourcePath);
                return;
            }

            CreateDirectory(destPath);

            string[] filters = filterPatterns != null ? filterPatterns.Split('|') : null;

            // file
            string[] files = Directory.GetFiles(sourcePath);
            foreach(string filePath in files)
            {
                if(filters != null)
                {
                    FileInfo fi = new FileInfo(filePath);
                    int index = Array.FindIndex<string>(filters, (string tobj) =>
                    {
                        return tobj.Contains(fi.Extension);
                    });

                    if (index != -1)
                        continue;
                }

                CopyFile(filePath, destPath + "/" + Path.GetFileName(filePath), true);
            }

            // folder
            string[] folders = Directory.GetDirectories(sourcePath);
            foreach(string folderPath in folders)
            {
                CopyDirectory(folderPath, destPath + "/" + Path.GetFileName(folderPath), filterPatterns);
            }
        }

        /// <summary>
        /// 移动文件夹
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destPath"></param>
        public static void MoveDirectory(string sourcePath, string destPath)
        {
            if(Directory.Exists(sourcePath))
            {
                Directory.Move(sourcePath, destPath);
            }
        }

        /// <summary>
        /// 获取所有目录
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <returns></returns>
        public static List<string> GetAllDirectories(string sourcePath)
        {
            List<string> folders = new List<string>();
            GetAllDirectoriesInner(folders, sourcePath);
            return folders;
        }

        /// <summary>
        /// 获取所有目录
        /// </summary>
        /// <param name="list"></param>
        /// <param name="sourcePath"></param>
        /// <returns></returns>
        public static void GetAllDirectoriesInner(List<string> list, string sourcePath)
        {
            if(Directory.Exists(sourcePath))
            {
                string[] folders = Directory.GetDirectories(sourcePath);
                list.AddRange(folders);
                foreach(string folder in folders)
                {
                    GetAllDirectoriesInner(list, folder);
                }
            }
        }

        /// <summary>
        /// 获取所有文件
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="searchPattern"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static List<string> GetAllFiles(string sourcePath, string searchPattern = "*", SearchOption option = SearchOption.AllDirectories)
        {
            List<string> files = new List<string>();
            List<string> folders = new List<string>() { sourcePath };

            foreach(string folder in folders)
            {
                if(Directory.Exists(folder))
                {
                    string[] folderFiles = Directory.GetFiles(folder, searchPattern, option);
                    files.AddRange(folderFiles);
                }
            }

            return files;
        }
    }
}
