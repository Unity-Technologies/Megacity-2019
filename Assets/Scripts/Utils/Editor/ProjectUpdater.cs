using UnityEngine;
using UnityEditor;
using System.IO;

public class ProjectUpdater : EditorWindow
{
    private string projectSourceFolder;
    private string projectDestinationFolder;

    [MenuItem("Tools/Project Updater")]
    public static void ShowWindow()
    {
        GetWindow<ProjectUpdater>("Project Updater");
    }

    private void OnGUI()
    {
        projectSourceFolder = Directory.GetParent(Application.dataPath).FullName;

        GUILayout.Label("Project Destination Folder", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        projectDestinationFolder = EditorGUILayout.TextField(projectDestinationFolder);
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            projectDestinationFolder = EditorUtility.OpenFolderPanel("Select Project Source Folder", "", "");
            GUI.FocusControl(null); // Remove focus from text field to refresh GUI
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(projectDestinationFolder));
        if (GUILayout.Button("Migrate"))
        {
            UpdateProject();
            // Show a notification dialog when the update is completed
            EditorUtility.DisplayDialog("Update Complete", "The project destination has been updated.", "OK");
        }
        EditorGUI.EndDisabledGroup();
    }

    private void UpdateProject()
    {
        // Delete existing Assets, Packages, and Project Settings folders in the destination folder
        DeleteFolder(Path.Combine(projectDestinationFolder, "Assets"));
        DeleteFolder(Path.Combine(projectDestinationFolder, "Packages"));
        DeleteFolder(Path.Combine(projectDestinationFolder, "ProjectSettings"));

        // Copy Assets, Packages, and Project Settings folders from source to destination
        CopyFolder(Path.Combine(projectSourceFolder, "Assets"), Path.Combine(projectDestinationFolder, "Assets"));
        CopyFolder(Path.Combine(projectSourceFolder, "Packages"), Path.Combine(projectDestinationFolder, "Packages"));
        CopyFolder(Path.Combine(projectSourceFolder, "ProjectSettings"),
            Path.Combine(projectDestinationFolder, "ProjectSettings"));

        // Delete LICENCE.md, README.md, and Third Party Notices.md in the destination folder
        DeleteFile(Path.Combine(projectDestinationFolder, "LICENCE.md"));
        DeleteFile(Path.Combine(projectDestinationFolder, "README.md"));
        DeleteFile(Path.Combine(projectDestinationFolder, "Third Party Notices.md"));

        // Copy LICENCE.md, README.md, and Third Party Notices.md from parent folder to the destination folder
        string parentFolder = Directory.GetParent(projectSourceFolder).FullName;
        CopyFile(Path.Combine(parentFolder, "LICENCE.md"), Path.Combine(projectDestinationFolder, "LICENCE.md"));
        CopyFile(Path.Combine(parentFolder, "README.md"), Path.Combine(projectDestinationFolder, "README.md"));
        CopyFile(Path.Combine(parentFolder, "Third Party Notices.md"),
            Path.Combine(projectDestinationFolder, "Third Party Notices.md"));

        // Update README.md file in the destination folder
        string readmeFilePath = Path.Combine(projectDestinationFolder, "README.md");
        UpdateReadmeFile(readmeFilePath);
    }

    private void DeleteFolder(string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, true);
        }
    }

    private void CopyFolder(string sourceFolderPath, string destinationFolderPath)
    {
        if (Directory.Exists(sourceFolderPath))
        {
            Directory.CreateDirectory(destinationFolderPath);

            foreach (string filePath in Directory.GetFiles(sourceFolderPath))
            {
                string fileName = Path.GetFileName(filePath);
                string destinationPath = Path.Combine(destinationFolderPath, fileName);
                File.Copy(filePath, destinationPath, true);
            }

            foreach (string subFolderPath in Directory.GetDirectories(sourceFolderPath))
            {
                string subFolderName = Path.GetFileName(subFolderPath);
                string destinationSubFolderPath = Path.Combine(destinationFolderPath, subFolderName);
                CopyFolder(subFolderPath, destinationSubFolderPath);
            }
        }
    }

    private void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private void CopyFile(string sourceFilePath, string destinationFilePath)
    {
        if (File.Exists(sourceFilePath))
        {
            File.Copy(sourceFilePath, destinationFilePath, true);
        }
    }

    private void UpdateReadmeFile(string readmeFilePath)
    {
        if (File.Exists(readmeFilePath))
        {
            string[] lines = File.ReadAllLines(readmeFilePath);
            bool modified = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.Contains("MegacityMultiplayer/Assets"))
                {
                    line = line.Replace("MegacityMultiplayer/Assets", "Assets");
                    lines[i] = line;
                    modified = true;
                }
                
                if (line.Contains("https://github.cds.internal.unity3d.com/unity/"))
                {
                    line = line.Replace("https://github.cds.internal.unity3d.com/unity/", "https://github.com/Unity-Technologies/");
                    lines[i] = line;
                    modified = true;
                }
            }

            if (modified)
            {
                File.WriteAllLines(readmeFilePath, lines);
                Debug.Log("Readme file updated successfully.");
            }
        }
    }
}
