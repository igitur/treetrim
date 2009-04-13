using System ;
using System.Collections.Generic ;
using System.Globalization ;
using System.IO ;
using System.Reflection ;

namespace Dunn.TreeTrim
{
    /// <summary>
    /// Represents a disk.  A facade over the Path and File API's.
    /// </summary>
    public class Disk : IDisk
    {
        /// <summary>
        /// Gets the local directory where the executing assembly is stored.
        /// </summary>
        /// <value>The directory of the executing assembly.</value>
        public string DirectoryOfExecutingAssembly
        {
            get
            {
                var uri = new Uri( Assembly.GetExecutingAssembly( ).GetName( ).CodeBase );

                string codebase = uri.LocalPath;

                string thisDirectory = Path.GetDirectoryName( codebase );

                return thisDirectory;
            }
        }

        /// <summary>
        /// Copies the folder.
        /// </summary>
        /// <param name="fromDirectory">From directory.</param>
        /// <param name="toDirectory">To directory.</param>
        public void CopyFolder(string fromDirectory, string toDirectory)
        {
            if (!Directory.Exists(fromDirectory))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        @"Cannot copy folder as the source folder '{0}' does not exist.",
                        fromDirectory ) ) ;
            }

            if (!Directory.Exists(toDirectory))
            {
                Directory.CreateDirectory(toDirectory);
            }

            string[] files = Directory.GetFiles(fromDirectory);

            // Copy the files and overwrite destination files if they already exist.
            foreach (string eachFile in files)
            {
                string destFile = Path.Combine(toDirectory, Path.GetFileName(eachFile));
                File.Copy(eachFile, destFile, true);
            }

            string[] directories = Directory.GetDirectories(fromDirectory);
            
            // Copy the files and overwrite destination files if they already exist.
            foreach (string eachDirectory in directories)
            {
                // Use static Path methods to extract only the file name from the path.
                string fileName = Path.GetFileName(eachDirectory);
                string destDirectory = Path.Combine(toDirectory, fileName);
                
                CopyFolder(eachDirectory, destDirectory);
            }
        }

        /// <summary>
        /// Deletes the file or directory.
        /// </summary>
        /// <param name="path">The path to the file or directory.</param>
        public void DeleteFileOrDirectory(string path)
        {
            if (isDirectory(path))
            {
                Directory.Delete(path, true);
                return;
            }

            if (isFileReadOnly(path))
            {
                SetFileReadOnly(path, false);
            }

            File.Delete(path);
        }

        /// <summary>
        /// Deletes the files or directories.
        /// </summary>
        /// <param name="paths">The paths.</param>
        public void DeleteFilesOrDirectories(IEnumerable<string> paths)
        {
            foreach (string eachPath in paths)
            {
                DeleteFileOrDirectory(eachPath);
            }
        }

        /// <summary>
        /// Gets the type of the entity, for instance, Folder, File, etc.
        /// </summary>
        /// <param name="path">The path to the item on disk.</param>
        /// <returns>The <see cref="DiskEntity"/> representing the entity at the specified path.</returns>
        public DiskEntity GetEntityType( string path )
        {
            if( File.Exists( path ))
            {
                return DiskEntity.File ;
            }

            return DiskEntity.Folder;
        }

        /// <summary>
        /// Determines whether the specified path is a folder.
        /// </summary>
        /// <param name="path">The path with which to see if it's a folder.</param>
        /// <returns>
        /// <c>true</c> if the specified path is a folder; otherwise, <c>false</c>.
        /// </returns>
        public bool IsFolder( string path )
        {
            return GetEntityType( path ) == DiskEntity.Folder ;
        }

        /// <summary>
        /// Determines whether the specified path is a file.
        /// </summary>
        /// <param name="path">The path to the disk entity.</param>
        /// <returns>
        /// <c>true</c> if the specified path is a file; otherwise, <c>false</c>.
        /// </returns>
        public bool IsFile( string path )
        {
            return GetEntityType( path ) == DiskEntity.File;
        }

        /// <summary>
        /// Builds a list containing all the files under a specific directory
        /// </summary>
        /// <param name="directory">The parent directory.</param>
        /// <returns>A collection of paths representing the child directories.</returns>
        public IEnumerable<string> GetChildDirectoriesRecursively(string directory)
        {
            foreach (string eachChildDirectory in Directory.GetDirectories(directory))
            {
                yield return eachChildDirectory;
            }
        }

        /// <summary>
        /// Builds a list containing all the files under a specific directory
        /// </summary>
        /// <param name="directory">The parent directory.</param>
        /// <returns>A collection of paths representing the child files.</returns>
        public IEnumerable<string> GetFilesInDirectoryRecursively(string directory)
        {
            var al = new List<string>();
            bool isEmpty = true;

            foreach (string eachFile in Directory.GetFiles(directory))
            {
                al.Add(eachFile);
                isEmpty = false;
            }

            if (isEmpty)
            {
                if (Directory.GetDirectories(directory).Length == 0)
                {
                    al.Add(directory + "/");
                }
            }

            foreach (string eachDirectory in Directory.GetDirectories(directory))
            {
                foreach (string s in GetFilesInDirectoryRecursively(eachDirectory))
                {
                    al.Add(s);
                }
            }

            return al;
        }

        /// <summary>
        /// Reads a text file from disk
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>The contents of the file.</returns>
        public string ReadAllText(string path)
        {
            using (StreamReader sr = File.OpenText(path))
            {
                return sr.ReadToEnd();
            }
        }

        /// <summary>
        /// Toggles the Read-Only attribute on a file
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <param name="isReadOnly">Whether or not to set the read only flag.</param>
        public void SetFileReadOnly(string filePath, bool isReadOnly)
        {
            var fileInfo = new FileInfo(filePath);
            
            if (!isReadOnly)
            {
                fileInfo.Attributes = fileInfo.Attributes & FileAttributes.Normal;
            }
            else
            {
                fileInfo.Attributes = fileInfo.Attributes & FileAttributes.ReadOnly;
            }
        }

        /// <summary>
        /// Writes a text file to disk
        /// </summary>
        /// <param name="path">The path of the file to write to.</param>
        /// <param name="fileContents">The contents to write.</param>
        public void WriteTextToFile(string path, string fileContents)
        {
            SetFileReadOnly(path, false);
            using (var sw = new StreamWriter(path, false))
            {
                sw.Write(fileContents);
            }
        }

        static bool isDirectory(string path)
        {
            if (path.EndsWith("\\", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (File.Exists(path) || Directory.Exists(path))
            {
                bool directory = (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
                bool hidden = (File.GetAttributes(path) & FileAttributes.Hidden) == FileAttributes.Hidden;

                return directory && !hidden;
            }

            return false;
        }

        static bool isFileReadOnly(string path)
        {
            var fileInfo = new FileInfo(path);

            return (fileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
        }
    }
}
