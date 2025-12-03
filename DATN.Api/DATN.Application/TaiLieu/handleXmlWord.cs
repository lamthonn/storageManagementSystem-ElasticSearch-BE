using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlAgilityPack;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Xsl;
using System.Xml;
using DocumentFormat.OpenXml.ExtendedProperties;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.Utils;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using DATN.Application.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace DATN.Application.TaiLieu
{
    public class OpenXmlPropertyString
    {
        public string name { get; set; }
        public string type { get; set; }
        public object value { get; set; }
    }
    public class OpenXmlElementString
    {
        public string name { get; set; }
        public byte[] bytes { get; set; }
        public List<OpenXmlPropertyString> properties { get; set; }
        public List<OpenXmlElementString> children { get; set; }
    }
    public class handleXmlWord
    {
        #region hàm phụ
        private static ImagePart? GetImagePartFromDrawing(MainDocumentPart mainPart, Drawing drawing)
        {
            // Lấy phần tử Inline hoặc Anchor
            var inline = drawing.Inline;
            if (inline == null)
                return null;

            // Truy cập đến Graphic
            var graphic = inline.Graphic;
            var graphicData = graphic?.GraphicData;
            if (graphicData == null)
                return null;

            // Tìm phần tử Picture
            var pic = graphicData.GetFirstChild<DocumentFormat.OpenXml.Drawing.Pictures.Picture>();
            if (pic == null)
                return null;

            // Lấy ID quan hệ (embed id)
            var blip = pic.BlipFill?.Blip;
            var embedId = blip?.Embed?.Value;
            if (string.IsNullOrEmpty(embedId))
                return null;

            // Lấy ImagePart từ embedId
            return mainPart.GetPartById(embedId) as ImagePart;
        }
        static string ExtractStyleValue(string style, string key)
        {
            var match = Regex.Match(style, $@"{key}\s*:\s*([^;]+);?");
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
        public static string ExtractImageInfo(DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline inline)
        {
            if (inline == null) return "";
            var ret = "";
            if (inline.Extent != null)
            {
                var ext = inline.Extent;
                if (ext.Cx.HasValue)
                    ret += ($"width: {(ext.Cx / 9525.0).ToString("#.##")}px;");
                else if (ext.Cy.HasValue) ret += ($"height: {(ext.Cy / 9525.0).ToString("#.##")}px;");
            }
            // Get wrap type (square, etc.)
            string floatStyle = "none";
            var wrapSquare = (inline.Parent as Drawing).Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapSquare>().FirstOrDefault();
            if (wrapSquare != null)
            {
                floatStyle = wrapSquare.WrapText?.Value switch
                {
                    DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapTextValues.BothSides => "left",
                    DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapTextValues.Right => "left",
                    DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapTextValues.Left => "right",
                    _ => "left"
                };
                ret += $"float:{floatStyle};";
            }
            return ret;
        }
        private static string GetRunPropertiesStyle(RunProperties rPr)
        {
            if (rPr == null || rPr.Spacing == null) return "";

            var spacing = int.Parse(rPr.Spacing.Val) / 20.0;
            return $"letter-spacing:{spacing}pt;";
        }
        public static bool isRuby(OpenXmlElement node)
        {

            if (node.Parent != null)
            {
                while (true)
                {
                    if (node.Parent == null)
                    {
                        return false;
                    }
                    if (node.Parent.LocalName != "ruby")
                    {
                        node = node.Parent;
                    }
                    else
                    {
                        return true;
                    }

                }
            }
            else
            {
                return false;
            }
        }
        public static bool isRt(OpenXmlElement node)
        {

            if (node.Parent != null)
            {
                while (true)
                {
                    if (node.Parent == null)
                    {
                        return false;
                    }
                    if (node.Parent.LocalName != "rt")
                    {
                        node = node.Parent;
                    }
                    else
                    {
                        return true;
                    }

                }
            }
            else
            {
                return false;
            }
        }
        public static (bool IsBold, bool IsItalic, bool IsUnderline, bool isSubscript, bool isSuperscript) GetRunStyle(DocumentFormat.OpenXml.Wordprocessing.Run run)
        {
            var props = run.RunProperties;
            if (props == null)
            {
                return (false, false, false, false, false);
            }

            // Bold
            bool isBold = props.Bold != null || (props.BoldComplexScript != null && props.BoldComplexScript.Val != null);

            // Italic
            bool isItalic = props.Italic != null || (props.ItalicComplexScript != null && props.ItalicComplexScript.Val != null);

            // Underline
            bool isUnderline = props.Underline?.Val?.Value != null;

            bool isSubscript = props.VerticalTextAlignment?.Val?.InnerText == "subscript";
            bool isSuperscript = props.VerticalTextAlignment?.Val?.InnerText == "superscript";

            return (isBold, isItalic, isUnderline, isSubscript, isSuperscript);
        }
        private static string GetParagraphStyle(ParagraphProperties pPr)
        {
            if (pPr == null || pPr.SpacingBetweenLines == null) return "";

            var spacing = pPr.SpacingBetweenLines;
            var before = spacing.Before != null ? int.Parse(spacing.Before) / 20.0 : 0;
            var after = spacing.After != null ? int.Parse(spacing.After) / 20.0 : 0;
            var line = spacing.Line != null ? int.Parse(spacing.Line) / 20.0 : 0;
            // tang them de hien thi tren HTML de nhin hon
            if (line > 0) line += 3;
            else line = 16;

            return $"margin-top:{before}pt; margin-bottom:{after}pt;";
        }
        public static byte[] LookupBytes(OpenXmlElementString elementString)
        {
            var bytes = new byte[] { };
            if (elementString.bytes != null && elementString.bytes.Length > 0) bytes = elementString.bytes;
            else
            {
                if (elementString.children != null)
                {
                    foreach (var ele in elementString.children)
                    {
                        if (bytes.Length > 0) break;
                        if (ele.bytes != null && ele.bytes.Length > 0)
                        {
                            bytes = ele.bytes;
                            break;
                        }
                        else bytes = LookupBytes(ele);
                    }
                }
            }
            return bytes;
        }
        private static string GetImageExtension(string contentType)
        {
            return contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/bmp" => ".bmp",
                _ => null,
            };
        }
        #endregion
        #region các hàm lưu ảnh
        //hàm trả về <outerHtml, base64>
        public static (string, List<Dictionary<string, string>>) CreateImgPath(string htmlContent, Microsoft.Extensions.Configuration.IConfiguration _config)
        {
            string rootPath = _config.GetSection("RootFileServer")["path"] ?? ""; //get cau hinh folder file server
            string userserver = _config.GetSection("RootFileServer")["username"] ?? "";
            string pwdserver = _config.GetSection("RootFileServer")["pwd"] ?? "";
            string drive = _config.GetSection("RootFileServer")["drive"] ?? "";
            string preview = _config.GetSection("RootFileServer")["preview"] ?? "";

            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            var imgNodes = htmlDoc.DocumentNode.SelectNodes("//img");
            var result = new List<Dictionary<string, string>>();

            if (imgNodes != null)
            {
                int index = 1;
                foreach (var img in imgNodes)
                {
                    try
                    {
                        if (img.Attributes["src"] != null)
                        {
                            var base64 = img.Attributes["src"].Value;

                            var bytes = Convert.FromBase64String(Regex.Split(base64, ";base64,")[1]);
                            var header = base64.Split(',')[0]; // "png;base64"
                            var extension = "";

                            // Nếu header có dạng "data:image/png;base64"
                            if (header.Contains("image/"))
                            {
                                var mime = header.Split(';')[0];          // "data:image/png"
                                var ext = mime.Split('/')[1];
                                if (ext == "x-wmf")
                                {
                                    ext = "wmf";
                                }
                                extension = "." + "png";                // ".png"
                            }
                            else
                            {
                                // Trường hợp bạn chỉ có "png;base64"
                                var ext = header.Split(';')[0];
                                if (ext == "x-wmf")
                                {
                                    ext = "wmf";
                                }
                                extension = "." + "png";                // ".png"
                            }

                            //tạo path ảnh
                            var rootDirectory = "import_nhch";
                            var timestamp = DateTime.Now.Ticks + Guid.NewGuid().ToString().Replace("-", "_");
                            var fileName = "image_drawing_" + index + "_" + timestamp + extension;

                            var filePath = System.IO.Path.Combine(rootPath, preview, rootDirectory, fileName);
                            var relativeFlePath = System.IO.Path.Combine(rootDirectory, fileName);

                            result.Add(new Dictionary<string, string>()
                        {
                            { "base64", base64 },
                            { "filePath", filePath }
                        });

                            // Change the src attribute
                            if (!string.IsNullOrEmpty(filePath))
                            {
                                //if (ObjectHasKey(x, "style") && x.style != "")
                                //  img.SetAttributeValue("src", "/api/file/tep-tin/get-file?downloadByte=false&filePath=" + filePath + "");
                                //else
                                img.SetAttributeValue("src", "/api/file/tep-tin/get-file?downloadByte=false&filePath=" + relativeFlePath + "");
                                img.SetAttributeValue("alt", filePath);
                                // Encode thẻ <img> thành chuỗi HTML an toàn
                                var encodedImg = img.OuterHtml;
                                // Tạo node mới chứa thẻ img đã encode dưới dạng text
                                var textNode = htmlDoc.CreateTextNode(encodedImg);
                                img.ParentNode.ReplaceChild(textNode, img);
                            }

                            index++;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (img.Attributes["src"].Value.IndexOf("png;base64,") == 0)
                            img.Attributes["src"].Value = "data:image/" + img.Attributes["src"].Value;
                        // htmlDoc.DocumentNode.InnerHtml += ex.Message;
                        index++;
                    }
                }
            }

            return (htmlDoc.DocumentNode.OuterHtml, result);
        }

        //hàm save list ảnhda
        // <Dictionary<string, string> là <outerHtml, base64>
        public static void SaveImgToServer(List<Dictionary<string, string>> imgDict, Microsoft.Extensions.Configuration.IConfiguration _config)
        {

            string rootPath = _config.GetSection("RootFileServer")["path"] ?? "";
            string userserver = _config.GetSection("RootFileServer")["username"] ?? "";
            string pwdserver = _config.GetSection("RootFileServer")["pwd"] ?? "";
            string drive = _config.GetSection("RootFileServer")["drive"] ?? "";
            string preview = _config.GetSection("RootFileServer")["preview"] ?? "";
            var rootDirectory = "import_nhch";

            NetworkDrive nd = new NetworkDrive();
            nd.MapNetworkDrive(rootPath, drive, userserver, pwdserver);

            foreach (var item in imgDict)
            {
                var filePath = item["filePath"];
                var base64 = item["base64"];
                var bytes = Convert.FromBase64String(Regex.Split(base64, ";base64,")[1]);
                var targetDriver = Path.Combine(rootPath, preview, rootDirectory);

                try
                {
                    if (!Directory.Exists(targetDriver))
                    {
                        Directory.CreateDirectory(targetDriver);
                    }
                    var maxWidth = 700;

                    using (var ms = new MemoryStream(bytes))
                    using (var originalImage = SixLabors.ImageSharp.Image.Load<Rgba32>(ms)) // ✅ ImageSharp
                    {
                        int width = originalImage.Width;
                        int height = originalImage.Height;

                        try
                        {
                            // Check if resizing is needed
                            if (width > maxWidth)
                            {
                                float scale = (float)maxWidth / width;
                                int newWidth = maxWidth;
                                int newHeight = (int)(height * scale);

                                originalImage.Mutate(x => x.Resize(newWidth, newHeight));
                            }
                            else if (height > maxWidth)
                            {
                                float scale = (float)maxWidth / height;
                                int newHeight = maxWidth;
                                int newWidth = (int)(width * scale);

                                originalImage.Mutate(x => x.Resize(newWidth, newHeight));
                            }

                            // Lưu ảnh
                            originalImage.Save(filePath, new PngEncoder());
                            var outputFile = HybridEncryption.EncryptFileToStoring(filePath, targetDriver, "pvECCLocal", "pbECCLocal");
                            // xóa file cũ chưa mã hóa
                            if(System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }
                        catch
                        {
                            originalImage.Save(filePath, new PngEncoder());
                            var outputFile = HybridEncryption.EncryptFileToStoring(filePath, targetDriver, "pvECCLocal", "pbECCLocal");
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }

                    }
                }
                catch
                {
                    try
                    {
                        var header = base64.Split(',')[0]; // "png;base64"
                        var extendtion = ".emf";
                        // Nếu header có dạng "data:image/png;base64"
                        if (header.Contains("image/"))
                        {
                            var mime = header.Split(';')[0];          // "data:image/png"
                            var ext = mime.Split('/')[1];
                            if (ext == "x-wmf")
                            {
                                extendtion = ".wmf";
                            }
                            else if (ext == "x-emf")
                            {
                                extendtion = ".emf";
                            }
                        }
                        else
                        {
                            // Trường hợp bạn chỉ có "png;base64"
                            var ext = header.Split(';')[0];
                            if (ext == "x-wmf")
                            {
                                extendtion = ".wmf";
                            }
                            else if (ext == "x-emf")
                            {
                                extendtion = ".emf";
                            }
                        }

                        // Lưu bytes gốc ra file tạm để Inkscape có thể đọc
                        System.IO.File.WriteAllBytes(filePath, bytes);

                        // Tạo file output PNG
                        var tempPngFile = Path.ChangeExtension(filePath, ".temp.png");
                        var originalFile = Path.ChangeExtension(filePath, extendtion);
                        System.IO.File.WriteAllBytes(originalFile, bytes);

                        // Convert WMF sang PNG bằng Inkscape
                        var process = new Process();
                        process.StartInfo.FileName = "inkscape";
                        process.StartInfo.Arguments = $"\"{originalFile}\" --export-type=png --export-filename=\"{tempPngFile}\" --export-dpi=300 --export-area-drawing";
                        //process.StartInfo.FileName = "libreoffice";
                        //process.StartInfo.Arguments = $"--headless --convert-to png \"{originalFile}\" --outdir \"{Path.GetDirectoryName(tempPngFile)}\"";
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.Start();
                        process.WaitForExit();

                        if (process.ExitCode == 0 && System.IO.File.Exists(tempPngFile))
                        {
                            // Đọc file PNG vừa convert và resize nếu cần
                            using (var convertedImage = SixLabors.ImageSharp.Image.Load<Rgba32>(tempPngFile))
                            {
                                int width = convertedImage.Width;
                                int height = convertedImage.Height;
                                var maxWidth = 700;

                                if (width > maxWidth)
                                {
                                    float scale = (float)maxWidth / width;
                                    int newWidth = maxWidth;
                                    int newHeight = (int)(height * scale);
                                    convertedImage.Mutate(x => x.Resize(newWidth, newHeight));
                                }
                                else if (height > maxWidth)
                                {
                                    float scale = (float)maxWidth / height;
                                    int newHeight = maxWidth;
                                    int newWidth = (int)(width * scale);
                                    convertedImage.Mutate(x => x.Resize(newWidth, newHeight));
                                }

                                // Xóa file WMF gốc và file PNG tạm
                                System.IO.File.Delete(filePath);
                                System.IO.File.Delete(tempPngFile);

                                // Lưu file PNG cuối cùng với tên đúng
                                var finalPngPath = Path.ChangeExtension(filePath, ".png");
                                convertedImage.Save(finalPngPath, new PngEncoder());
                                filePath = finalPngPath;
                            }
                        }
                        else
                        {
                            //continue;
                            // Cleanup nếu thất bại
                            if (System.IO.File.Exists(tempPngFile)) System.IO.File.Delete(tempPngFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        //continue;
                        //throw new Exception($"Fallback Inkscape failed: {ex.Message}");
                    }
                }
            }
        }

        static string SaveImageDrawing(DocumentFormat.OpenXml.Wordprocessing.Drawing drawing, int sttImage, Microsoft.Extensions.Configuration.IConfiguration _config, string type)
        {
            var blip = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
            if (blip?.Embed == null) return null;

            var part = (ImagePart)drawing.Ancestors<DocumentFormat.OpenXml.Wordprocessing.Document>().First().MainDocumentPart!.GetPartById(blip.Embed);

            string rootPath = _config.GetSection("RootFileServer")["path"] ?? ""; //get cau hinh folder file server
            string userserver = _config.GetSection("RootFileServer")["username"] ?? "";
            string pwdserver = _config.GetSection("RootFileServer")["pwd"] ?? "";
            string drive = _config.GetSection("RootFileServer")["drive"] ?? "";
            NetworkDrive nd = new NetworkDrive();

            string secret = _config.GetSection("RootFileServer")["secret"] ?? "";

            nd.MapNetworkDrive(rootPath, drive, userserver, pwdserver);

            var rootDirectory = "import_nhch";
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = type + "_image_drawing_" + sttImage + "_" + timestamp + ".jpg";
            //var fileName = formFile.FileName ?? ""
            var filePath = System.IO.Path.Combine(rootPath, secret, rootDirectory, fileName);

            DateTime fileTime = DateTime.MinValue;

            var targetDriver = Path.Combine(rootPath, secret, rootDirectory);
            if (!Directory.Exists(targetDriver))
            {
                Directory.CreateDirectory(targetDriver);
            }

            using (var stream = part.GetStream())
            {
                // Lưu file vào thư mục đích
                using (var stream2 = new FileStream(filePath, FileMode.Create))
                {
                    stream.CopyTo(stream2);
                }

                // ma hoa
                var oldFilePath = filePath;
                var appCode = _config.GetSection("AppCode").Get<string>().ToString();
               
                return ("/" + rootDirectory + "/" + Path.GetFileName(filePath)).Replace("//", "/");
                //return (filePath);
            }
        }

        static string ConvertImageToBase64(DocumentFormat.OpenXml.Wordprocessing.Drawing drawing)
        {
            var blip = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
            if (blip?.Embed == null) return null;

            var part = (ImagePart)drawing.Ancestors<DocumentFormat.OpenXml.Wordprocessing.Document>().First().MainDocumentPart.GetPartById(blip.Embed);
            using (var stream = part.GetStream())
            {
                byte[] imageBytes = new byte[stream.Length];
                stream.Read(imageBytes, 0, imageBytes.Length);
                return Convert.ToBase64String(imageBytes);
            }
        }
        #endregion


        #region parse from xml to html
        //parse paragraph to html
        private static void ParseParagraph(MainDocumentPart mainPart, Paragraph para, int paramLevel, StringBuilder sbHTML, Dictionary<string, byte[]> _dicImageByte)
        {
            var props = para.ParagraphProperties;
            var paraInnerText = para.InnerText.Trim();

            //text align
            string alignmentStyle = "";
            string textIndentStyle = "";
            if (props?.Justification?.Val?.Value != null)
            {
                var justification = props.Justification.Val.Value.ToString().ToLower();
                switch (justification)
                {
                    case "left":
                        alignmentStyle = "text-align: left;";
                        break;
                    case "right":
                        alignmentStyle = "text-align: right;";
                        break;
                    case "center":
                        alignmentStyle = "text-align: center;";
                        break;
                    case "both":
                        alignmentStyle = "text-align: justify;";
                        break;
                }
            }
            //props.PageBreakBefore
            //props.ParagraphBorders
            string textFontStyle = "";
            var fontFamilyProp = para?.Descendants<Run>().FirstOrDefault()?.RunProperties?.RunFonts;
            var fontFamilyProp2 = para?.Descendants<RunProperties>();
            var fontSizeProp = props?.Descendants<FontSize>().FirstOrDefault();
            var fontSizeCSProp = props?.Descendants<FontSizeComplexScript>().FirstOrDefault();
            if (fontSizeCSProp == null)
            {
                fontSizeCSProp = para.Descendants<FontSizeComplexScript>().FirstOrDefault();
                if (fontSizeCSProp == null && fontSizeProp == null)
                {
                    fontSizeCSProp = new FontSizeComplexScript();
                    fontSizeCSProp.Val = "24"; // default font
                }
            }
            if (fontFamilyProp != null && fontSizeCSProp != null && ((fontFamilyProp.ComplexScript + "").ToLower().Contains("times new roman") || (fontFamilyProp.ComplexScript + "").ToLower().Contains("arial")) && fontSizeProp == null)
            {
                fontSizeProp = new FontSize();
                fontSizeProp.Val = "24"; // default font
                if (props != null)
                {
                    var runProps = props.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.RunProperties>();
                    if (runProps != null)
                    {
                        runProps.FontSize = new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = fontSizeProp.Val };
                        fontSizeCSProp.Val = fontSizeProp.Val;
                    }
                    else
                    {
                        var runMarkProps = props.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.ParagraphMarkRunProperties>();
                        if (runMarkProps != null)
                        {
                            runMarkProps.PrependChild(new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = fontSizeProp.Val });
                            fontSizeCSProp.Val = fontSizeProp.Val;
                        }
                    }
                }
            }
            if (fontFamilyProp != null)
            {
                if (fontFamilyProp.Ascii == "SimSun" || fontFamilyProp.ComplexScript == "SimSun" || fontFamilyProp.EastAsia == "SimSun")
                {
                    textFontStyle += $"font-family: SimSun;";
                }
                else if (fontFamilyProp.Ascii == "MS Mincho" || fontFamilyProp.ComplexScript == "MS Mincho" || fontFamilyProp.EastAsia == "MS Mincho")
                {
                    textFontStyle += $"font-family: Noto Serif JP;";
                }
                else
                {
                    textFontStyle += $"font-family: {fontFamilyProp.Ascii ?? fontFamilyProp.ComplexScript ?? fontFamilyProp.EastAsia ?? "Times New Roman"};";
                }
            }
            else if (fontFamilyProp2 != null)
            {
                var runFontPropSimSun = fontFamilyProp2.Any(x => x.RunFonts?.Ascii == "SimSun" || x.RunFonts?.ComplexScript == "SimSun" || x.RunFonts?.EastAsia == "SimSun");
                var runFontPropMSMincho = fontFamilyProp2.Any(x => x.RunFonts?.Ascii == "MS Mincho" || x.RunFonts?.ComplexScript == "MS Mincho" || x.RunFonts?.EastAsia == "MS Mincho");
                var runFontPropDefault = fontFamilyProp2.FirstOrDefault()?.RunFonts?.Ascii ?? fontFamilyProp2.FirstOrDefault()?.RunFonts?.ComplexScript ?? fontFamilyProp2.FirstOrDefault()?.RunFonts?.EastAsia;

                if (runFontPropSimSun)
                {
                    textFontStyle += $"font-family: SimSun;";
                }
                else if (runFontPropMSMincho)
                {
                    textFontStyle += $"font-family: Noto Serif JP;";
                }
                else
                {
                    textFontStyle += $"font-family: {runFontPropDefault ?? "Times New Roman"};";
                }
            }
            if (fontSizeProp != null && !textFontStyle.Contains("font-size"))
            {
                textFontStyle += $"font-size: {(int.Parse(fontSizeProp.Val) / 2)}pt;"; //  * 96 / 72
            }
            else
            {
                fontSizeProp = para.Descendants<FontSize>().FirstOrDefault();
                if (fontSizeProp != null && !textFontStyle.Contains("font-size"))
                {
                    textFontStyle += $"font-size: {(int.Parse(fontSizeProp.Val) / 2)}pt;"; //  * 96 / 72
                                                                                           //textFontStyle += $"font-size: {(int.Parse(fontSizeProp.Val) / 2)}px;"; //  * 96 / 72
                }
            }
            if (fontSizeCSProp != null && !textFontStyle.Contains("font-size"))
            {
                textFontStyle += $"font-size: {(int.Parse(fontSizeCSProp.Val) / 2)}pt;"; //  * 96 / 72
                                                                                         //textFontStyle += $"font-size: {(int.Parse(fontSizeCSProp.Val) / 2)}px;"; //  * 96 / 72
            }
            else
            {
                fontSizeCSProp = para.Descendants<FontSizeComplexScript>().FirstOrDefault();
                if (fontSizeCSProp != null && !textFontStyle.Contains("font-size"))
                {
                    textFontStyle += $"font-size: {(int.Parse(fontSizeCSProp.Val) / 2)}pt;"; //  * 96 / 72
                                                                                             //textFontStyle += $"font-size: {(int.Parse(fontSizeCSProp.Val) / 2)}px;"; //  * 96 / 72
                }
            }

            //text-indent
            if (props?.Indentation != null && !paraInnerText.StartsWith("A.") && !paraInnerText.StartsWith("B.") && !paraInnerText.StartsWith("C.") && !paraInnerText.StartsWith("D."))
            {
                var textIndent = props.Indentation?.FirstLine;
                if (textIndent != null)
                {
                    var textIndentPx = int.Parse("0" + textIndent) / 30;
                    textIndentStyle = $"text-indent: {textIndentPx}px; word-indent: {textIndent}px;";
                }
            }

            string textSpacingStyle = "";
            string textNumberStyle = "";
            if (para != null && para.ParagraphProperties != null)
            {
                foreach (var prop in para?.ParagraphProperties)
                {
                    if (prop is DocumentFormat.OpenXml.Wordprocessing.SpacingBetweenLines)
                    {
                    }
                    if (prop is DocumentFormat.OpenXml.Wordprocessing.NumberingProperties numPr)
                    {
                        // numPr.NumberingId.Val mà == 0 là không phải list
                        if (numPr.NumberingId != null && numPr.NumberingId.Val + "" != "" && numPr.NumberingId.Val != 0)
                            textNumberStyle = "text-number: " + numPr.NumberingId.Val + ";";
                    }
                }
            }
            textSpacingStyle += GetParagraphStyle(props);

            if (true)
            {
                foreach (var runProp in para.Descendants<DocumentFormat.OpenXml.Wordprocessing.RunProperties>())
                {
                    var propFontSize = runProp.Descendants<DocumentFormat.OpenXml.Wordprocessing.FontSize>().FirstOrDefault();
                    var propFontSizeCS = runProp.Descendants<DocumentFormat.OpenXml.Wordprocessing.FontSizeComplexScript>().FirstOrDefault();
                    if (propFontSize == null && fontSizeProp != null)
                    {
                        runProp.FontSize = new FontSize();
                        runProp.FontSize.Val = fontSizeProp.Val;
                    }
                    if (propFontSizeCS != null)
                    {
                        propFontSizeCS.Val = propFontSizeCS.Val;
                    }
                }

                foreach (var text in para.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>())
                {
                    var textNode = text.Text;
                    if (Regex.IsMatch(textNode, @"\$(.+?)\$"))
                    {
                        text.Text = Regex.Replace(textNode, @"\$(.+?)\$", m => $"${{{m.Groups[1].Value}}}$");
                    }
                }


            }

            //dataDocxAttr = "";
            // Append <p> tag with alignment style if present
            if (!string.IsNullOrEmpty(alignmentStyle) || !string.IsNullOrEmpty(textIndentStyle) || !string.IsNullOrEmpty(textSpacingStyle) || !string.IsNullOrEmpty(textNumberStyle) || !string.IsNullOrEmpty(textFontStyle))
            {
                sbHTML.Append($"<p style='{alignmentStyle ?? ""}{textIndentStyle ?? ""}{textSpacingStyle ?? ""}{textNumberStyle ?? ""}{textFontStyle ?? ""}'>");
            }
            else
            {
                sbHTML.Append($"<p>");
            }
            if (textNumberStyle != "")
                sbHTML.Append($"<li style='{alignmentStyle ?? ""}{textIndentStyle ?? ""}{textSpacingStyle ?? ""}{textNumberStyle ?? ""}{textFontStyle ?? ""}'>");

            var prevTag = "";
            bool isBold = false;
            bool isItalic = false;
            bool isUnderline = false;
            bool isSubscript = false;
            bool isSuperscript = false;
            string fieldCodeRuby = "";

            // anh phong xem sua lai ChildNodes
            foreach (var element in para.Descendants())
            {
                if (element is Run run)
                {
                    var runStyle = GetRunStyle(run);
                    isBold = runStyle.IsBold;
                    isItalic = runStyle.IsItalic;
                    isUnderline = runStyle.IsUnderline;
                    isSubscript = runStyle.isSubscript;
                    isSuperscript = runStyle.isSuperscript;
                    string fontName = run?.RunProperties?.RunFonts?.Ascii?.Value ?? "";
                    var fontSize = run?.RunProperties?.FontSize?.Val + "";
                    bool isCourier = fontName.Equals("Courier New", StringComparison.OrdinalIgnoreCase);

                    foreach (var ele in run.Elements())
                    {
                        if (ele is DocumentFormat.OpenXml.Wordprocessing.TabChar)
                        {
                            //sbHTML.Append("   "); // hoặc "    "
                            sbHTML.Append("\t");
                        }
                        else if (ele is DocumentFormat.OpenXml.Wordprocessing.Break)
                        {
                            sbHTML.Append($"</p><p style='{alignmentStyle ?? ""}{textIndentStyle ?? ""}{textSpacingStyle ?? ""}{textNumberStyle ?? ""}{textFontStyle ?? ""}'>");
                            // sbHTML.Append($"<br />");
                        }
                        else if (ele is DocumentFormat.OpenXml.Wordprocessing.Text textNode)
                        {
                            if (textNode != null)
                            {
                                var textString = textNode.Text;
                                if (textString == " ")
                                {
                                    sbHTML.Append($"&nbsp;");
                                    continue;
                                }
                                if (isRuby(ele) || isRt(ele))
                                {
                                    textString = "";
                                }
                                if (isCourier)
                                {
                                    if (!textString.Contains("<code>"))
                                    {
                                        textString = $"<code>{HtmlDocument.HtmlEncode(textString).Replace(" ", "[space]")}</code>";
                                    }
                                }

                                // Close previous tags if necessary
                                if (prevTag != "" &&
                                    ((prevTag.Contains("strong") && !isBold) ||
                                     (prevTag.Contains("i") && !isItalic) ||
                                     (prevTag.Contains("u") && !isUnderline) ||
                                     (prevTag.Contains("sub") && !isSubscript) ||
                                     (prevTag.Contains("sup") && !isSuperscript)
                                    ))
                                {
                                    // Close tags in reverse order: u, i, strong
                                    if (prevTag.Contains("u")) sbHTML.Append("</u>");
                                    if (prevTag.Contains("i")) sbHTML.Append("</i>");
                                    if (prevTag.Contains("strong")) sbHTML.Append("</strong>");
                                    if (prevTag.Contains("sub")) sbHTML.Append("</sub>");
                                    if (prevTag.Contains("sup")) sbHTML.Append("</sup>");
                                    prevTag = "";
                                }

                                // Determine new tag combination
                                string newTag = "";
                                if (isBold) newTag += "strong";
                                if (isItalic) newTag += "-i";
                                if (isUnderline) newTag += "-u";
                                if (isSubscript) newTag += "-sub";
                                if (isSuperscript) newTag += "-sup";

                                // Open new tags if necessary
                                if (newTag != prevTag && newTag != "")
                                {
                                    // Close any existing tags
                                    if (prevTag != "")
                                    {
                                        if (prevTag.Contains("u")) sbHTML.Append("</u>");
                                        if (prevTag.Contains("i")) sbHTML.Append("</i>");
                                        if (prevTag.Contains("strong")) sbHTML.Append("</strong>");
                                        if (prevTag.Contains("sub")) sbHTML.Append("</sub>");
                                        if (prevTag.Contains("sup")) sbHTML.Append("</sup>");
                                    }

                                    // Open new tags in order: strong, i, u
                                    if (isBold) sbHTML.Append($"<strong style=\"{textFontStyle}\">");
                                    if (isItalic) sbHTML.Append($"<i style=\"{textFontStyle}\">");
                                    if (isUnderline) sbHTML.Append($"<u style=\"{textFontStyle}\">");
                                    if (isSubscript)
                                    {
                                        try
                                        {
                                            if (fontSizeProp != null)
                                            {
                                                sbHTML.Append($"<sub style=\"font-size: {(int.Parse(fontSizeProp.Val) / 2) - 2}pt;\">");
                                            }
                                            else
                                            {
                                                if (fontSizeCSProp != null)
                                                {
                                                    sbHTML.Append($"<sub style=\"font-size: {(int.Parse(fontSizeCSProp.Val) / 2) - 2}pt;\">");
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            sbHTML.Append($"<sub style=\"{textFontStyle}\">");
                                        }
                                    }
                                    if (isSuperscript)
                                    {
                                        try
                                        {
                                            if (fontSizeProp != null)
                                            {
                                                sbHTML.Append($"<sup style=\"font-size: {(int.Parse(fontSizeProp.Val) / 2) - 2}pt;\">");
                                            }
                                            else
                                            {
                                                if (fontSizeCSProp != null)
                                                {
                                                    sbHTML.Append($"<sup style=\"font-size: {(int.Parse(fontSizeCSProp.Val) / 2) - 2}pt;\">");
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            sbHTML.Append($"<sup style=\"{textFontStyle}\">");
                                        }
                                    }
                                    prevTag = newTag;
                                }
                                else if (newTag == "")
                                {
                                    prevTag = "";
                                }
                                var fontSizeStr = "";
                                var fontFamily = "";
                                var spacingRunStyle = "";
                                var runFonts = run.Descendants<RunFonts>().FirstOrDefault();
                                if (runFonts != null && (runFonts.Ascii != null || runFonts.EastAsia != null)) fontFamily = $"font-family:{runFonts.Ascii?.Value ?? runFonts.EastAsia?.Value ?? "Times New Roman"} ";
                                var runFontSize = run.Descendants<FontSize>().FirstOrDefault();
                                if (runFontSize != null) fontSizeStr = $"font-size:{(int.Parse(runFontSize.Val) / 2) * 96 / 72}px;";
                                var runProperties = run.Descendants<RunProperties>().FirstOrDefault();
                                if (runProperties != null) spacingRunStyle = GetRunPropertiesStyle(runProperties);
                                if (fontSizeStr + fontFamily + spacingRunStyle != "" && false)
                                    sbHTML.Append($"<span style=\"{fontSizeStr}{fontFamily}{spacingRunStyle}\">{textString}</span>".Replace("\0", ""));
                                else
                                    sbHTML.Append($"{textString}".Replace("\0", ""));
                            }
                        }
                        // Xử lý chú thích tiếng Nhật
                        else if (ele is DocumentFormat.OpenXml.Wordprocessing.Ruby ruby)
                        {
                            string baseText = "";
                            string rtText = "";

                            // Lấy nội dung trong <w:rubyBase>
                            var rubyBase = ruby.RubyBase;
                            if (rubyBase != null)
                            {
                                baseText = string.Concat(rubyBase.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
                            }

                            // Lấy nội dung trong <w:rt>
                            var rt = ruby.RubyContent;
                            if (rt != null)
                            {
                                rtText = string.Concat(rt.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
                            }

                            // Tạo chuỗi HTML thay thế
                            string rubyHtml = $"<ruby>{baseText}<rt>{rtText}</rt></ruby>";
                            var textString = rubyHtml;

                            // Close previous tags if necessary
                            if (prevTag != "" &&
                                ((prevTag.Contains("strong") && !isBold) ||
                                 (prevTag.Contains("i") && !isItalic) ||
                                 (prevTag.Contains("u") && !isUnderline) ||
                                 (prevTag.Contains("sub") && !isSubscript) ||
                                 (prevTag.Contains("sup") && !isSuperscript)
                                ))
                            {
                                // Close tags in reverse order: u, i, strong
                                if (prevTag.Contains("u")) sbHTML.Append("</u>");
                                if (prevTag.Contains("i")) sbHTML.Append("</i>");
                                if (prevTag.Contains("strong")) sbHTML.Append("</strong>");
                                if (prevTag.Contains("sub")) sbHTML.Append("</sub>");
                                if (prevTag.Contains("sup")) sbHTML.Append("</sup>");
                                prevTag = "";
                            }

                            // Determine new tag combination
                            string newTag = "";
                            if (isBold) newTag += "strong";
                            if (isItalic) newTag += "-i";
                            if (isUnderline) newTag += "-u";
                            if (isSubscript) newTag += "-sub";
                            if (isSuperscript) newTag += "-sup";

                            // Open new tags if necessary
                            if (newTag != prevTag && newTag != "")
                            {
                                // Close any existing tags
                                if (prevTag != "")
                                {
                                    if (prevTag.Contains("u")) sbHTML.Append("</u>");
                                    if (prevTag.Contains("i")) sbHTML.Append("</i>");
                                    if (prevTag.Contains("strong")) sbHTML.Append("</strong>");
                                    if (prevTag.Contains("sub")) sbHTML.Append("</sub>");
                                    if (prevTag.Contains("sup")) sbHTML.Append("</sup>");
                                }

                                // Open new tags in order: strong, i, u
                                if (isBold) sbHTML.Append("<strong>");
                                if (isItalic) sbHTML.Append("<i>");
                                if (isUnderline) sbHTML.Append("<u>");
                                if (isSubscript) sbHTML.Append("<sub>");
                                if (isSuperscript) sbHTML.Append("<sup>");
                                prevTag = newTag;
                            }
                            else if (newTag == "")
                            {
                                prevTag = "";
                            }
                            sbHTML.Append($"{textString}".Replace("\0", ""));
                        }
                        // anh phong xem sua lai 
                        else if (ele is DocumentFormat.OpenXml.Wordprocessing.FieldCode ruby2)
                        {
                            //fieldCodeRuby += ruby2.InnerText;
                            sbHTML.Append("RUBYFCODE");
                        }
                        //
                        else
                        {
                            var drawing = ele.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>().FirstOrDefault();
                            if (drawing != null)
                            {
                                bool isImage = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().Any();

                                if (!isImage)
                                {
                                    sbHTML.Append("[DRAWING]");
                                }
                                else
                                {
                                    var imageStyle = "DRAWING:1;display: inline-flex;" + ExtractImageInfo(drawing.Inline);
                                    var anchor = drawing.Anchor;
                                    if (anchor != null)
                                    {
                                        long cx = anchor?.Extent?.Cx;
                                        long cy = anchor?.Extent?.Cy;

                                        // Chuyển đổi từ EMUs sang pixel (1 inch = 96 dpi, 1 inch = 914400 EMUs)
                                        int widthPx = (int)(cx / 9525);   // 914400 / 96 ≈ 9525
                                        int heightPx = (int)(cy / 9525);

                                        imageStyle += $"width: {widthPx}px;height: {heightPx}px;";
                                    }

                                    // Get wrap type (square, etc.)
                                    string floatStyle = "none";
                                    var wrapSquare = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapSquare>().FirstOrDefault();
                                    if (wrapSquare != null)
                                    {
                                        var hPos = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.HorizontalPosition>().FirstOrDefault();

                                        if (hPos != null)
                                        {
                                            if (hPos.RelativeFrom == "column" && int.Parse("" + hPos.PositionOffset.Text) > 11906016 / 4) // A4 EMUS
                                            {
                                                floatStyle = "right";
                                                imageStyle += $"float:{floatStyle};margin:3px;";
                                            }
                                        }
                                        else
                                        {
                                            floatStyle = wrapSquare.WrapText?.Value switch
                                            {
                                                DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapTextValues.BothSides => "left",
                                                DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapTextValues.Right => "left",
                                                DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapTextValues.Left => "right",
                                                _ => "left"
                                            };
                                            imageStyle += $"float:{floatStyle};margin:3px;";
                                        }
                                    }
                                    else
                                    {
                                        var shapeStyle = drawing.Descendants<DocumentFormat.OpenXml.Vml.Shape>().FirstOrDefault();
                                        imageStyle += shapeStyle?.Style ?? "";
                                    }

                                    var srcRect = drawing.Descendants<DocumentFormat.OpenXml.Drawing.SourceRectangle>().FirstOrDefault();
                                    if (srcRect != null)
                                    {
                                        int left = int.TryParse(srcRect.Left, out var l) ? l : 0;
                                        int top = int.TryParse(srcRect.Top, out var t) ? t : 0;
                                        int right = int.TryParse(srcRect.Right, out var r) ? r : 0;
                                        int bottom = int.TryParse(srcRect.Bottom, out var b) ? b : 0;

                                        // Original image dimensions in EMUs (example: from <wp:extent cx="..." cy="...">)
                                        long originalCx = 9144000; // width in EMUs (10 inches as example)
                                        long originalCy = 6858000; // height in EMUs (7.5 inches as example)

                                        // Compute visible area
                                        long visibleCx = originalCx - left - right;
                                        long visibleCy = originalCy - top - bottom;

                                        // Convert to percentage for CSS
                                        float cropLeftPct = (float)left / originalCx * 100;
                                        float cropTopPct = (float)top / originalCy * 100;
                                        float cropRightPct = (float)right / originalCx * 100;
                                        float cropBottomPct = (float)bottom / originalCy * 100;

                                        if (left + top + right + bottom != 0)
                                        {
                                            imageStyle += $"object-fit: cover; " +
                                                           $"widthx: 100%; height: auto; " +
                                                           $"clip-path: inset({cropTopPct}% {cropRightPct}% {cropBottomPct}% {cropLeftPct}%);";
                                        }
                                    }

                                    var blip = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
                                    if (blip == null || !blip.Embed.HasValue) continue;

                                    var imagePartId = blip.Embed.Value;
                                    var imagePart = (ImagePart)mainPart.GetPartById(imagePartId);
                                    // Lấy content type (vd: "image/png", "image/jpeg", "image/gif")
                                    string contentType = imagePart.ContentType;

                                    // Lấy extension (vd: "png", "jpg", "gif")
                                    string extension = contentType.Split('/').Last();
                                    try
                                    {
                                        using (var stream = imagePart.GetStream())
                                        using (var ms = new MemoryStream())
                                        {
                                            stream.CopyTo(ms);
                                            //CropImageBySrcRect
                                            string base64Image = Convert.ToBase64String(ms.ToArray());
                                            if (!string.IsNullOrEmpty(imageStyle))
                                                sbHTML.Append($"<img style='{imageStyle}' src='{extension};base64,{base64Image}'>");
                                            else
                                                sbHTML.Append($"<img src='{extension};base64,{base64Image}'>");
                                        }
                                    }
                                    catch
                                    {
                                        //CropImageBySrcRect
                                        string base64Image = Convert.ToBase64String(_dicImageByte[imagePartId]);
                                        if (!string.IsNullOrEmpty(imageStyle))
                                            sbHTML.Append($"<img  style='{imageStyle}' src='{extension};base64,{base64Image}'>");
                                        else
                                            sbHTML.Append($"<img  src='{extension};base64,{base64Image}'>");
                                    }
                                }
                            }
                            else
                            {
                                if (ele is not DocumentFormat.OpenXml.Wordprocessing.Drawing && ele.InnerText != "")
                                    sbHTML.Append(ele + " : [" + ele.InnerText + "]");
                            }
                        }
                    }
                    //if (fontSize + "" != "")
                    //    sbHTML.Append($"<span>");
                }
                else if (element is DocumentFormat.OpenXml.Math.OfficeMath math)
                {
                    var latex = ImportNHCHelper.ConvertOfficeMathToLatex(math.OuterXml);
                    sbHTML.Append($"<span style='EQUATION:1'></span>{latex}");
                }
                else if (element is EmbeddedObject oleObject)
                {
                    if (oleObject != null)
                    {
                        bool isImage = oleObject.Descendants<DocumentFormat.OpenXml.Vml.ImageData>().Any();
                        bool isMathType = oleObject.Descendants<DocumentFormat.OpenXml.Vml.Formulas>().Any();
                        if (!isMathType)
                        {
                            isMathType = oleObject
                                .Descendants<DocumentFormat.OpenXml.Vml.Office.OleObject>()
                                .Any(x =>
                                    x.ProgId?.Value != null &&
                                    (
                                        x.ProgId.Value.ToLower().Contains("equation") ||
                                        x.ProgId.Value.ToLower().Contains("mathtype")
                                    )
                                );
                        }

                        if (!isImage)
                        {
                            sbHTML.Append("[MATH-TYPE]");
                        }
                        else
                        {
                            if (isMathType == true)
                            {
                                sbHTML.Append("[MATH-TYPE]");
                            }
                            var imageStyle = "OLE:1;display: inline-flex;"; // ExtractImageInfo(oleObject.Descendants<DocumentFormat.OpenXml.Vml.ImageData>().FirstOrDefault());

                            //check width ảnh
                            var rect = oleObject.Descendants<DocumentFormat.OpenXml.Vml.Rectangle>().FirstOrDefault();
                            if (rect != null && rect.Style != null)
                            {
                                string style = rect.Style.Value;
                                double? widthPt = null, heightPt = null;

                                var parts = style.Split(';', StringSplitOptions.RemoveEmptyEntries);
                                var styleString = string.Join(";", parts);

                                imageStyle += $"{styleString};";
                            }


                            // Get wrap type (square, etc.)
                            string floatStyle = "none";
                            var wrapSquare = oleObject.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapSquare>().FirstOrDefault();
                            if (wrapSquare != null)
                            {
                                var hPos = oleObject.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.HorizontalPosition>().FirstOrDefault();

                                if (hPos != null)
                                {
                                    if (hPos.RelativeFrom == "column" && int.Parse("" + hPos.PositionOffset.Text) > 11906016 / 4) // A4 EMUS
                                    {
                                        floatStyle = "right";
                                        imageStyle += $"float:{floatStyle};margin:3px;";
                                    }
                                }
                                else
                                {
                                    floatStyle = wrapSquare.WrapText?.Value switch
                                    {
                                        DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapTextValues.BothSides => "left",
                                        DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapTextValues.Right => "left",
                                        DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapTextValues.Left => "right",
                                        _ => "left"
                                    };
                                    imageStyle += $"float:{floatStyle};margin:3px;";
                                }
                            }

                            var textWrapSquare = oleObject.Descendants<DocumentFormat.OpenXml.Vml.Wordprocessing.TextWrap>().FirstOrDefault();
                            if (textWrapSquare != null)
                            {
                                string wrapType = textWrapSquare.Type?.Value.ToString();
                                if (wrapType.Trim().ToLower() == "square")
                                {
                                    var shape = oleObject.Descendants<DocumentFormat.OpenXml.Vml.Shape>().FirstOrDefault();
                                    string style = shape.GetAttribute("style", "").Value;

                                    // Use Regex or CSS parser to extract key styles
                                    string marginLeft = ExtractStyleValue(style, "margin-left");
                                    string marginTop = ExtractStyleValue(style, "margin-top");
                                    string width = ExtractStyleValue(style, "width");
                                    string height = ExtractStyleValue(style, "height");
                                    string position = ExtractStyleValue(style, "position");

                                    string htmlStyle = $"position1:absolute; " +
                                        $"left:{marginLeft}; " +
                                        $"top:{marginTop}; " +
                                        $"width:{width}; " +
                                        $"height:{height};";

                                    // Guess float direction based on margin-left
                                    if (float.TryParse(marginLeft?.Replace("pt", ""), out var ml) && ml > 200)
                                        htmlStyle += " float:right;";
                                    else
                                        htmlStyle += " float:left;";
                                    imageStyle += $"{htmlStyle}margin:3px;";
                                }
                                else
                                {
                                    var shapeStyle = oleObject.Descendants<DocumentFormat.OpenXml.Vml.Shape>().FirstOrDefault();
                                    imageStyle += shapeStyle?.Style ?? "";
                                }
                            }
                            else
                            {
                                var shapeStyle = oleObject.Descendants<DocumentFormat.OpenXml.Vml.Shape>().FirstOrDefault();
                                imageStyle += shapeStyle?.Style ?? "";
                            }

                            var srcRect = oleObject.Descendants<DocumentFormat.OpenXml.Drawing.SourceRectangle>().FirstOrDefault();
                            if (srcRect != null)
                            {
                                int left = int.TryParse(srcRect.Left, out var l) ? l : 0;
                                int top = int.TryParse(srcRect.Top, out var t) ? t : 0;
                                int right = int.TryParse(srcRect.Right, out var r) ? r : 0;
                                int bottom = int.TryParse(srcRect.Bottom, out var b) ? b : 0;

                                // Original image dimensions in EMUs (example: from <wp:extent cx="..." cy="...">)
                                long originalCx = 9144000; // width in EMUs (10 inches as example)
                                long originalCy = 6858000; // height in EMUs (7.5 inches as example)

                                // Compute visible area
                                long visibleCx = originalCx - left - right;
                                long visibleCy = originalCy - top - bottom;

                                // Convert to percentage for CSS
                                float cropLeftPct = (float)left / originalCx * 100;
                                float cropTopPct = (float)top / originalCy * 100;
                                float cropRightPct = (float)right / originalCx * 100;
                                float cropBottomPct = (float)bottom / originalCy * 100;

                                if (left + top + right + bottom != 0)
                                {
                                    imageStyle += $"object-fit: cover; " +
                                                   $"widthx: 100%; height: auto; " +
                                                   $"clip-path: inset({cropTopPct}% {cropRightPct}% {cropBottomPct}% {cropLeftPct}%);";
                                }
                            }

                            var blip = oleObject.Descendants<DocumentFormat.OpenXml.Vml.ImageData>().FirstOrDefault();
                            if (blip == null) continue;

                            var imagePartId = blip.RelationshipId;
                            var imagePart = (ImagePart)mainPart.GetPartById(imagePartId);
                            // Lấy content type (vd: "image/png", "image/jpeg", "image/gif")
                            string contentType = imagePart.ContentType;

                            // Lấy extension (vd: "png", "jpg", "gif")
                            string extension = contentType.Split('/').Last();

                            var eleString = new OpenXmlElementString();
                            //var eleString = new OpenXmlElementStringRefactor();
                            try
                            {
                                using (var stream = imagePart.GetStream())
                                using (var ms = new MemoryStream())
                                {
                                    stream.CopyTo(ms);
                                    //CropImageBySrcRect
                                    string base64Image = Convert.ToBase64String(ms.ToArray());
                                    //dataDocxImgAttr = "";
                                    if (!string.IsNullOrEmpty(imageStyle))
                                        sbHTML.Append($"<img style='{imageStyle}' src='{extension};base64,{base64Image}' >");
                                    else
                                        sbHTML.Append($"<img src='{extension};base64,{base64Image}'>");
                                }
                            }
                            catch
                            {
                                //CropImageBySrcRect
                                string base64Image = Convert.ToBase64String(_dicImageByte[imagePartId]);
                                if (!string.IsNullOrEmpty(imageStyle))
                                    sbHTML.Append($"<img style='{imageStyle}' src='{extension};base64,{base64Image}'>");
                                else
                                    sbHTML.Append($"<img src='{extension};base64,{base64Image}'>");
                            }
                        }
                    }
                }
                else if (element is Drawing drawing)
                {

                    bool isImage = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().Any();

                    if (!isImage)
                    {
                        //sbHTML.Append("[DRAWING]");
                        var imageParts = GetImagePartFromDrawing(mainPart, drawing);

                        int imageIndex = 1;
                        if (imageParts != null)
                        {
                            string contentType = imageParts.ContentType; // ví dụ: "image/png"
                            string extension = GetImageExtension(contentType); // ".png"
                            if (extension != null)
                            {
                                string fileName = Path.Combine("C:", $"image{imageIndex}{extension}");
                                using (var stream = imageParts.GetStream())
                                using (var fileStream = new FileStream(fileName, FileMode.Create))
                                {
                                    stream.CopyTo(fileStream);
                                }

                                imageIndex++;
                            }
                        }
                    }
                    else
                    {
                        var imageStyle = "DRAWING:1;display: inline-flex;" + ExtractImageInfo(drawing.Inline);
                        var anchor = drawing.Anchor;
                        if (anchor != null)
                        {
                            long cx = anchor?.Extent?.Cx;
                            long cy = anchor?.Extent?.Cy;

                            // Chuyển đổi từ EMUs sang pixel (1 inch = 96 dpi, 1 inch = 914400 EMUs)
                            int widthPx = (int)(cx / 9525);   // 914400 / 96 ≈ 9525
                            int heightPx = (int)(cy / 9525);

                            imageStyle += $"width: {widthPx}px;height: {heightPx}px;";
                        }

                        // Get wrap type (square, etc.)
                        string floatStyle = "none";
                        var wrapSquare = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapSquare>().FirstOrDefault();
                        if (wrapSquare != null)
                        {
                            var hPos = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.HorizontalPosition>().FirstOrDefault();

                            if (hPos != null)
                            {
                                if (hPos.RelativeFrom != null && hPos.PositionOffset != null && (hPos.RelativeFrom == "column" || hPos.RelativeFrom == "margin") && int.Parse("" + hPos.PositionOffset.Text) > 11906016 / 4) // A4 EMUS
                                {
                                    floatStyle = "right";
                                    imageStyle += $"float:{floatStyle};margin:3px;";
                                }
                            }
                            else
                            {
                                floatStyle = wrapSquare.WrapText?.Value switch
                                {
                                    DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapTextValues.BothSides => "left",
                                    DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapTextValues.Right => "left",
                                    DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapTextValues.Left => "right",
                                    _ => "left"
                                };
                                imageStyle += $"float:{floatStyle};margin:3px;";
                            }
                        }
                        else
                        {
                            var shapeStyle = drawing.Descendants<DocumentFormat.OpenXml.Vml.Shape>().FirstOrDefault();
                            imageStyle += shapeStyle?.Style ?? "";
                        }

                        var srcRect = drawing.Descendants<DocumentFormat.OpenXml.Drawing.SourceRectangle>().FirstOrDefault();
                        if (srcRect != null)
                        {
                            int left = int.TryParse(srcRect.Left, out var l) ? l : 0;
                            int top = int.TryParse(srcRect.Top, out var t) ? t : 0;
                            int right = int.TryParse(srcRect.Right, out var r) ? r : 0;
                            int bottom = int.TryParse(srcRect.Bottom, out var b) ? b : 0;

                            // Original image dimensions in EMUs (example: from <wp:extent cx="..." cy="...">)
                            long originalCx = 9144000; // width in EMUs (10 inches as example)
                            long originalCy = 6858000; // height in EMUs (7.5 inches as example)

                            // Compute visible area
                            long visibleCx = originalCx - left - right;
                            long visibleCy = originalCy - top - bottom;

                            // Convert to percentage for CSS
                            float cropLeftPct = (float)left / originalCx * 100;
                            float cropTopPct = (float)top / originalCy * 100;
                            float cropRightPct = (float)right / originalCx * 100;
                            float cropBottomPct = (float)bottom / originalCy * 100;

                            if (left + top + right + bottom != 0)
                            {
                                imageStyle += $"object-fit: cover; " +
                                               $"widthx: 100%; height: auto; " +
                                               $"clip-path: inset({cropTopPct}% {cropRightPct}% {cropBottomPct}% {cropLeftPct}%);";
                            }
                        }

                        var blip = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
                        if (blip == null || !blip.Embed.HasValue) continue;

                        var imagePartId = blip.Embed.Value;
                        var imagePart = (ImagePart)mainPart.GetPartById(imagePartId);
                        // Lấy content type (vd: "image/png", "image/jpeg", "image/gif")
                        string contentType = imagePart.ContentType;

                        // Lấy extension (vd: "png", "jpg", "gif")
                        string extension = contentType.Split('/').Last();
                        try
                        {
                            using (var stream = imagePart.GetStream())
                            using (var ms = new MemoryStream())
                            {
                                stream.CopyTo(ms);
                                //CropImageBySrcRect
                                string base64Image = Convert.ToBase64String(ms.ToArray());
                                if (!string.IsNullOrEmpty(imageStyle))
                                    sbHTML.Append($"<img style='{imageStyle}' src='{extension};base64,{base64Image}'>");
                                else
                                    sbHTML.Append($"<img src='{extension};base64,{base64Image}'>");
                            }
                        }
                        catch
                        {
                            //CropImageBySrcRect
                            string base64Image = Convert.ToBase64String(_dicImageByte[imagePartId]);
                            if (!string.IsNullOrEmpty(imageStyle))
                                sbHTML.Append($"<img style='{imageStyle}' src='{extension};base64,{base64Image}'>");
                            else
                                sbHTML.Append($"<img src='{extension};base64,{base64Image}'>");
                        }
                    }
                }
                else
                {
                    //if ((element is Text) == null && element.InnerText != "")
                    //    sbHTML.Append(element + " : " + element.InnerText);
                }
            }
            //"EQ \\* jc2 \\* \"Font:MS Mincho\" \\* hps11 \\o\\ad(\\s\\up 10(とうきょう),東京)"
            if (fieldCodeRuby != "")
            {
                string baseText = "";
                string rtText = "";

                // Lấy nội dung trong <w:rubyBase>
                if (fieldCodeRuby.Contains("),"))
                {
                    baseText = fieldCodeRuby.Split("),")[1].Split(")")[0];
                    rtText = fieldCodeRuby.Split("up 10(")[1].Split(")")[0];

                    // Tạo chuỗi HTML thay thế
                    string rubyHtml = $"<ruby>{baseText}<rt>{rtText}</rt></ruby>";
                    var textString = rubyHtml;

                    // Close previous tags if necessary
                    if (prevTag != "")
                    {
                        if (prevTag.Contains("u")) sbHTML.Append("</u>");
                        if (prevTag.Contains("i")) sbHTML.Append("</i>");
                        if (prevTag.Contains("strong")) sbHTML.Append("</strong>");
                        prevTag = "";
                    }

                    sbHTML.Replace("RUBYFCODE", $"{textString}".Replace("\0", ""));
                }
                prevTag = "";
            }
            // Close any open tags at the end
            if (prevTag != "")
            {
                if (prevTag.Contains("u")) sbHTML.Append("</u>");
                if (prevTag.Contains("i")) sbHTML.Append("</i>");
                if (prevTag.Contains("strong")) sbHTML.Append("</strong>");
            }

            if (textNumberStyle != "")
                sbHTML.Append($"</li>");
            if (paramLevel == 1)
                sbHTML.AppendLine("</p>");
            else
                sbHTML.Append("</p>");

        }

        //parse table to html
        private static void ParseTable(MainDocumentPart mainPart, DocumentFormat.OpenXml.Wordprocessing.Table tbl, int paramLevel, StringBuilder sbHTML, Dictionary<string, byte[]> _dicImageByte)
        {
            var tblProperties = tbl.GetFirstChild<TableProperties>();
            var tblWidth = tblProperties?.GetFirstChild<TableWidth>();
            var tableAlign = tblProperties?.GetFirstChild<TableJustification>();
            var sWidth = "auto";
            var jcPro = "margin-right: auto;"; // mặc định căn trái
            bool isDisableBorder = false;

            //check border 
            var borders = tblProperties?.GetFirstChild<TableBorders>();
            if (borders != null)
            {
                var borderValues = new[]
                {
                    borders.TopBorder?.Val?.Value,
                    borders.BottomBorder?.Val?.Value,
                    borders.LeftBorder?.Val?.Value,
                    borders.RightBorder?.Val?.Value,
                    borders.InsideHorizontalBorder?.Val?.Value,
                    borders.InsideVerticalBorder?.Val?.Value
                };
                isDisableBorder = borderValues.All(val => val == null || val == BorderValues.Nil);
            }

            if (tblWidth != null && int.TryParse(tblWidth.Width, out int widthTwips))
            {
                if (tblWidth.Type! + "" == "dxa")
                    sWidth = (widthTwips / 15.0).ToString("#.##") + "px";
                else if (tblWidth.Type! + "" == "pct")
                    sWidth = (widthTwips / 50.0).ToString("#.##") + "%";
                else
                    sWidth = (widthTwips / 9525.0).ToString("#.##") + "px";
            }

            if (tableAlign?.Val != null)
            {
                switch (tableAlign.Val.Value)
                {
                    case TableRowAlignmentValues.Right:
                        jcPro = "margin-left: auto;";
                        break;
                    case TableRowAlignmentValues.Center:
                        jcPro = "margin: auto;";
                        break;
                    default:
                        jcPro = "margin-right: auto;";
                        break;
                }
            }
            // Theo dõi rowspan
            var rowSpanMap = new Dictionary<(int rowIndex, int colIndex), int>();
            int currentRowIndex = 0;
            if (sWidth == "") sWidth = "100%";
            if (isDisableBorder != true)
            {
                sbHTML.Append($"<table class='table-cau-hoi' style='width: {sWidth ?? "200px"};border-collapse: collapse;{jcPro}';>");
            }
            else
            {
                sbHTML.Append($"<table class='table-disable-border' style='width: {sWidth ?? "200px"};border-collapse: collapse;{jcPro}'>");
            }

            foreach (var row in tbl.Elements<TableRow>())
            {
                //dataDocxTblAttr = "";
                sbHTML.Append($"<tr>");
                int colIndex = 0;

                var cells = row.Elements<TableCell>().ToList();
                int cellIndex = 0;

                while (cellIndex < cells.Count || rowSpanMap.ContainsKey((currentRowIndex, colIndex)))
                {
                    // Kiểm tra nếu vị trí này đang bị chiếm bởi rowspan từ hàng trước
                    if (rowSpanMap.TryGetValue((currentRowIndex, colIndex), out int spanLeft))
                    {
                        rowSpanMap[(currentRowIndex, colIndex)] = spanLeft - 1;
                        if (rowSpanMap[(currentRowIndex, colIndex)] <= 0)
                            rowSpanMap.Remove((currentRowIndex, colIndex));
                        colIndex++;
                        continue;
                    }

                    var cell = cells[cellIndex];
                    cellIndex++;

                    string cellWidth = "auto";
                    int colspan = 1;
                    int rowspan = 1;

                    var cellProps = cell.GetFirstChild<TableCellProperties>();
                    var cellWidthElem = cellProps?.GetFirstChild<TableCellWidth>();
                    var gridSpan = cellProps?.GetFirstChild<GridSpan>();
                    var vMerge = cellProps?.GetFirstChild<VerticalMerge>();

                    // Xử lý độ rộng
                    if (cellWidthElem != null && int.TryParse(cellWidthElem.Width, out int widthCellTwips))
                    {
                        if (cellWidthElem.Type + "" == "dxa")
                            cellWidth = (widthCellTwips / 15.0).ToString("#.##") + "px";
                        else if (cellWidthElem.Type + "" == "pct")
                            cellWidth = (widthCellTwips / 50.0).ToString("#.##") + "%";
                        else
                            cellWidth = (widthCellTwips / 9525.0).ToString("#.##") + "px";
                    }

                    // Xử lý colspan
                    if (gridSpan != null && int.TryParse(gridSpan.Val, out int span))
                    {
                        colspan = span;
                    }

                    // Xử lý rowspan
                    if (vMerge != null)
                    {
                        if (vMerge != null && vMerge.Val == null || vMerge.Val?.Value == MergedCellValues.Continue)
                        {
                            // Tìm cell gốc để tính rowspan
                            int r = currentRowIndex - 1;
                            while (r >= 0 && rowSpanMap.ContainsKey((r, colIndex)) && rowSpanMap[(r, colIndex)] > 1)
                            {
                                r--;
                            }
                            continue; // Không render cell đang merge tiếp theo
                        }
                        else if (vMerge != null && vMerge.Val?.Value == MergedCellValues.Restart)
                        {
                            // Tính rowspan bằng cách đếm bao nhiêu hàng có vMerge = continue
                            int spanCount = 1;
                            int tempRowIndex = currentRowIndex + 1;
                            while (tempRowIndex < tbl.Elements<TableRow>().Count())
                            {
                                var tempRow = tbl.Elements<TableRow>().ElementAt(tempRowIndex);
                                var tempCells = tempRow.Elements<TableCell>().ToList();
                                if (colIndex < tempCells.Count)
                                {
                                    var tempProps = tempCells[colIndex].GetFirstChild<TableCellProperties>();
                                    var tempMerge = tempProps?.GetFirstChild<VerticalMerge>();
                                    if (tempMerge != null && (tempMerge.Val == null || tempMerge.Val.Value == MergedCellValues.Continue))
                                    {
                                        spanCount++;
                                        tempRowIndex++;
                                    }
                                    else break;
                                }
                                else break;
                            }

                            if (spanCount > 1)
                            {
                                rowspan = spanCount;
                                for (int i = 1; i < rowspan; i++)
                                {
                                    rowSpanMap[(currentRowIndex + i, colIndex)] = rowspan - i;
                                }
                            }
                        }
                    }


                    //dataDocxCellAttr = "";
                    sbHTML.Append($"<td style='width: {cellWidth}; position: relative; vertical-align: top;'" + // vertical-align: top;
                                  $"{(colspan > 1 ? $" colspan='{colspan}'" : "")}" +
                                  $"{(rowspan > 1 ? $" rowspan='{rowspan}'" : "")}>");

                    if (cell.Descendants<TopLeftToBottomRightCellBorder>().Any())
                    {
                        sbHTML.Append(@"
      <div  class=""diagonal-line"" style=""
        position: absolute;
        top: 0; left: 0;
        width: 100%;
        height: 100%;
        background: linear-gradient(to bottom left, transparent 49%, black 50%, transparent 51%);
        pointer-events: none;""></div>");
                    }
                    if (cell.Descendants<TopRightToBottomLeftCellBorder>().Any())
                    {
                        sbHTML.Append(@"
      <div  class=""diagonal-line"" style=""
        position: absolute;
        top: 0; left: 0;
        width: 100%;
        height: 100%;
        background: linear-gradient(to bottom right, transparent 49%, black 50%, transparent 51%);
        pointer-events: none;""></div>");
                    }

                    foreach (var para in cell.Elements<Paragraph>())
                    {
                        ParseParagraph(mainPart, para, paramLevel + 1, sbHTML, _dicImageByte);
                    }

                    sbHTML.Append($"</td>");
                    colIndex += colspan;
                }

                sbHTML.Append($"</tr>");
                currentRowIndex++;
            }

            sbHTML.Append($"</table>");
        }
        #endregion

        public static (string, Dictionary<string, byte[]>) handleReadXml(Stream docxInput)
        {
            StringBuilder sbHTML = new StringBuilder();
            //string warrning;

            // Copy input stream to memory to allow OpenXML write
            var memStream = new MemoryStream();
            docxInput.CopyTo(memStream);
            memStream.Position = 0;
            Dictionary<string, byte[]> _dicImageByte = new Dictionary<string, byte[]> { };
            using (WordprocessingDocument doc = WordprocessingDocument.Open(memStream, false))
            {
                var mainPart = doc.MainDocumentPart;
                var body = mainPart.Document.Body;

                var lstXml = body.Elements();

                foreach (var element in lstXml)
                {

                    var paramLevel = 1;
                    if (element is Paragraph p)
                    {
                        ParseParagraph(mainPart, p, paramLevel, sbHTML, _dicImageByte);
                    }
                    else if (element is DocumentFormat.OpenXml.Wordprocessing.Table tbl)
                    {
                        sbHTML.Append("<p>");
                        ParseTable(mainPart, tbl, paramLevel, sbHTML, _dicImageByte);
                        sbHTML.AppendLine("</p>");
                    }
                }
            }

            // Reset stream for output
            memStream.Position = 0;
            return (sbHTML.ToString(), _dicImageByte);
        }
    }
}

public class ImportNHCHelper
{
    // Hàm chuyển XML trong Office Math sang Latex
    public static string ConvertOfficeMathToLatex(string mathXml)
    {
        var rootPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
        var path = System.IO.Path.Combine(rootPath, "Templates");

        // Đường dẫn file XSLT chuyển OMML → MathML
        string ommlToMathMLXslPath = Path.Combine(path, "ImportNHCH", "omml2mml.xsl");
        //"C:\\G-Connect\\HTTHI\\Example\\omml2mml.xsl";
        // Đường dẫn file XSLT chuyển MathML → LaTeX
        string mathMLToLaTeXXslPath = Path.Combine(path, "ImportNHCH", "mmltex.xsl");
        //string mathMLToLaTeXXslPath = "C:\\G-Connect\\HTTHI\\Example\\mmltex.xsl";
        // Chuyển định dạng OMML sang MathML
        string mathml = ConvertOMMLToMathML(mathXml, ommlToMathMLXslPath);
        // Chuyển MathML sang latex
        string latex = ConvertMathMLToLaTeX(mathml, mathMLToLaTeXXslPath);
        return latex;
    }

    // Chuyển OMML → MathML
    static string ConvertOMMLToMathML(string ommlXml, string xslPath)
    {
        XslCompiledTransform xslt = new XslCompiledTransform();
        xslt.Load(xslPath);

        using (StringReader sr = new StringReader(ommlXml))
        using (XmlReader xr = XmlReader.Create(sr))
        using (StringWriter sw = new StringWriter())
        using (XmlWriter xw = XmlWriter.Create(sw))
        {
            xslt.Transform(xr, xw);
            string mathml = sw.ToString();
            // 🔹 Loại bỏ tất cả tiền tố "mml:"
            mathml = Regex.Replace(mathml, @"\bmml:", "");

            mathml = Regex.Replace(mathml, @"\b:mml", "");

            mathml = Regex.Replace(mathml, @"\s*xmlns:m=""http://schemas\.openxmlformats\.org/officeDocument/2006/math""", "");

            return mathml;
        }
    }
    // Chuyển MathML → LaTeX
    static string ConvertMathMLToLaTeX(string mathmlXml, string xslPath)
    {
        if (!System.IO.File.Exists(xslPath))
        {
            throw new FileNotFoundException($"Không tìm thấy file XSLT tại: {xslPath}");
        }
        try
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            XsltSettings settings = new XsltSettings(true, false);
            CustomXmlResolver resolver = new CustomXmlResolver();
            xslt.Load(xslPath, settings, resolver);

            using (StringReader stringReader = new StringReader(mathmlXml))
            using (XmlReader xmlReader = XmlReader.Create(stringReader))
            using (StringWriter writer = new StringWriter())
            {
                XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
                {
                    ConformanceLevel = ConformanceLevel.Fragment  // Cho phép XML fragment
                };

                using (XmlWriter xmlWriter = XmlWriter.Create(writer, xmlWriterSettings))
                {
                    xslt.Transform(xmlReader, null, xmlWriter); // Truyền XmlReader vào Transform
                }
                String result = writer.ToString().Replace("$", "$$$");
                return result;
            }
        }
        catch (XsltException ex)
        {
            Console.WriteLine($"Lỗi XSLT tại dòng {ex.LineNumber}, cột {ex.LinePosition}: {ex.Message}");
            return null;
        }
    }
}

public class CustomXmlResolver : XmlUrlResolver
{
    public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
    {
        return base.GetEntity(absoluteUri, role, ofObjectToReturn);
    }
}