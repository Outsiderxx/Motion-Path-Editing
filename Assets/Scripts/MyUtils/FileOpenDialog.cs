
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;



[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public class OpenFileName
{
    public int structSize = 0;
    public IntPtr dlgOwner = IntPtr.Zero;
    public IntPtr instance = IntPtr.Zero;
    public string filter;
    public string customFilter;
    public int maxCustFilter = 0;
    public int filterIndex = 0;
    public IntPtr file;
    public int maxFile = 0;
    public string fileTitle;
    public int maxFileTitle = 0;
    public string initialDir;
    public string title;
    public int flags = 0;
    public short fileOffset = 0;
    public short fileExtension = 0;
    public string defExt;
    public IntPtr custData = IntPtr.Zero;
    public IntPtr hook = IntPtr.Zero;
    public string templateName;
    public IntPtr reservedPtr = IntPtr.Zero;
    public int reservedInt = 0;
    public int flagsEx = 0;
}

public enum OpenFileNameFlags
{
    OFN_HIDEREADONLY = 0x4,
    OFN_FORCESHOWHIDDEN = 0x10000000,
    OFN_ALLOWMULTISELECT = 0x200,
    OFN_EXPLORER = 0x80000,
    OFN_FILEMUSTEXIST = 0x1000,
    OFN_PATHMUSTEXIST = 0x800,
    OFN_NOCHANGEDIR = 0x00000008,
}



class DialogShow
{
    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

    public static string[] ShowOpenFileDialog(string dialogTitle, string startPath, string filter, bool showHidden, bool allowMultiSelect)
    {
        const int MAX_FILE_LENGTH = 2048;

        OpenFileName ofn = new OpenFileName();

        ofn.structSize = Marshal.SizeOf(ofn);
        ofn.filter = filter.Replace("|", "\0") + "\0";
        ofn.fileTitle = new String(new char[MAX_FILE_LENGTH]);
        ofn.maxFileTitle = ofn.fileTitle.Length;
        ofn.initialDir = startPath;
        ofn.title = dialogTitle;
        ofn.flags = (int)OpenFileNameFlags.OFN_HIDEREADONLY | (int)OpenFileNameFlags.OFN_EXPLORER | (int)OpenFileNameFlags.OFN_FILEMUSTEXIST | (int)OpenFileNameFlags.OFN_PATHMUSTEXIST | (int)OpenFileNameFlags.OFN_NOCHANGEDIR;

        // Create buffer for file names
        string fileNames = new String(new char[MAX_FILE_LENGTH]);
        ofn.file = Marshal.StringToBSTR(fileNames);
        ofn.maxFile = fileNames.Length;

        if (showHidden)
        {
            ofn.flags |= (int)OpenFileNameFlags.OFN_FORCESHOWHIDDEN;
        }

        if (allowMultiSelect)
        {
            ofn.flags |= (int)OpenFileNameFlags.OFN_ALLOWMULTISELECT;
        }

        if (GetOpenFileName(ofn))
        {
            List<string> selectedFilesList = new List<string>();

            long pointer = (long)ofn.file;
            string file = Marshal.PtrToStringAuto(ofn.file);

            // Retrieve file names
            while (file.Length > 0)
            {
                selectedFilesList.Add(file);

                pointer += file.Length * 2 + 2;
                ofn.file = (IntPtr)pointer;
                file = Marshal.PtrToStringAuto(ofn.file);
            }

            if (selectedFilesList.Count == 1)
            {
                // Only one file selected with full path
                return selectedFilesList.ToArray();
            }
            else
            {
                // Multiple files selected, add directory
                string[] selectedFiles = new string[selectedFilesList.Count - 1];

                for (int i = 0; i < selectedFiles.Length; i++)
                {
                    selectedFiles[i] = selectedFilesList[0] + "\\" + selectedFilesList[i + 1];
                }

                return selectedFiles;
            }
        }
        else
        {
            // "Cancel" pressed
            return null;
        }
    }
}