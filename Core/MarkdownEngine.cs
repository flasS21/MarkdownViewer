using System;
using System.Text;
using System.Threading.Tasks;
using Markdig;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MarkdownViewer.Core
{
    public class MarkdownEngine
    {
        private readonly MarkdownPipeline _pipeline;
        private string? _currentFilePath;

        public MarkdownEngine()
        {
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseTaskLists()
                .UseFootnotes()
                .UseAbbreviations()
                .UseAutoLinks()
                .UseEmojiAndSmiley()
                .UseGridTables()
                .UsePipeTables()
                .UseMediaLinks()
                .Build();
        }

        public void SetCurrentFilePath(string? filePath)
        {
            _currentFilePath = filePath;
        }

        public string ConvertToHtml(string markdown)
        {
            string bodyHtml = Markdown.ToHtml(markdown, _pipeline);
            return bodyHtml;
        }

        public async Task<string> ConvertToHtmlAsync(string markdown)
        {
            return await Task.Run(() => ConvertToHtml(markdown));
        }

        public string ReadFileContent(string filePath)
        {
            // Detect encoding properly
            string content;
            using (var reader = new System.IO.StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                content = reader.ReadToEnd();
            }
            return content;
        }

        public async Task<string> ReadFileContentAsync(string filePath)
        {
            return await Task.Run(() => ReadFileContent(filePath));
        }

        public string GetFileDirectory()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
                return string.Empty;
            return System.IO.Path.GetDirectoryName(_currentFilePath) ?? string.Empty;
        }
    }
}
