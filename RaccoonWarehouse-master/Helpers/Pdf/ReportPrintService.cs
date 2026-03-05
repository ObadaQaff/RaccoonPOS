using Microsoft.Win32;
using QuestPDF.Fluent;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace RaccoonWarehouse.Helpers.Pdf
{
    public static class ReportPrintService
    {
        public static void ExportPdf(IReportDocument document, Window owner)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"{document.FileName}_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                AddExtension = true,
                DefaultExt = ".pdf"
            };

            if (dialog.ShowDialog(owner) != true)
                return;

            document.GeneratePdf(dialog.FileName);
            MessageBox.Show(owner, "تم تصدير التقرير بنجاح.", "PDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void Print(IReportDocument document, Window owner)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"{document.FileName}_{Guid.NewGuid():N}.pdf");
            document.GeneratePdf(tempPath);

            Process.Start(new ProcessStartInfo
            {
                FileName = tempPath,
                Verb = "print",
                UseShellExecute = true,
                CreateNoWindow = true
            });
        }
    }
}
