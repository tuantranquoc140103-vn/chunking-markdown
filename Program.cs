using System.Text;
using Markdig;
using Markdig.Syntax;
using static Unit;

FileService fileService = new FileService(@".\data");

string pathFileMarkdown = @"example2.md";

string contentMarkdown = fileService.ReadFile(pathFileMarkdown).Result;
string smallContentMarkdown = contentMarkdown.Length > 1000 ? contentMarkdown[..1000] : contentMarkdown;

// Console.WriteLine(smallContentMarkdown);

var pipelineMarkdown = new MarkdownPipelineBuilder()
                            .UsePipeTables()
                            .UseEmphasisExtras()
                            .Build();

MarkdownDocument document = Markdown.Parse(contentMarkdown, pipelineMarkdown);
// MarkdownDocument document = Markdown.Parse(smallContentMarkdown, pipelineMarkdown);

List<HeadingBlock> chunkHeaders = new List<HeadingBlock>();
List<Block> chunkTables = new List<Block>();
List<string> listChunkParagraph = new List<string>();


// int index = 0;

HeadingBlock? beforHeaderBlock = null;  

foreach(Block block in document)
{
    if(block is HeadingBlock headingBlock)
    {
        chunkHeaders.Add(headingBlock);
        if(beforHeaderBlock is null)
        {
            string content = MarkdownHelper.GetContentBetweenHeader(beforHeaderBlock, headingBlock, contentMarkdown);
            listChunkParagraph.Add(content.Trim());
            
        }
        else if (beforHeaderBlock.Level < headingBlock.Level)
        {
            string content = MarkdownHelper.GetContentBetweenHeader(beforHeaderBlock, headingBlock, contentMarkdown);
            listChunkParagraph.Add(content.Trim());
            
        }

        beforHeaderBlock = headingBlock;
        
        
    }
    else if(block is HtmlBlock htmlBlock && Unit.MarkdownHelper.IsTableBlock(htmlBlock))
    {
        chunkTables.Add(block);
    }
}

int i=0;
foreach(var item in listChunkParagraph)
{
    if(i == 10)
    {
        break;
    }
    i++;
    // if(item.Length > 50)
    // {
    //     Console.WriteLine(item[..50]);
    // }
    // else
    // {
    //     Console.WriteLine(item);
    // }
    // Console.WriteLine(new string('-', 20));
}

var contentHeader1s = Unit.MarkdownHelper.CreateContentBelongtoHeader(contentMarkdown, chunkHeaders, 1);
// var contentHeader2s = Unit.MarkdownHelper.CreateContentBelongtoHeader(contentMarkdown, chunkHeaders, 2);
// var contentHeader3s = Unit.MarkdownHelper.CreateContentBelongtoHeader(contentMarkdown, chunkHeaders, 3);

// Console.WriteLine($"contentHeader1s count: {contentHeader1s.Count}");

// int i = 0;
// Console.WriteLine($"contentHeader2s count: {contentHeader2s.Count}");
// foreach(var item in contentHeader2s)
// {
//     // Console.WriteLine(new string('-', 20));
//     // Console.WriteLine(item.Value);
//     int lengthWord = item.Value.Split(' ').Length;
//     Console.WriteLine($"Count words at index {i++}: {lengthWord}");
//     if(lengthWord > 8192)
//     {
//         Console.WriteLine("\t Too long. next chunk to header 3");
//         Console.WriteLine(new string ('-', 20));
//         var result = Unit.MarkdownHelper.SplitSectionByHeaders(item);
//         Console.WriteLine(new string ('-', 20));

//     }
// }


// Unit.MarkdownHelper.PrintCountHeader(contentHeader1s, 4);
