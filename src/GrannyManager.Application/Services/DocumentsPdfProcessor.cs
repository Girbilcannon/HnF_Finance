using System;
using System.IO;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Security;

namespace GrannyManager.Application.Services;

public static class DocumentsPdfProcessor
{
    public static bool IsPdf(string filePath)
    {
        return string.Equals(Path.GetExtension(filePath), ".pdf", StringComparison.OrdinalIgnoreCase);
    }

    public static void MergePdfs(IReadOnlyList<string> sourcePdfPaths, string targetPdfPath, string? password)
    {
        if (sourcePdfPaths is null || sourcePdfPaths.Count == 0)
            throw new InvalidOperationException("Choose at least one PDF to merge.");

        using var outputDocument = new PdfDocument();

        foreach (var sourcePdfPath in sourcePdfPaths)
        {
            using var inputDocument = PdfReader.Open(sourcePdfPath, PdfDocumentOpenMode.Import);

            for (var pageIndex = 0; pageIndex < inputDocument.PageCount; pageIndex++)
                outputDocument.AddPage(inputDocument.Pages[pageIndex]);
        }

        ApplyPasswordIfNeeded(outputDocument, password);
        outputDocument.Save(targetPdfPath);
    }

    public static void CopyPdfWithOptionalPassword(string sourcePdfPath, string targetPdfPath, string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            File.Copy(sourcePdfPath, targetPdfPath, overwrite: false);
            return;
        }

        using var inputDocument = PdfReader.Open(sourcePdfPath, PdfDocumentOpenMode.Import);
        using var outputDocument = new PdfDocument();

        for (var pageIndex = 0; pageIndex < inputDocument.PageCount; pageIndex++)
            outputDocument.AddPage(inputDocument.Pages[pageIndex]);

        ApplyPasswordIfNeeded(outputDocument, password);
        outputDocument.Save(targetPdfPath);
    }

    private static void ApplyPasswordIfNeeded(PdfDocument document, string? password)
    {
        if (document is null || string.IsNullOrWhiteSpace(password))
            return;

        var security = document.SecuritySettings;
        security.UserPassword = password;
        security.OwnerPassword = password;

        security.PermitAnnotations = false;
        security.PermitAssembleDocument = false;
        security.PermitExtractContent = false;
        security.PermitFormsFill = true;
        security.PermitFullQualityPrint = true;
        security.PermitModifyDocument = false;
        security.PermitPrint = true;
    }
}
