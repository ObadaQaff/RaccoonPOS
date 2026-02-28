using QuestPDF.Infrastructure;

namespace RaccoonWarehouse.Helpers.Pdf
{
    public interface IReportDocument : IDocument
    {
        string FileName { get; }
    }
}
