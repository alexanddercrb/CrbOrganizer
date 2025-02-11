using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using MetadataExtractor;

namespace MediaOrganizerV2
{
    public static class ProgramRunner
    {
        public static async Task Runner(string sourceFolder, string destinationFolder, CategorizationTypeEnum categorizationType, bool includeSubfolders, MainWindow uiInstance)
        {
            uiInstance.AppendLogs("Starting app");
            uiInstance.AppendLogs();

            try
            {
                uiInstance.Clear();
                MoveMediaFiles(sourceFolder, destinationFolder, categorizationType, includeSubfolders, uiInstance);
                uiInstance.EnableControls();

                uiInstance.AppendLogs("\nExecution finished", true);
            }
            catch (Exception ex)
            {
                uiInstance.AppendLogs();
                uiInstance.AppendLogs("Critical Error: " + ex.ToString(), true, 2);
                uiInstance.EnableControls();
            }
        }

        public static void MoveMediaFiles(string sourceFolder, string destinationFolder, CategorizationTypeEnum categorizationType, bool includeSubfolders, MainWindow uiInstance)
        {
            var fileList = System.IO.Directory.GetFiles(sourceFolder);
            uiInstance.AppendLogs($"{fileList.Length} files found in path {sourceFolder}");
            if (fileList.Length > 0)
            {
                uiInstance.AppendLogs("Moving the documents...");

                var counter = 0;

                foreach (var file in fileList)
                {
                    var fileMoved = IsFileSuccessfullyMoved(file, destinationFolder, categorizationType, uiInstance);
                    if (fileMoved)
                        counter++;
                }

                uiInstance.AppendLogs($"{counter} media files succesfully organised in folders.", false, 1);
                var remainingFiles = fileList.Length - counter;
                if (remainingFiles > 0)
                {
                    uiInstance.AppendLogs($"{fileList.Length - counter} files are invalid or have missing metadatas. Please review them manually", false, 2);
                }
            }

            if (includeSubfolders)
            {
                CheckAndProcessSubfolders(sourceFolder, destinationFolder, categorizationType, uiInstance);
            }
        }

        public static bool IsFileSuccessfullyMoved(string file, string destinationFolder, CategorizationTypeEnum categorizationType, MainWindow uiInstance)
        {
            if (file.ToLowerInvariant().EndsWith("exe") 
            || file.ToLowerInvariant().EndsWith("ini")
            || file.ToLowerInvariant().EndsWith(".ds_store")) 
                return false;

            try
            {
                IEnumerable<MetadataExtractor.Directory> metadataDirectories = ImageMetadataReader.ReadMetadata(file);

                var fileDate = string.Empty;
                DateTime? date = null;

                foreach (var metadataDirectory in metadataDirectories)
                    foreach (var tag in metadataDirectory.Tags)
                    {
                        if (tag.Name.ToLowerInvariant().Contains("date/time original") && !string.IsNullOrWhiteSpace(tag.Description))
                        {
                            date = DateTime.ParseExact(tag.Description, "yyyy:MM:dd H:m:ss", CultureInfo.InvariantCulture);
                        }

                        if (file.ToLowerInvariant().Contains("mov") || file.ToLowerInvariant().Contains("mp4"))
                            if (metadataDirectory.Name.ToLowerInvariant().Contains("movie")
                                && tag.Name.ToLowerInvariant().Contains("created") && !string.IsNullOrWhiteSpace(tag.Description))
                            {
                                date = DateTime.ParseExact(tag.Description, "ddd MMM d H:m:ss yyyy", CultureInfo.InvariantCulture);
                            }


                    }

                if (date != null)
                {
                    //default is day mode
                    var partialPath = Path.Combine(date.Value.ToString("yyyy"), date.Value.ToString("MMM"));
                    var relativePath = string.Empty;
                    
                    if (categorizationType == CategorizationTypeEnum.ByMonth)
                    {
                        relativePath = partialPath;
                    }
                    else if (categorizationType == CategorizationTypeEnum.ByYear)
                    {
                        relativePath = Path.Combine(date.Value.ToString("yyyy"));
                    }
                    else {
                        relativePath = Path.Combine(partialPath, date.Value.ToString("dd"));
                    }

                    var finalFolder = Path.Combine(destinationFolder, relativePath);
                    
                    var fileName = Path.GetFileName(file);

                    var finalPath = Path.Combine(finalFolder, fileName);
                    
                    System.IO.Directory.CreateDirectory(finalFolder);

                    File.Move(file, finalPath, true);

                    return true;
                }
            }
            catch (ImageProcessingException ex)
            {
                uiInstance.AppendLogs($"Error: (!) File {file} is not a valid image", true, 2);
                return false;
            }

            return false;
        }

        public static void CheckAndProcessSubfolders(string sourceFolder, string destinationFolder, CategorizationTypeEnum categorizationType, MainWindow uiInstance)
        {
            uiInstance.AppendLogs();
            uiInstance.AppendLogs();
            uiInstance.AppendLogs($"Getting subfolders for {sourceFolder}", true);
            var foldersList = System.IO.Directory.GetDirectories(sourceFolder);
            uiInstance.AppendLogs($"{foldersList.Length} subfolders found in path {sourceFolder}");
            if (foldersList.Length > 0)
            {
                uiInstance.AppendLogs("Started to process subfolders");
                uiInstance.AppendLogs();

                foreach (var folder in foldersList)
                {
                    MoveMediaFiles(folder, destinationFolder, categorizationType, true, uiInstance);
                }
            }
        }
    }
}
