using System;
using System.IO;
using System.Linq;

namespace BlankFiller.Views.Pages;

public static class ExcelFileHelper
{
    public static string? FindFile(string folder, string searchPattern)
    {
        if (!Directory.Exists(folder)) return null;

        return Directory.GetFiles(folder, "*.xlsx")
            .Concat(Directory.GetFiles(folder, "*.xls"))
            .Where(f =>
            {
                var name = Path.GetFileName(f);
                return name.StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase);
            })
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .FirstOrDefault();
    }
}