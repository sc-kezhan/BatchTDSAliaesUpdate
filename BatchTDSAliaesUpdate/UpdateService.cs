using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BatchTDSAliaesUpdate
{
    public class UpdateService
    {
        public IEnumerable<string> AllIncludePathes;
        public List<Item> ItemMapList;
        public List<string> OutputFileLines;
        static string ProjectFolderPath { get; set; }
        public void Process(string projectFilePath)
        {
            //get project file from argument
            var projName = projectFilePath;
            //check if project file exists
            if (!File.Exists((projName)))
            {
                Console.WriteLine("Project file doesn't exist");
                Console.ReadKey();
                return;
            }
            ProjectFolderPath = new FileInfo(projName).DirectoryName;
            //read all lines of project files
            var projFileContent = File.ReadAllLines(projName);
            //find all Sitecore Item lines
            var allSitecoreItemLines = ExtractTDSLinesFromProjectFile(projFileContent);

            if (!allSitecoreItemLines.Any())
            {
                Console.WriteLine("Is it a TDS project?");
                Console.ReadKey();
                return;
            }
            //extract all include pathes
            AllIncludePathes = ExtractIncludePathsFromTDSLines(allSitecoreItemLines);

            ItemMapList = BuildItemMapList(AllIncludePathes);

            //Move file
            //start from level 2 (level 1 is sitecore)
            ProcessFileChanges(ItemMapList);

            //atfer all done, go through all line of project file again and process each line(meaning an item)
            OutputFileLines = BuildNewProjectFileList(projFileContent, ItemMapList);

            //here assumes lines are in order of parents are always in frotn of children
            var bakPath = projName + ".bak";
            if (File.Exists(bakPath))
            {
                File.Delete(bakPath);
            }
            File.Move(projName, bakPath);
            //write all new lines to overwrite the old project file, do back up of old file
            File.WriteAllLines(projName, OutputFileLines);
        }

        public virtual List<string> BuildNewProjectFileList(string[] projFileContent, List<Item> itemMapList)
        {
            var outputFile = new List<string>();
            foreach (var fileLine in projFileContent)
            {
                //rename the item/folder and build up the path using parent items new name, replace the old line with new line in project
                var lineOutput = fileLine;
                try
                {
                    if (fileLine.Trim().StartsWith("<SitecoreItem"))
                    {
                        var includePath = ExtractIncludePathFromLine(fileLine);
                        var currentItem = itemMapList.FirstOrDefault(x =>
                            x.OriginalIncludePathAsID.ToUpperInvariant() == includePath.ToUpperInvariant());
                        var currentLevel = currentItem.Level;
                        var oldName = currentItem.OldName;
                        var newParentPath = GetParentNewPath(itemMapList, currentItem);
                        var newInCludePath = newParentPath + "\\" + currentItem.NewName + ".item";
                        lineOutput = lineOutput.Replace(includePath, newInCludePath);
                        if (!lineOutput.Contains("SitecoreName"))
                        {
                            lineOutput = lineOutput.Replace("</SitecoreItem>",
                                $"<SitecoreName>{oldName}</SitecoreName></SitecoreItem>");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }


                outputFile.Add(lineOutput);
            }

            return outputFile;
        }

        public virtual void ProcessFileChanges(List<Item> itemMapList)
        {
            for (int i = 2; i < 1000; i++) //1000 is magic number
            {
                var currentLevel = i;
                var currentLevelItems = itemMapList.Where(x => x.Level == i);
                foreach (var currentLevelItem in currentLevelItems)
                {
                    MoveFile(currentLevelItem, itemMapList);
                }
            }
        }

        public virtual List<Item> BuildItemMapList(IEnumerable<string> allIncludePathes)
        {
            var itemMapList = new List<Item>()
            {
                new Item()
                {
                    Level = 1,
                    OldName = "sitecore",
                    NewName = "sitecore",
                    OriginalIncludePathAsID = "sitecore",
                    ParentId = ""
                }
            };
            //start from level 2 (level 1 is sitecore)
            for (int i = 2; i < 1000; i++) //1000 is magic number
            {
                var currentLevel = i;
                //go through all lines that have 'level - 1' amount of '\' and end with '.item'
                var thisLevelIncludePathes = allIncludePathes.Where(x => x.Count(c => c == '\\') == currentLevel - 1);
                //level 2 example:
                //sitecore\templates.item
                //sitecore\content.item
                foreach (var thisLevelIncludePath in thisLevelIncludePathes)
                {
                    //count all of them and assign numbers using mapping class
                    var levelArray = ExtractLevelsFromIncludePath(thisLevelIncludePath);
                    var item = new Item()
                    {
                        Level = currentLevel,
                        OldName = CleanupItemExtension(levelArray[currentLevel - 1]),
                        OriginalIncludePathAsID = thisLevelIncludePath
                    };
                    itemMapList.Add(item);
                }

                //now with all lines, map their new name
                var allCurrentLevelItems = itemMapList.Where(x => x.Level == currentLevel);
                int currentLevelItemIndex = 1;
                foreach (var currentLevelItem in allCurrentLevelItems)
                {
                    var parentItem = itemMapList.FirstOrDefault(x =>
                        x.Level == currentLevel - 1 && currentLevelItem.OriginalIncludePathAsID.ToUpperInvariant().Replace($"\\{currentLevelItem.OldName.ToUpperInvariant()}.ITEM", "") == x.OriginalIncludePathAsID.ToUpperInvariant().Replace(".ITEM", ""));
                    currentLevelItem.NewName = currentLevelItemIndex.ToString();
                    currentLevelItem.ParentId = parentItem != null ? parentItem.OriginalIncludePathAsID : "sitecore";
                    currentLevelItemIndex++;
                }

                //old name -> new name
                //level
            }

            return itemMapList;
        }

        public virtual IEnumerable<string> ExtractIncludePathsFromTDSLines(IEnumerable<string> allSitecoreItemLines)
        {
            return allSitecoreItemLines.Select(x => { return ExtractIncludePathFromLine(x); });
        }

        public virtual IEnumerable<string> ExtractTDSLinesFromProjectFile(string[] projFileContent)
        {
            return projFileContent.Where(x => x.Trim().StartsWith("<SitecoreItem"));
        }

        public virtual void MoveFile(Item currentLevelItem, List<Item> itemMapList)
        {
            //assume parents are already moved to new place
            var parentNewPath = GetParentNewPath(itemMapList, currentLevelItem);
            var fullParentNewPath = ProjectFolderPath + "\\" + parentNewPath;
            var oldFolderPath = fullParentNewPath + "\\" + currentLevelItem.OldName;
            var newFolderPath = fullParentNewPath + "\\" + currentLevelItem.NewName;
            var oldItemPath = fullParentNewPath + "\\" + currentLevelItem.OldName + ".item";
            var newItemPath = fullParentNewPath + "\\" + currentLevelItem.NewName + ".item";
            if (Directory.Exists(oldFolderPath) && oldFolderPath.ToUpperInvariant() != newFolderPath.ToUpperInvariant())
            {
                if (Directory.Exists(newFolderPath))
                {
                    Directory.Delete(newFolderPath, true);
                }

                Directory.Move(oldFolderPath, newFolderPath);
            }
            if (File.Exists(oldItemPath) && oldItemPath.ToUpperInvariant() != newItemPath.ToUpperInvariant())
            {
                if (File.Exists(newItemPath))
                {
                    File.Delete(newItemPath);
                }

                File.Move(oldItemPath, newItemPath);
            }
        }

        public virtual string GetParentNewPath(List<Item> itemMapList, Item currentLevelItem)
        {
            var getAllParents = GetAllParents(itemMapList, currentLevelItem);
            var parentPath = string.Join("\\", getAllParents.Select(x => x.NewName));
            if (!Directory.Exists(ProjectFolderPath + "\\" + parentPath))
            {
                throw new Exception("somethingh not right");
            }
            return parentPath;
        }

        public virtual List<Item> GetAllParents(List<Item> itemMapList, Item currentLevelItem)
        {
            var allParents = new List<Item>();
            var childItem = currentLevelItem;
            for (int i = currentLevelItem.Level - 1; i >= 1; i--)
            {
                try
                {
                    var currentItem
                        = itemMapList.FirstOrDefault(x => x.Level == i && x.OriginalIncludePathAsID.ToUpperInvariant() == childItem.ParentId.ToUpperInvariant());

                    allParents.Add(currentItem);
                    childItem = currentItem;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            allParents.Reverse();
            return allParents;
        }

        public virtual string CleanupItemExtension(string level)
        {
            return level.Replace(".item", "");
        }

        public virtual string[] ExtractLevelsFromIncludePath(string s)
        {
            return s.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public virtual string ExtractIncludePathFromLine(string x)
        {
            var doubleQuoteSplitArray = x.Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries);
            return doubleQuoteSplitArray[1];
        }
    }

    public class Item
    {
        public int Level { get; set; }
        public string OriginalIncludePathAsID { get; set; }
        public string ParentId { get; set; }
        public string NewName { get; set; }
        public string OldName { get; set; }
    }
}
