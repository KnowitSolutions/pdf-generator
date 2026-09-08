using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Contracts;
using iText.Forms;
using iText.IO.Image;
using iText.IO.Source;
using iText.Kernel.Font;

using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using iText.Layout.Element;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Service
{
	public class DocumentController : ControllerBase
	{
		private readonly ILogger<DocumentController> _logger;

		public DocumentController(ILogger<DocumentController> logger)
		{
			_logger = logger;
		}

		[HttpPost, Route("api/document/merge")]
		public IActionResult Merge([FromBody] MergeRequest mergeRequest)
		{
			using var outputStream = new MemoryStream();
			using (var mergedDocument = new PdfDocument(new PdfWriter(outputStream, new WriterProperties().SetFullCompressionMode(true).UseSmartMode())))
			{
				var merger = new PdfMerger(mergedDocument);
				merger.SetCloseSourceDocuments(true);

				foreach (var documentData in mergeRequest.Documents)
				{
					var documentStream = new MemoryStream(Convert.FromBase64String(documentData.Base64Bytes));
					var document = new PdfDocument(new PdfReader(documentStream));
					// page numbers are 1-indexed, toPage is inclusive
					merger.Merge(document, 1, document.GetNumberOfPages());
				}
			}

			return Ok(Convert.ToBase64String(outputStream.ToArray()));
		}

		[HttpPost, Route("api/document/create")]
		public async Task<IActionResult> Create([FromBody] CreateRequest createRequest)
		{
			await using var outputStream = new ByteArrayOutputStream();
			try
			{
				Create(createRequest, outputStream);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error on creating PDF, exception was: {ex}");
				throw;
			}

			_logger.LogDebug($"Completed generation of document with DocumentGuid={createRequest.Document.Id}");
			return Ok(Convert.ToBase64String(outputStream.ToArray()));
		}

		[HttpPost, Route("api/document/parsefields")]
		public IActionResult ParseFields([FromBody] ParseFieldsRequest parseFieldsRequest)
		{
			var document = PdfUtilities.LoadPdf(Convert.FromBase64String(parseFieldsRequest.Base64Document));
			var fields = PdfUtilities.ParseFields(document);
			return Ok(fields);
		}

		private void Create(CreateRequest loadPdfRequest, ByteArrayOutputStream outputStream)
		{
			using var pdf = PdfUtilities.LoadPdf(Convert.FromBase64String(loadPdfRequest.Document.Base64Bytes), outputStream);

			//Applies global mappings first and then the local mappings
			var form = PdfAcroForm.GetAcroForm(pdf, false);
			var caseMap = form?.GetFormFields().Keys.ToDictionary(
				key => key.ToUpper(), key => key);


			// Ensure no null-values in form fields.
			// Makes Flattening sometimes crash for some reason
			if (form != null)
			{
				foreach (var field in form.GetFormFields().Select(x => x.Key))
				{
					if (form.GetField(field).GetValue() == null)
						form.GetField(field).SetValue(string.Empty);
				}
			}
			
			foreach (var global in loadPdfRequest.Globals)
			{
				if (caseMap != null && caseMap.TryGetValue(global.Item1.Name.ToUpper(), out var field))
				{
					var decodedImage = Convert.FromBase64String(global.Item2.Values.First().Base64Image);
					if (decodedImage.Length > 0)
					{
						ImageData image = ImageDataFactory.Create(decodedImage);
					}
					form.GetField(field).SetValue(global.Item2.Values.First().Text);

				}
			}

			var values = loadPdfRequest.Values;
			for (var index = 0; index < loadPdfRequest.Mappings.Count; index++)
			{
				try
				{
					var decodedImage = Convert.FromBase64String(values[index].Values[0].Base64Image);
					if (decodedImage.Length > 0)
					{
						_logger.LogDebug("Found image data of length " + decodedImage.Length);
						ImageData imageData = ImageDataFactory.Create(decodedImage);

						var fieldPlacement = form.GetField(loadPdfRequest.Mappings[index].Field).GetWidgets()[0].GetRectangle();
						float width = (float)(fieldPlacement.GetAsNumber(2).GetValue() - fieldPlacement.GetAsNumber(0).GetValue());
						float height = (float)(fieldPlacement.GetAsNumber(3).GetValue() - fieldPlacement.GetAsNumber(1).GetValue());

						Image image = new Image(imageData);
						image.SetMaxHeight(height);

						image.SetMaxWidth(width);
						image.SetFixedPosition((float)fieldPlacement.GetAsNumber(0).GetValue(), (float)fieldPlacement.GetAsNumber(1).GetValue());
						iText.Layout.Document doc = new iText.Layout.Document(pdf);
						doc.Add(image);
					}						


					//User defined font and fontsize.
					if (values[index].Values[0].Font != null)
					{
						string fontFamily = loadPdfRequest.Values[index].Values[0].Font;
						string fontPath = $"C:\\Windows\\Fonts\\{fontFamily}.ttf";
						PdfFont pdfFont = null;
						try
						{
							if (PdfFontFactory.IsRegistered(fontFamily))
							{
								pdfFont = PdfFontFactory.CreateRegisteredFont(fontFamily);
							}
							else if (System.IO.File.Exists(fontPath))
							{
								PdfFontFactory.Register(fontPath, fontFamily);
								pdfFont = PdfFontFactory.CreateRegisteredFont(fontFamily);
							}
						}

						catch (Exception e)
						{
							Debug.WriteLine(e);
							_logger.LogWarning($"Font \"{fontFamily}\" not found, Using default PDF font instead");
						}

						form.GetField(loadPdfRequest.Mappings[index].Field)?.SetValue(values[index].Values[0].Text, pdfFont, loadPdfRequest.Values[index].Values[0].FontSize);
					}

					// No font found, using default font size and : 
					else if (values[index].Values[0].Text != null)
						form.GetField(loadPdfRequest.Mappings[index].Field)?.SetValue(values[index].Values[0].Text);
				}
				catch (Exception)
				{
					_logger.LogWarning("Mapping with no corresponding acrofield found, skipping.");
				}
			}
			if (loadPdfRequest.Document.Optimize)
			{
				_logger.LogWarning($"Flatteing fields for document: {loadPdfRequest.Document.Name}");
				PdfAcroForm.GetAcroForm(pdf, true).FlattenFields();
			}
		}
	}
}
