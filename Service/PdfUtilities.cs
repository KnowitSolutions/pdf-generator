using System.Collections.Generic;
using System.IO;
using System.Linq;
using iText.Forms;
using iText.Kernel.Pdf;

namespace Service
{
	public static class PdfUtilities
    {
        /// <summary>
        ///     Create a new <see cref="PdfDocument" /> from a byte array. Must be closed after use.
        /// </summary>
        /// <param name="bytes">the document</param>
        /// <param name="outputStream">an optional output stream for the document, is closed when PdfDocument is closed</param>
        /// <returns>the PDF document</returns>
        public static PdfDocument LoadPdf(byte[] bytes, Stream? outputStream = null)
        {
            var inputStream = new MemoryStream(bytes);
            var reader = new PdfReader(inputStream);
            reader.SetUnethicalReading(true);
            return outputStream == null
                ? new PdfDocument(reader)
                : new PdfDocument(reader, new PdfWriter(outputStream));
        }

        public static IEnumerable<string> ParseFields(byte[] bytes)
        {
            using var document = LoadPdf(bytes);
            return ParseFields(document);
        }

        public static IEnumerable<string> ParseFields(PdfDocument document)
        {
            var form = PdfAcroForm.GetAcroForm(document, false);
            return form?.GetFormFields().Keys ?? Enumerable.Empty<string>();
        }
    }
}
