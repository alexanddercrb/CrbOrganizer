using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using MetadataExtractor;
using ReverseGeocoding;

namespace CrbOrganizer
{
    public static class ProgramRunner
    {
        public static async Task Runner(ExecutionParams parameters, MainWindow uiInstance)
        {
            uiInstance.AppendLogs("Starting app");
            uiInstance.AppendLogs();

            try
            {
                uiInstance.Clear();
                MoveMediaFiles(parameters, uiInstance);
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

        public static void MoveMediaFiles(ExecutionParams parameters, MainWindow uiInstance)
        {
            var fileList = System.IO.Directory.GetFiles(parameters.sourcePath);
            uiInstance.AppendLogs($"{fileList.Length} files found in path {parameters.sourcePath}");
            if (fileList.Length > 0)
            {
                uiInstance.AppendLogs("Moving the documents...");

                var counter = 0;

                foreach (var file in fileList)
                {
                    var fileMoved = IsFileSuccessfullyMoved(file, parameters, uiInstance);
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

            if (parameters.includeSubfolders)
            {
                CheckAndProcessSubfolders(parameters, uiInstance);
            }
        }

        public static bool IsFileSuccessfullyMoved(string file, ExecutionParams parameters, MainWindow uiInstance)
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
                string longitude = "";
                string latitude = "";

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

                        if (tag.Name.ToLowerInvariant().Equals("gps latitude") && !string.IsNullOrWhiteSpace(tag.Description))
                        {
                            latitude = tag.Description;
                        }
                        if (tag.Name.ToLowerInvariant().Equals("gps longitude") && !string.IsNullOrWhiteSpace(tag.Description))
                        {
                            longitude = tag.Description;
                        }
                    }

                if (date != null)
                {
                    //default is day mode
                    var partialPath = Path.Combine(date.Value.ToString("yyyy"), date.Value.ToString("MMM"));
                    var relativePath = string.Empty;
                    
                    if (parameters.categorizationType == CategorizationTypeEnum.ByMonth)
                    {
                        relativePath = partialPath;
                    }
                    else if (parameters.categorizationType == CategorizationTypeEnum.ByYear)
                    {
                        relativePath = Path.Combine(date.Value.ToString("yyyy"));
                    }
                    else {
                        relativePath = Path.Combine(partialPath, date.Value.ToString("dd"));
                    }

                    if (parameters.locationWrapper)
                    {
                        if (string.IsNullOrWhiteSpace(longitude) || string.IsNullOrWhiteSpace(longitude))
                            relativePath = Path.Combine("unknown_location", partialPath);
                        else
                        {
                            double longitudeDouble = CoordinateToDouble(longitude);
                            double latitudeDouble = CoordinateToDouble(latitude);
                            var locationName = GeocoderService.Instance.GetNearestPlaceName(latitudeDouble, longitudeDouble);
                            relativePath = Path.Combine(locationName, partialPath);
                        }
                    }

                    var finalFolder = Path.Combine(parameters.targetPath, relativePath);
                    
                    var fileName = Path.GetFileName(file);

                    var finalPath = Path.Combine(finalFolder, fileName);
                    
                    System.IO.Directory.CreateDirectory(finalFolder);

                    if (parameters.keepOriginal)
                        File.Copy(file, finalPath, true);
                    else
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

        private static double CoordinateToDouble(string coordinate)
        {
            // Remove unnecessary characters and split the input
            coordinate = coordinate.Replace("°", " ").Replace("'", " ").Replace("\"", " ").Trim();
            string[] parts = coordinate.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3)
            {
                throw new FormatException($"Invalid coordinate format for '{coordinate}'");
            }

            double degrees = double.Parse(parts[0], CultureInfo.InvariantCulture);
            double minutes = double.Parse(parts[1], CultureInfo.InvariantCulture);
            double seconds = double.Parse(parts[2], CultureInfo.InvariantCulture);

            // Convert to decimal degrees
            return degrees + (minutes / 60) + (seconds / 3600);
        }

        private static void CheckAndProcessSubfolders(ExecutionParams parameters, MainWindow uiInstance)
        {
            uiInstance.AppendLogs();
            uiInstance.AppendLogs();
            uiInstance.AppendLogs($"Getting subfolders for {parameters.sourcePath}", true);
            var foldersList = System.IO.Directory.GetDirectories(parameters.sourcePath);
            uiInstance.AppendLogs($"{foldersList.Length} subfolders found in path {parameters.sourcePath}");
            if (foldersList.Length > 0)
            {
                uiInstance.AppendLogs("Started to process subfolders");
                uiInstance.AppendLogs();

                foreach (var folder in foldersList)
                {
                    var newParameters = new ExecutionParams()
                    {
                        sourcePath = folder,
                        targetPath = parameters.targetPath,
                        categorizationType = parameters.categorizationType,
                        includeSubfolders = true,
                        keepOriginal = parameters.keepOriginal,
                        locationWrapper = parameters.locationWrapper
                    };
                    MoveMediaFiles(newParameters, uiInstance);
                }
            }
        }
    }
}
