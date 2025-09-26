using econsys.DocGenServiceSTA.Constants;
using econsys.DocGenServiceSTA.Controllers;
using econsys.DocGenServiceSTA.Models;
using econsys.DocGenServiceSTA.Services.Interfaces;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using System.Text.RegularExpressions;

namespace econsys.DocGenServiceSTA.Services
{
    public class DocGeneratorService : IDocGeneratorService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DocumentGeneratorController> _logger;
        private readonly UnresolvedPlaceHolderSettings _unresolvedPlaceHolderSettings;

        public DocGeneratorService(ILogger<DocumentGeneratorController> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
            _unresolvedPlaceHolderSettings = _configuration.GetSection("UnresolvedPlaceHolderSettings").Get<UnresolvedPlaceHolderSettings>();
        }

        public Task<WordDocument> Generate(DocGenDto docGenDto)
        {
            //Pre
            PreValidateDocGenDto(docGenDto);
            PreprocessAndSeggregateRequestVariables(docGenDto);

            //Merge
            WordDocument targetDocument = MergeAndGetTargetDocument(docGenDto);
            if (targetDocument == null) throw new Exception("MergeAndGetTargetDocument - Result is null");

            //Resolve Vriables
            ProcessVariablesInTargetDoc(docGenDto,targetDocument);

            return Task.FromResult(targetDocument);
        }

        #region Utility Methods

        #region PreValidate and Preprocess Variables Data and Segregate SimpleVariables and RepeatedVariables in DocGenDto

        private bool PreValidateDocGenDto(DocGenDto docGenDto)
        {
            if (docGenDto == null)
            {
                throw new Exception("PreValidateDocGenDto - docGenDto is null");
            }

            if (docGenDto.Document == null)
            {
                throw new Exception("PreValidateDocGenDto - docGenDto.Document is null");
            }

            if (docGenDto.HasStationery && docGenDto.Stationery == null)
            {
                throw new Exception("PreValidateDocGenDto - docGenDto.HasStationery is true, but docGenDto.Stationery is null");
            }

            return true;
        }

        private void PreprocessAndSeggregateRequestVariables(DocGenDto docGenDto)
        {
            docGenDto.SimpleVariables = new Dictionary<string, object>();
            docGenDto.RepeatedVariables = new Dictionary<string, List<Dictionary<string, object>>>();

            if (docGenDto.VariableData == null) return;

            foreach (var kv in docGenDto.VariableData)
            {
                // Handle repeated variables (tables)
                if (kv.Value is System.Text.Json.JsonElement jsonElem && jsonElem.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var list = new List<Dictionary<string, object>>();
                    foreach (var item in jsonElem.EnumerateArray())
                    {
                        if (item.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            var dict = new Dictionary<string, object>();
                            foreach (var prop in item.EnumerateObject())
                                dict[prop.Name] = prop.Value.ValueKind == System.Text.Json.JsonValueKind.Number ? (object)prop.Value.GetDecimal() : (object)prop.Value.ToString();
                            list.Add(dict);
                        }
                    }
                    if (list.Count > 0)
                        docGenDto.RepeatedVariables[kv.Key] = list;
                }
                else if (kv.Value is IEnumerable<object> objList && !(kv.Value is string))
                {
                    var list = new List<Dictionary<string, object>>();
                    foreach (var item in objList)
                    {
                        var dict = new Dictionary<string, object>();
                        var props = item.GetType().GetProperties();
                        foreach (var prop in props)
                        {
                            var val = prop.GetValue(item);
                            dict[prop.Name] = val;
                        }
                        list.Add(dict);
                    }
                    if (list.Count > 0)
                        docGenDto.RepeatedVariables[kv.Key] = list;
                }
                // Handle simple/flattened variables
                else if (kv.Value is System.Text.Json.JsonElement elem && elem.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    var flat = new Dictionary<string, object>();
                    FlattenObject(kv.Key, kv.Value, flat);
                    foreach (var fkv in flat)
                        docGenDto.SimpleVariables[fkv.Key] = fkv.Value;
                }
                else if (kv.Value is IDictionary<string, object> dict)
                {
                    var flat = new Dictionary<string, object>();
                    FlattenObject(kv.Key, dict, flat);
                    foreach (var fkv in flat)
                        docGenDto.SimpleVariables[fkv.Key] = fkv.Value;
                }
                else if (kv.Value != null && kv.Value.GetType().IsClass && !(kv.Value is string))
                {
                    var flat = new Dictionary<string, object>();
                    FlattenObject(kv.Key, kv.Value, flat);
                    foreach (var fkv in flat)
                        docGenDto.SimpleVariables[fkv.Key] = fkv.Value;
                }
                else
                {
                    docGenDto.SimpleVariables[kv.Key] = kv.Value;
                }
            }
        }

        private void FlattenObject(string prefix, object obj, Dictionary<string, object> flatDict)
        {
            if (obj == null) return;
            var type = obj.GetType();
            if (obj is IDictionary<string, object> dict)
            {
                foreach (var kv in dict)
                {
                    FlattenObject(string.IsNullOrEmpty(prefix) ? kv.Key : $"{prefix}.{kv.Key}", kv.Value, flatDict);
                }
            }
            else if (obj is System.Text.Json.JsonElement jsonElem)
            {
                if (jsonElem.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var prop in jsonElem.EnumerateObject())
                    {
                        FlattenObject(string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}", prop.Value, flatDict);
                    }
                }
                else if (jsonElem.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    // Do not flatten arrays here
                }
                else
                {
                    flatDict[prefix] = jsonElem.ToString();
                }
            }
            else if (!(obj is string) && obj is System.Collections.IEnumerable enumerable && !(obj is byte[]))
            {
                // Do not flatten arrays here
            }
            else if (type.IsClass && type != typeof(string))
            {
                foreach (var prop in type.GetProperties())
                {
                    var val = prop.GetValue(obj);
                    FlattenObject(string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}", val, flatDict);
                }
            }
            else
            {
                flatDict[prefix] = obj;
            }
        }
        #endregion

        #region Merge Docs
        private WordDocument MergeAndGetTargetDocument(DocGenDto docGenDto)
        {
            //No stationary. So no merge required, return the doc as it is
            if (!docGenDto.HasStationery)
            {
                return docGenDto.Document;
            }

            //Else pick stationary, then take each section from the document and put in stationary

            WordDocument targetDocument = docGenDto.Stationery;
            WordDocument sourceDocument = docGenDto.Document;

            foreach (WSection selectedSection in sourceDocument.Sections)
            {
                IWSection targetSection = targetDocument.LastSection;
                foreach (var entity in selectedSection.Body.ChildEntities)
                {
                    if (entity is Entity ent)
                    {
                        targetSection.Body.ChildEntities.Add(ent.Clone());
                    }
                }
            }

            return targetDocument;
        } 
        #endregion
               
        #region Tabular Data - Rows repeatation inside the doc (Helper to process repeated table rows for a list (e.g., cvr_Summary))
        private void ExpandTableRowsForList(WordDocument document, string listName, IList<object> list, List<string> propertyNames)
        {
            foreach (WSection section in document.Sections)
            {
                foreach (IWTable table in section.Body.Tables)
                {
                    for (int rowIdx = 0; rowIdx < table.Rows.Count; rowIdx++)
                    {
                        var row = table.Rows[rowIdx];
                        bool isTemplateRow = false;
                        foreach (var cellObj in row.Cells)
                        {
                            var cell = cellObj as WTableCell;
                            if (cell == null) continue;
                            foreach (var paraObj in cell.Paragraphs)
                            {
                                if (paraObj is WParagraph para)
                                {
                                    foreach (var prop in propertyNames)
                                    {
                                        string variable = $"{CommonConstants.Variable_Prefix}{listName}.{prop}{CommonConstants.Variable_Suffix}";
                                        //$"<${listName}.{prop}$>"
                                        if (para.Text.Contains(variable, StringComparison.OrdinalIgnoreCase))
                                        {
                                            isTemplateRow = true;
                                            break;
                                        }
                                    }
                                    if (isTemplateRow) break;
                                }
                            }
                            if (isTemplateRow) break;
                        }
                        if (isTemplateRow)
                        {
                            // Insert rows for each item in the list
                            for (int i = 0; i < list.Count; i++)
                            {
                                var item = list[i];
                                var newRow = (WTableRow)row.Clone();
                                for (int c = 0; c < newRow.Cells.Count; c++)
                                {
                                    var newCell = newRow.Cells[c] as WTableCell;
                                    if (newCell == null) continue;
                                    foreach (var paraObj in newCell.Paragraphs)
                                    {
                                        if (paraObj is WParagraph para)
                                        {
                                            foreach (var prop in propertyNames)
                                            {
                                                string placeholder = $"{CommonConstants.Variable_Prefix}{listName}.{prop}{CommonConstants.Variable_Suffix}";
                                                //string placeholder = $"<${listName}.{prop}$>";
                                                
                                                string value = string.Empty;
                                                if (item is Dictionary<string, object> dict && dict.ContainsKey(prop))
                                                {
                                                    value = dict[prop]?.ToString() ?? string.Empty;
                                                }
                                                else
                                                {
                                                    value = GetPropertyValue(item, prop)?.ToString() ?? string.Empty;
                                                }
                                                para.Text = para.Text.Replace(placeholder, value);
                                                // Also replace in WTextRange if split
                                                for (int ent = 0; ent < para.ChildEntities.Count; ent++)
                                                {
                                                    if (para.ChildEntities[ent] is WTextRange tr && tr.Text.Contains(placeholder))
                                                        tr.Text = tr.Text.Replace(placeholder, value);
                                                }
                                            }
                                        }
                                    }
                                }
                                table.Rows.Insert(rowIdx + i + 1, newRow);
                            }
                            table.Rows.Remove(row); // Remove template row
                            return; // Only one template row per table
                        }
                    }
                }
            }
        }

        private object GetPropertyValue(object obj, string propertyName)
        {
            if (obj is System.Text.Json.JsonElement jsonElem && jsonElem.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (jsonElem.TryGetProperty(propertyName, out var val))
                    return val.ToString();
            }
            var type = obj.GetType();
            var prop = type.GetProperty(propertyName);
            return prop?.GetValue(obj);
        }
        #endregion

        #region Variable Resolve Logics
        private void ProcessVariablesInTargetDoc(DocGenDto docGenDto, WordDocument targetDocument)
        {
            // Expand repeated table rows in the doc
            foreach (var tableEntry in docGenDto.RepeatedVariables)
            {
                if (tableEntry.Value.Count > 0)
                {
                    var propertyNames = tableEntry.Value[0].Keys.ToList();
                    var list = tableEntry.Value.Select(row => (object)row).ToList();
                    ExpandTableRowsForList(targetDocument, tableEntry.Key, list, propertyNames);
                }
            }

            // Process variables in selectedDoc using SimpleVariables
            ProcessDocumentVariables(targetDocument, docGenDto.SimpleVariables);

            //Finally Replace unresolved placeholders with ''
            if(_unresolvedPlaceHolderSettings.ShouldCleanup)
                ReplaceUnresolvedPlaceholders(targetDocument);
        }

        private void ReplacePlaceholderInParagraph(WParagraph para, string placeholder, string value)
        {
            // Gather all WTextRange objects
            var textRanges = new List<(int idx, WTextRange range)>();
            for (int i = 0; i < para.ChildEntities.Count; i++)
            {
                if (para.ChildEntities[i] is WTextRange tr)
                    textRanges.Add((i, tr));
            }
            if (textRanges.Count == 0) return;

            // Reconstruct the full text and map indices
            string fullText = string.Concat(textRanges.Select(tr => tr.range.Text));
            int matchIndex = fullText.IndexOf(placeholder, StringComparison.Ordinal);
            if (matchIndex == -1) return;

            // Find which text ranges the match spans
            int charCount = 0;
            int startIdx = -1, endIdx = -1;
            for (int i = 0; i < textRanges.Count; i++)
            {
                int nextCharCount = charCount + textRanges[i].range.Text.Length;
                if (startIdx == -1 && matchIndex < nextCharCount)
                    startIdx = i;
                if (startIdx != -1 && (matchIndex + placeholder.Length) <= nextCharCount)
                {
                    endIdx = i;
                    break;
                }
                charCount = nextCharCount;
            }
            if (startIdx == -1 || endIdx == -1) return;

            // Replace the matched text ranges with a single one
            // Remove all in the range except the first
            for (int i = endIdx; i > startIdx; i--)
                para.ChildEntities.RemoveAt(textRanges[i].idx);
            // Set the first one's text to the replacement value
            ((WTextRange)para.ChildEntities[textRanges[startIdx].idx]).Text = value;
        }

        private void ProcessDocumentVariables(WordDocument document, Dictionary<string, object> variables)
        {
            if (variables == null || variables.Count == 0) return;

            // First, replace all text variables globally (body, header, footer)
            foreach (KeyValuePair<string, object> entry in variables)
            {
                string key = entry.Key;
                string value = entry.Value?.ToString() ?? string.Empty;
                if (!key.Contains("Image:", StringComparison.OrdinalIgnoreCase))
                {
                    string placeholder = $"{CommonConstants.Variable_Prefix}{key}{CommonConstants.Variable_Suffix}";
                    //string placeholder = $"<${key}$>";
                    document.Replace(placeholder, value, true, true);

                    // Replace in all headers and footers
                    foreach (WSection section in document.Sections)
                    {
                        var headersFooters = section.HeadersFooters;
                        if (headersFooters != null)
                        {
                            HeaderFooter[] allHeadersFooters = new HeaderFooter[] {
                                headersFooters.Header,
                                headersFooters.FirstPageHeader,
                                headersFooters.OddHeader,
                                headersFooters.EvenHeader,
                                headersFooters.Footer,
                                headersFooters.FirstPageFooter,
                                headersFooters.OddFooter,
                                headersFooters.EvenFooter
                            };
                            foreach (var hf in allHeadersFooters)
                            {
                                if (hf == null) continue;
                                foreach (var entity in hf.ChildEntities)
                                {
                                    if (entity is WParagraph para)
                                    {
                                        ReplacePlaceholderInParagraph(para, placeholder, value);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            /* Use if we want to replace image variables in the doc with a specific syntax
            // Handle image variables in the main document body
            foreach (KeyValuePair<string, object> entry in variables)
            {
                string key = entry.Key;
                string value = entry.Value?.ToString() ?? string.Empty;
                if (key.Contains("Image:", StringComparison.OrdinalIgnoreCase))
                {
                    string placeholder = $"{CommonConstants.Variable_Prefix}{key}{CommonConstants.Variable_Suffix}";
                    //string placeholder = $"<${key}$>";
                    InsertImageAtPlaceholder(document, placeholder, value);
                }
            }
            */
        }

        /* Use if we want to replace image variables in the doc with a specific syntax
        private void InsertImageAtPlaceholder(WordDocument document, string placeholder, string imageData)
        {
            TextSelection[] textSelections = document.FindAll(placeholder, false, true);
            if (textSelections != null)
            {
                if ((placeholder).Contains("Image"))
                {
                    //------------- Insert Image into word document 

                    string signature = !string.IsNullOrEmpty(imageData) ? imageData : string.Empty;

                    if (!string.IsNullOrEmpty(signature))
                    {
                        byte[] bytes = Convert.FromBase64String(signature);
                        foreach (TextSelection searchTextSelection in textSelections)
                        {
                            //Replaces the image placeholder text with desired image
                            WParagraph paragraph = new(document);

                            WPicture picture = paragraph.AppendPicture(bytes) as WPicture;
                            picture.Height = 50; picture.Width = 200;

                            picture.HorizontalAlignment = ShapeHorizontalAlignment.Left;
                            TextSelection newSelection = new(paragraph, 0, 1);
                            TextBodyPart bodyPart = new(document);
                            bodyPart.BodyItems.Add(paragraph);

                            document.Replace(searchTextSelection.SelectedText, bodyPart, true, true);
                        }
                    }
                }

            }
        }
        */

        #endregion

        #region Final Cleanup - If need to replace Unresolved variables with an alternate

        private void ReplaceUnresolvedPlaceholders(WordDocument document)
        {
            var placeholderPattern = new Regex(@"<\$[^<>]+\$>");
            foreach (WSection section in document.Sections)
            {
                // Body
                foreach (var entity in section.Body.ChildEntities)
                {
                    if (entity is WParagraph para)
                    {
                        ReplaceUnresolvedInParagraph(para, placeholderPattern);
                    }
                    else if (entity is WTable table)
                    {
                        foreach (WTableRow row in table.Rows)
                        {
                            foreach (WTableCell cell in row.Cells)
                            {
                                foreach (WParagraph cellPara in cell.Paragraphs)
                                {
                                    ReplaceUnresolvedInParagraph(cellPara, placeholderPattern);
                                }
                            }
                        }
                    }
                }
                // Headers/Footers
                var headersFooters = section.HeadersFooters;
                if (headersFooters != null)
                {
                    HeaderFooter[] allHeadersFooters = new HeaderFooter[] {
                        headersFooters.Header,
                        headersFooters.FirstPageHeader,
                        headersFooters.OddHeader,
                        headersFooters.EvenHeader,
                        headersFooters.Footer,
                        headersFooters.FirstPageFooter,
                        headersFooters.OddFooter,
                        headersFooters.EvenFooter
                    };
                    foreach (var hf in allHeadersFooters)
                    {
                        if (hf == null) continue;
                        foreach (var entity in hf.ChildEntities)
                        {
                            if (entity is WParagraph para)
                            {
                                ReplaceUnresolvedInParagraph(para, placeholderPattern);
                            }
                            else if (entity is WTable table)
                            {
                                foreach (WTableRow row in table.Rows)
                                {
                                    foreach (WTableCell cell in row.Cells)
                                    {
                                        foreach (WParagraph cellPara in cell.Paragraphs)
                                        {
                                            ReplaceUnresolvedInParagraph(cellPara, placeholderPattern);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ReplaceUnresolvedInParagraph(WParagraph para, Regex placeholderPattern)
        {
            // Reconstruct the full paragraph text
            string fullText = string.Concat(para.ChildEntities.OfType<WTextRange>().Select(tr => tr.Text));
            if (string.IsNullOrEmpty(fullText)) return;
            if (!placeholderPattern.IsMatch(fullText)) return;

            // Replace all placeholders in the full text
            string replacedText = placeholderPattern.Replace(fullText, _unresolvedPlaceHolderSettings.ReplaceWith);

            // Now update all WTextRange children to match the replaced text
            int pos = 0;
            foreach (var tr in para.ChildEntities.OfType<WTextRange>())
            {
                int len = tr.Text?.Length ?? 0;
                if (len > 0 && pos < replacedText.Length)
                {
                    int take = Math.Min(len, replacedText.Length - pos);
                    tr.Text = replacedText.Substring(pos, take);
                    pos += take;
                }
                else
                {
                    tr.Text = string.Empty;
                }
            }
        }

        #endregion
       
        #endregion
    }
}
