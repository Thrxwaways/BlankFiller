using System;
using System.Collections.Generic;
using System.IO;

namespace BlankFiller.Services;

public class AppSettings
{
    public string TesseractDataPath { get; set; } =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

    public string DatabasePath { get; set; } =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "blanks.db");

    public string ScreenshotsPath { get; set; } =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");

    public string ExcelTemplatesPath { get; set; } =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Excel", "Templates");

    public string ExcelOutputPath { get; set; } =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Excel", "Output");

    /// <summary>
    /// Варианты источников дохода (можно редактировать)
    /// </summary>
    public List<string> IncomeSources { get; set; } = new()
    {
        "строительству",
        "рабочим",
        "разнорабочим",
        "электриком",
        "сварщиком",
        "поваром",
        "уборщиком",
        "дворником"
    };
}